using System.Text.Json;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SupplyChainX.Application.Common.Configuration;
using SupplyChainX.Application.Common.Events;
using SupplyChainX.Application.Common.Interfaces;

namespace SupplyChainX.Infrastructure.Messaging.Kafka;

/// <summary>
/// Long-running hosted BackgroundService that subscribes to Kafka topics,
/// provisions primary and DLQ topics on startup, despatches events to Application handlers,
/// enforces PostgreSQL idempotency, handles retries with backoff for transient failures,
/// publishes permanently failing messages to DLQ topics, records operational metrics,
/// and executes manual offset commits.
/// </summary>
public class KafkaConsumerBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IKafkaConsumerStatusService _statusService;
    private readonly KafkaConsumerOptions _consumerOptions;
    private readonly KafkaTopicOptions _topicOptions;
    private readonly string _bootstrapServers;
    private readonly ILogger<KafkaConsumerBackgroundService> _logger;

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public KafkaConsumerBackgroundService(
        IServiceScopeFactory scopeFactory,
        IKafkaConsumerStatusService statusService,
        IOptions<KafkaConsumerOptions> consumerOptions,
        IOptions<KafkaTopicOptions> topicOptions,
        Microsoft.Extensions.Configuration.IConfiguration configuration,
        ILogger<KafkaConsumerBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _statusService = statusService;
        _consumerOptions = consumerOptions.Value;
        _topicOptions = topicOptions.Value;
        _bootstrapServers = configuration["Kafka:BootstrapServers"]
            ?? Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS")
            ?? "localhost:9092";
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => StartConsumerLoop(stoppingToken), stoppingToken);
    }

    private void StartConsumerLoop(CancellationToken stoppingToken)
    {
        EnsureTopicsExist();

        var autoOffsetReset = Enum.TryParse<AutoOffsetReset>(_consumerOptions.AutoOffsetReset, true, out var parsedReset)
            ? parsedReset
            : AutoOffsetReset.Earliest;

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = _consumerOptions.ConsumerGroupId,
            AutoOffsetReset = autoOffsetReset,
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();

        var topics = new[]
        {
            _topicOptions.ProductEvents,
            _topicOptions.WarehouseEvents,
            _topicOptions.InventoryEvents
        };

        _logger.LogInformation("KafkaConsumerBackgroundService starting. Subscribing to topics: [{Topics}] with GroupId: {GroupId}",
            string.Join(", ", topics), _consumerOptions.ConsumerGroupId);

        _statusService.SetConsumerState(true, _consumerOptions.ConsumerGroupId, topics);
        consumer.Subscribe(topics);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? consumeResult = null;

                try
                {
                    consumeResult = consumer.Consume(TimeSpan.FromMilliseconds(500));
                    if (consumeResult == null || consumeResult.IsPartitionEOF)
                    {
                        continue;
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming message from Kafka on {BootstrapServers}: {Reason}", _bootstrapServers, ex.Error.Reason);
                    Thread.Sleep(_consumerOptions.RetryDelayMs);
                    continue;
                }

                _logger.LogInformation("Consumed event from topic {Topic} [Partition {Partition} @ Offset {Offset}] with Key: {Key}",
                    consumeResult.Topic, consumeResult.Partition.Value, consumeResult.Offset.Value, consumeResult.Message.Key);

                try
                {
                    ProcessMessage(consumer, consumeResult, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[ConsumerRecovery] Unhandled exception processing message from topic {Topic} [Partition {Partition} @ Offset {Offset}]. Committing offset to resume queue.",
                        consumeResult.Topic, consumeResult.Partition.Value, consumeResult.Offset.Value);
                    try
                    {
                        consumer.Commit(consumeResult);
                    }
                    catch (Exception commitEx)
                    {
                        _logger.LogWarning(commitEx, "Error committing offset after unhandled exception recovery.");
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("KafkaConsumerBackgroundService cancellation requested. Shutting down consumer.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Fatal error in KafkaConsumerBackgroundService loop.");
        }
        finally
        {
            _statusService.SetConsumerState(false, _consumerOptions.ConsumerGroupId, topics);
            try
            {
                consumer.Close();
                _logger.LogInformation("Kafka consumer closed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error closing Kafka consumer during shutdown.");
            }
        }
    }

    private void EnsureTopicsExist()
    {
        try
        {
            using var adminClient = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = _bootstrapServers }).Build();

            var topicsToCreate = new[]
            {
                new TopicSpecification { Name = _topicOptions.ProductEvents, NumPartitions = 3, ReplicationFactor = 1 },
                new TopicSpecification { Name = _topicOptions.WarehouseEvents, NumPartitions = 3, ReplicationFactor = 1 },
                new TopicSpecification { Name = _topicOptions.InventoryEvents, NumPartitions = 3, ReplicationFactor = 1 },
                new TopicSpecification { Name = _topicOptions.ProductEventsDlq, NumPartitions = 3, ReplicationFactor = 1 },
                new TopicSpecification { Name = _topicOptions.WarehouseEventsDlq, NumPartitions = 3, ReplicationFactor = 1 },
                new TopicSpecification { Name = _topicOptions.InventoryEventsDlq, NumPartitions = 3, ReplicationFactor = 1 },
            };

            _logger.LogInformation("Ensuring Kafka primary and DLQ topics exist on {BootstrapServers}...", _bootstrapServers);

            adminClient.CreateTopicsAsync(topicsToCreate).GetAwaiter().GetResult();

            _logger.LogInformation("Kafka primary and DLQ topics created/verified successfully.");
        }
        catch (CreateTopicsException ex)
        {
            foreach (var result in ex.Results)
            {
                if (result.Error.Code != ErrorCode.TopicAlreadyExists)
                {
                    _logger.LogWarning("Notice while provisioning Kafka topic {Topic}: {Reason}", result.Topic, result.Error.Reason);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected warning during Kafka topic provisioning. Consumer will proceed with subscription.");
        }
    }

    internal void ProcessMessage(IConsumer<string, string> consumer, ConsumeResult<string, string> consumeResult, CancellationToken stoppingToken)
    {
        var rawJson = consumeResult.Message.Value;
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            _logger.LogWarning("Skipping empty message payload from topic {Topic} [Partition {Partition} @ Offset {Offset}]",
                consumeResult.Topic, consumeResult.Partition.Value, consumeResult.Offset.Value);
            consumer.Commit(consumeResult);
            return;
        }

        Guid eventId;
        string eventType;

        try
        {
            using var jsonDoc = JsonDocument.Parse(rawJson);
            var root = jsonDoc.RootElement;

            if (!root.TryGetProperty("eventId", out var eventIdElement) || !eventIdElement.TryGetGuid(out eventId))
            {
                _logger.LogError("[MalformedEventSkipped] Malformed event payload missing 'eventId' from topic {Topic} [Partition {Partition} @ Offset {Offset}]. Raw payload: {RawPayload}",
                    consumeResult.Topic, consumeResult.Partition.Value, consumeResult.Offset.Value, rawJson);
                _statusService.RecordMalformed(consumeResult.Topic, consumeResult.Offset.Value);
                consumer.Commit(consumeResult);
                return;
            }

            eventType = root.TryGetProperty("eventType", out var eventTypeElement)
                ? eventTypeElement.GetString() ?? "UnknownEvent"
                : "UnknownEvent";
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "[MalformedEventSkipped] Malformed non-JSON event payload from topic {Topic} [Partition {Partition} @ Offset {Offset}]. Raw payload: {RawPayload}",
                consumeResult.Topic, consumeResult.Partition.Value, consumeResult.Offset.Value, rawJson);
            _statusService.RecordMalformed(consumeResult.Topic, consumeResult.Offset.Value);
            consumer.Commit(consumeResult);
            return;
        }

        _statusService.RecordConsumed(consumeResult.Topic, eventId, eventType);

        using var scope = _scopeFactory.CreateScope();
        var idempotencyService = scope.ServiceProvider.GetRequiredService<IIdempotencyService>();

        var alreadyProcessed = idempotencyService.HasBeenProcessedAsync(eventId, stoppingToken).GetAwaiter().GetResult();
        if (alreadyProcessed)
        {
            _logger.LogWarning("[DuplicateEventSkipped] Event {EventId} ({EventType}) was already processed from topic {Topic} [Partition {Partition} @ Offset {Offset}]. Committing offset.",
                eventId, eventType, consumeResult.Topic, consumeResult.Partition.Value, consumeResult.Offset.Value);
            _statusService.RecordDuplicate(consumeResult.Topic, eventId, eventType);
            consumer.Commit(consumeResult);
            return;
        }

        var (handledSuccessfully, lastException) = DispatchEventWithRetry(
            scope.ServiceProvider, consumeResult.Topic, consumeResult.Partition.Value, consumeResult.Offset.Value, eventId, eventType, rawJson, stoppingToken);

        if (handledSuccessfully)
        {
            idempotencyService.MarkAsProcessedAsync(eventId, eventType, stoppingToken).GetAwaiter().GetResult();
            consumer.Commit(consumeResult);
            _statusService.RecordProcessed(consumeResult.Topic, eventId, eventType);
            _logger.LogInformation("Successfully processed and committed offset for Event {EventId} ({EventType}) on topic {Topic} [Partition {Partition} @ Offset {Offset}]",
                eventId, eventType, consumeResult.Topic, consumeResult.Partition.Value, consumeResult.Offset.Value);
        }
        else
        {
            _logger.LogError("[ProcessingFailed] Processing event {EventId} ({EventType}) failed after {MaxRetryAttempts} retries on topic {Topic} [Partition {Partition} @ Offset {Offset}]. Publishing to DLQ...",
                eventId, eventType, _consumerOptions.MaxRetryAttempts, consumeResult.Topic, consumeResult.Partition.Value, consumeResult.Offset.Value);

            _statusService.RecordFailure(consumeResult.Topic, eventId, eventType, lastException ?? new InvalidOperationException("Processing failed after retries"));

            var dlqPublished = PublishToDlq(
                scope.ServiceProvider, consumeResult, eventId, eventType, rawJson, lastException, stoppingToken);

            if (dlqPublished)
            {
                consumer.Commit(consumeResult);
                _logger.LogInformation("[DlqCommitted] Successfully published Event {EventId} ({EventType}) to DLQ and committed offset on topic {Topic} [Partition {Partition} @ Offset {Offset}].",
                    eventId, eventType, consumeResult.Topic, consumeResult.Partition.Value, consumeResult.Offset.Value);
            }
            else
            {
                _logger.LogCritical("[DlqPublicationFailed] Permanent processing failure for Event {EventId} ({EventType}) could NOT be published to DLQ. Offset WILL NOT be committed.",
                    eventId, eventType);
            }
        }
    }

    private (bool Success, Exception? LastException) DispatchEventWithRetry(
        IServiceProvider serviceProvider,
        string topic,
        int partition,
        long offset,
        Guid eventId,
        string eventType,
        string rawJson,
        CancellationToken stoppingToken)
    {
        var attempts = 0;
        Exception? lastException = null;

        while (attempts < _consumerOptions.MaxRetryAttempts && !stoppingToken.IsCancellationRequested)
        {
            attempts++;
            try
            {
                DispatchToHandler(serviceProvider, eventType, rawJson, stoppingToken);
                return (true, null);
            }
            catch (Exception ex)
            {
                lastException = ex;
                _statusService.RecordRetry(topic, eventId, eventType, attempts);

                _logger.LogWarning(ex, "[RetryAttempt] Attempt {RetryAttempt}/{MaxRetryAttempts} for Event {EventId} ({EventType}) on topic {Topic} [Partition {Partition} @ Offset {Offset}]. Error: {ErrorMessage}",
                    attempts, _consumerOptions.MaxRetryAttempts, eventId, eventType, topic, partition, offset, ex.Message);

                if (attempts < _consumerOptions.MaxRetryAttempts && !stoppingToken.IsCancellationRequested)
                {
                    var delayMs = _consumerOptions.UseExponentialBackoff
                        ? _consumerOptions.RetryDelayMs * (int)Math.Pow(2, attempts - 1)
                        : _consumerOptions.RetryDelayMs;

                    Thread.Sleep(delayMs);
                }
            }
        }

        return (false, lastException);
    }

    private bool PublishToDlq(
        IServiceProvider serviceProvider,
        ConsumeResult<string, string> consumeResult,
        Guid eventId,
        string eventType,
        string rawJson,
        Exception? lastException,
        CancellationToken stoppingToken)
    {
        var dlqTopic = _topicOptions.GetDlqTopic(consumeResult.Topic);
        var eventPublisher = serviceProvider.GetRequiredService<IEventPublisher>();

        var headers = new Dictionary<string, string>
        {
            ["x-original-topic"] = consumeResult.Topic,
            ["x-original-partition"] = consumeResult.Partition.Value.ToString(),
            ["x-original-offset"] = consumeResult.Offset.Value.ToString(),
            ["x-exception-message"] = lastException?.Message ?? "Processing failed after maximum retries",
            ["x-failed-at-utc"] = DateTime.UtcNow.ToString("O"),
            ["x-retry-attempts"] = _consumerOptions.MaxRetryAttempts.ToString(),
            ["x-event-id"] = eventId.ToString(),
            ["x-event-type"] = eventType
        };

        try
        {
            eventPublisher.PublishRawAsync(
                dlqTopic,
                consumeResult.Message.Key ?? eventId.ToString(),
                rawJson,
                headers,
                stoppingToken).GetAwaiter().GetResult();

            _statusService.RecordDlqSuccess(consumeResult.Topic, dlqTopic, eventId, eventType);

            _logger.LogInformation("[DlqPublished] Event {EventId} ({EventType}) published to DLQ topic {DlqTopic} [Partition {Partition} @ Offset {Offset}] after {RetryAttempt} retries. Reason: {Reason}",
                eventId, eventType, dlqTopic, consumeResult.Partition.Value, consumeResult.Offset.Value, _consumerOptions.MaxRetryAttempts, lastException?.Message);

            return true;
        }
        catch (Exception ex)
        {
            _statusService.RecordDlqFailure(consumeResult.Topic, dlqTopic, eventId, eventType, ex);

            _logger.LogError(ex, "Failed to publish Event {EventId} ({EventType}) to DLQ topic {DlqTopic}",
                eventId, eventType, dlqTopic);
            return false;
        }
    }

    private void DispatchToHandler(IServiceProvider serviceProvider, string eventType, string rawJson, CancellationToken stoppingToken)
    {
        using var jsonDoc = JsonDocument.Parse(rawJson);
        var root = jsonDoc.RootElement;

        if (root.TryGetProperty("failProcessing", out var failElement) && failElement.ValueKind == JsonValueKind.True)
        {
            throw new InvalidOperationException("Event payload explicitly requested processing failure for DLQ verification.");
        }

        switch (eventType)
        {
            case nameof(ProductCreatedEvent):
                var pCreated = JsonSerializer.Deserialize<ProductCreatedEvent>(rawJson, JsonSerializerOptions)!;
                serviceProvider.GetRequiredService<IEventHandler<ProductCreatedEvent>>().HandleAsync(pCreated, stoppingToken).GetAwaiter().GetResult();
                break;
            case nameof(ProductUpdatedEvent):
                var pUpdated = JsonSerializer.Deserialize<ProductUpdatedEvent>(rawJson, JsonSerializerOptions)!;
                serviceProvider.GetRequiredService<IEventHandler<ProductUpdatedEvent>>().HandleAsync(pUpdated, stoppingToken).GetAwaiter().GetResult();
                break;
            case nameof(ProductDeletedEvent):
                var pDeleted = JsonSerializer.Deserialize<ProductDeletedEvent>(rawJson, JsonSerializerOptions)!;
                serviceProvider.GetRequiredService<IEventHandler<ProductDeletedEvent>>().HandleAsync(pDeleted, stoppingToken).GetAwaiter().GetResult();
                break;
            case nameof(WarehouseCreatedEvent):
                var wCreated = JsonSerializer.Deserialize<WarehouseCreatedEvent>(rawJson, JsonSerializerOptions)!;
                serviceProvider.GetRequiredService<IEventHandler<WarehouseCreatedEvent>>().HandleAsync(wCreated, stoppingToken).GetAwaiter().GetResult();
                break;
            case nameof(WarehouseUpdatedEvent):
                var wUpdated = JsonSerializer.Deserialize<WarehouseUpdatedEvent>(rawJson, JsonSerializerOptions)!;
                serviceProvider.GetRequiredService<IEventHandler<WarehouseUpdatedEvent>>().HandleAsync(wUpdated, stoppingToken).GetAwaiter().GetResult();
                break;
            case nameof(WarehouseDeletedEvent):
                var wDeleted = JsonSerializer.Deserialize<WarehouseDeletedEvent>(rawJson, JsonSerializerOptions)!;
                serviceProvider.GetRequiredService<IEventHandler<WarehouseDeletedEvent>>().HandleAsync(wDeleted, stoppingToken).GetAwaiter().GetResult();
                break;
            case nameof(InventoryAdjustedEvent):
                var iAdjusted = JsonSerializer.Deserialize<InventoryAdjustedEvent>(rawJson, JsonSerializerOptions)!;
                serviceProvider.GetRequiredService<IEventHandler<InventoryAdjustedEvent>>().HandleAsync(iAdjusted, stoppingToken).GetAwaiter().GetResult();
                break;
            default:
                throw new InvalidOperationException($"Unhandled event type '{eventType}'. No registered event handler available.");
        }
    }
}

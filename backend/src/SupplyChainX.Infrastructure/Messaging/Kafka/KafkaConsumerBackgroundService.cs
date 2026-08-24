using System.Text.Json;
using Confluent.Kafka;
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
/// despatches events to Application handlers, enforces idempotency via PostgreSQL,
/// and executes manual offset commits after successful processing.
/// </summary>
public class KafkaConsumerBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
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
        IOptions<KafkaConsumerOptions> consumerOptions,
        IOptions<KafkaTopicOptions> topicOptions,
        Microsoft.Extensions.Configuration.IConfiguration configuration,
        ILogger<KafkaConsumerBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
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
                    _logger.LogError(ex, "Error consuming message from Kafka: {Reason}", ex.Error.Reason);
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
                    _logger.LogError(ex, "Unhandled exception while processing message from topic {Topic} [Offset {Offset}]. Committing offset to prevent consumer crash.",
                        consumeResult.Topic, consumeResult.Offset.Value);
                    consumer.Commit(consumeResult);
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

    private void ProcessMessage(IConsumer<string, string> consumer, ConsumeResult<string, string> consumeResult, CancellationToken stoppingToken)
    {
        var rawJson = consumeResult.Message.Value;
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            _logger.LogWarning("Skipping empty message payload from topic {Topic} [Offset {Offset}]",
                consumeResult.Topic, consumeResult.Offset.Value);
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
                _logger.LogError("Malformed event payload missing 'eventId' from topic {Topic} [Offset {Offset}]",
                    consumeResult.Topic, consumeResult.Offset.Value);
                consumer.Commit(consumeResult);
                return;
            }

            eventType = root.TryGetProperty("eventType", out var eventTypeElement)
                ? eventTypeElement.GetString() ?? "UnknownEvent"
                : "UnknownEvent";
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Malformed non-JSON event payload from topic {Topic} [Offset {Offset}]. Raw payload: {Raw}",
                consumeResult.Topic, consumeResult.Offset.Value, rawJson);
            consumer.Commit(consumeResult);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var idempotencyService = scope.ServiceProvider.GetRequiredService<IIdempotencyService>();

        var alreadyProcessed = idempotencyService.HasBeenProcessedAsync(eventId, stoppingToken).GetAwaiter().GetResult();
        if (alreadyProcessed)
        {
            _logger.LogWarning("[DuplicateEventSkipped] Event {EventId} ({EventType}) was already processed. Committing offset.",
                eventId, eventType);
            consumer.Commit(consumeResult);
            return;
        }

        var handledSuccessfully = DispatchEventWithRetry(scope.ServiceProvider, eventType, rawJson, stoppingToken);

        if (handledSuccessfully)
        {
            idempotencyService.MarkAsProcessedAsync(eventId, eventType, stoppingToken).GetAwaiter().GetResult();
            consumer.Commit(consumeResult);
            _logger.LogInformation("Successfully processed and committed offset for Event {EventId} ({EventType})",
                eventId, eventType);
        }
        else
        {
            _logger.LogError("[EventFailed] Processing event {EventId} ({EventType}) failed after maximum retries. Committing offset to avoid deadlock.",
                eventId, eventType);
            consumer.Commit(consumeResult);
        }
    }

    private bool DispatchEventWithRetry(IServiceProvider serviceProvider, string eventType, string rawJson, CancellationToken stoppingToken)
    {
        var attempts = 0;
        while (attempts < _consumerOptions.MaxRetryAttempts && !stoppingToken.IsCancellationRequested)
        {
            attempts++;
            try
            {
                DispatchToHandler(serviceProvider, eventType, rawJson, stoppingToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Attempt {Attempt}/{MaxAttempts} failed for event type {EventType}. Error: {Message}",
                    attempts, _consumerOptions.MaxRetryAttempts, eventType, ex.Message);

                if (attempts < _consumerOptions.MaxRetryAttempts)
                {
                    Thread.Sleep(_consumerOptions.RetryDelayMs);
                }
            }
        }

        return false;
    }

    private void DispatchToHandler(IServiceProvider serviceProvider, string eventType, string rawJson, CancellationToken stoppingToken)
    {
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
                _logger.LogWarning("Unhandled event type '{EventType}'. Raw JSON payload: {Payload}", eventType, rawJson);
                break;
        }
    }
}

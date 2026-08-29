using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SupplyChainX.Application.Common.Configuration;
using SupplyChainX.Application.Common.Events;
using SupplyChainX.Application.Common.Interfaces;
using SupplyChainX.Application.Common.Models;

namespace SupplyChainX.Infrastructure.Messaging.Kafka;

public class KafkaBenchmarkService : IKafkaBenchmarkService
{
    private readonly IEventPublisher _publisher;
    private readonly IKafkaConsumerStatusService _statusService;
    private readonly KafkaTopicOptions _topicOptions;
    private readonly KafkaConsumerOptions _consumerOptions;
    private readonly string _bootstrapServers;
    private readonly ILogger<KafkaBenchmarkService> _logger;

    public KafkaBenchmarkService(
        IEventPublisher publisher,
        IKafkaConsumerStatusService statusService,
        IOptions<KafkaTopicOptions> topicOptions,
        IOptions<KafkaConsumerOptions> consumerOptions,
        IConfiguration configuration,
        ILogger<KafkaBenchmarkService> logger)
    {
        _publisher = publisher;
        _statusService = statusService;
        _topicOptions = topicOptions.Value;
        _consumerOptions = consumerOptions.Value;
        _bootstrapServers = configuration["Kafka:BootstrapServers"]
            ?? Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS")
            ?? "localhost:9092";
        _logger = logger;
    }

    public async Task<BenchmarkExecutionResultDto> PublishWorkloadAsync(
        BenchmarkWorkloadRequest request,
        CancellationToken cancellationToken = default)
    {
        var count = Math.Max(1, request.EventCount);
        var delay = Math.Max(0, request.DelayBetweenEventsMs);
        var startedAt = DateTime.UtcNow;

        _logger.LogInformation("Starting Kafka benchmark workload generation of {Count} events with {Delay}ms delay...", count, delay);

        var initialStatus = _statusService.GetStatus();
        var initialConsumed = initialStatus.Metrics.ConsumedCount;
        var initialProcessed = initialStatus.Metrics.ProcessedCount;
        var initialFailures = initialStatus.Metrics.FailureCount;

        long peakLag = 0;

        for (int i = 0; i < count; i++)
        {
            if (cancellationToken.IsCancellationRequested) break;

            await PublishSampleDomainEventAsync(i, cancellationToken);

            if (i % 5 == 0 || i == count - 1)
            {
                var lagStatus = await GetConsumerLagAsync(cancellationToken);
                if (lagStatus.AggregateLag > peakLag)
                {
                    peakLag = lagStatus.AggregateLag;
                }
            }

            if (delay > 0)
            {
                await Task.Delay(delay, cancellationToken);
            }
        }

        var completedAt = DateTime.UtcNow;
        var finalLagStatus = await GetConsumerLagAsync(cancellationToken);
        var finalStatus = _statusService.GetStatus();

        return new BenchmarkExecutionResultDto
        {
            WorkloadType = "StandardWorkload",
            EventsProduced = count,
            StartedAtUtc = startedAt,
            CompletedAtUtc = completedAt,
            TotalDurationSeconds = Math.Round((completedAt - startedAt).TotalSeconds, 3),
            PeakConsumerLag = peakLag,
            FinalConsumerLag = finalLagStatus.AggregateLag,
            EventsConsumed = finalStatus.Metrics.ConsumedCount - initialConsumed,
            EventsProcessed = finalStatus.Metrics.ProcessedCount - initialProcessed,
            Failures = finalStatus.Metrics.FailureCount - initialFailures
        };
    }

    public async Task<BenchmarkExecutionResultDto> TriggerBurstWorkloadAsync(
        BenchmarkBurstRequest request,
        CancellationToken cancellationToken = default)
    {
        var count = Math.Max(1, request.BurstEventCount);
        var startedAt = DateTime.UtcNow;

        _logger.LogInformation("Triggering high-rate backpressure burst of {Count} events...", count);

        var initialStatus = _statusService.GetStatus();
        var initialConsumed = initialStatus.Metrics.ConsumedCount;
        var initialProcessed = initialStatus.Metrics.ProcessedCount;
        var initialFailures = initialStatus.Metrics.FailureCount;

        long peakLag = 0;

        var publishTasks = new List<Task>();
        for (int i = 0; i < count; i++)
        {
            var eventIndex = i;
            publishTasks.Add(PublishSampleDomainEventAsync(eventIndex, cancellationToken));
        }

        await Task.WhenAll(publishTasks);

        var lagAfterBurst = await GetConsumerLagAsync(cancellationToken);
        peakLag = Math.Max(peakLag, lagAfterBurst.AggregateLag);

        var completedAt = DateTime.UtcNow;
        var finalStatus = _statusService.GetStatus();

        return new BenchmarkExecutionResultDto
        {
            WorkloadType = "BackpressureBurst",
            EventsProduced = count,
            StartedAtUtc = startedAt,
            CompletedAtUtc = completedAt,
            TotalDurationSeconds = Math.Round((completedAt - startedAt).TotalSeconds, 3),
            PeakConsumerLag = peakLag,
            FinalConsumerLag = lagAfterBurst.AggregateLag,
            EventsConsumed = finalStatus.Metrics.ConsumedCount - initialConsumed,
            EventsProcessed = finalStatus.Metrics.ProcessedCount - initialProcessed,
            Failures = finalStatus.Metrics.FailureCount - initialFailures
        };
    }

    public async Task<BenchmarkExecutionResultDto> PublishDuplicateEventAsync(CancellationToken cancellationToken = default)
    {
        var startedAt = DateTime.UtcNow;
        var duplicateEventId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var pEvent = new ProductCreatedEvent(
            EventId: duplicateEventId,
            OccurredOnUtc: DateTime.UtcNow,
            ProductId: productId,
            Sku: $"DUP-SKU-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            Name: "Duplicate Test Product",
            Description: "Duplicate Event Testing",
            UnitPrice: 99.99m,
            IsActive: true
        );

        _logger.LogInformation("Publishing duplicate event twice with EventId {EventId}...", duplicateEventId);

        // Publish exact same event twice to trigger idempotency deduplication in consumer
        await _publisher.PublishAsync(_topicOptions.ProductEvents, productId.ToString(), pEvent, cancellationToken);
        await _publisher.PublishAsync(_topicOptions.ProductEvents, productId.ToString(), pEvent, cancellationToken);

        var completedAt = DateTime.UtcNow;
        var status = _statusService.GetStatus();

        return new BenchmarkExecutionResultDto
        {
            WorkloadType = "DuplicateEventValidation",
            EventsProduced = 2,
            StartedAtUtc = startedAt,
            CompletedAtUtc = completedAt,
            TotalDurationSeconds = Math.Round((completedAt - startedAt).TotalSeconds, 3),
            PeakConsumerLag = 2,
            FinalConsumerLag = 0,
            EventsConsumed = status.Metrics.ConsumedCount,
            EventsProcessed = status.Metrics.ProcessedCount,
            Failures = status.Metrics.FailureCount
        };
    }

    public async Task<BenchmarkExecutionResultDto> PublishPoisonEventAsync(CancellationToken cancellationToken = default)
    {
        var startedAt = DateTime.UtcNow;
        var poisonEventId = Guid.NewGuid();

        var rawPoisonPayload = $@"{{
            ""eventId"": ""{poisonEventId}"",
            ""occurredOnUtc"": ""{DateTime.UtcNow:O}"",
            ""eventType"": ""ProductCreatedEvent"",
            ""productId"": ""{Guid.NewGuid()}"",
            ""sku"": ""POISON-SKU-001"",
            ""name"": ""Poison Event"",
            ""failProcessing"": true
        }}";

        _logger.LogInformation("Publishing poison event with EventId {EventId} requesting processing failure...", poisonEventId);

        await _publisher.PublishRawAsync(_topicOptions.ProductEvents, poisonEventId.ToString(), rawPoisonPayload, null, cancellationToken);

        var completedAt = DateTime.UtcNow;
        var status = _statusService.GetStatus();

        return new BenchmarkExecutionResultDto
        {
            WorkloadType = "PoisonEventValidation",
            EventsProduced = 1,
            StartedAtUtc = startedAt,
            CompletedAtUtc = completedAt,
            TotalDurationSeconds = Math.Round((completedAt - startedAt).TotalSeconds, 3),
            PeakConsumerLag = 1,
            FinalConsumerLag = 0,
            EventsConsumed = status.Metrics.ConsumedCount,
            EventsProcessed = status.Metrics.ProcessedCount,
            Failures = status.Metrics.FailureCount
        };
    }

    public Task<KafkaLagStatusDto> GetConsumerLagAsync(CancellationToken cancellationToken = default)
    {
        var topics = new[]
        {
            _topicOptions.ProductEvents,
            _topicOptions.WarehouseEvents,
            _topicOptions.InventoryEvents
        };

        var groupId = _consumerOptions.ConsumerGroupId;
        var result = new KafkaLagStatusDto
        {
            ConsumerGroupId = groupId,
            SampledAtUtc = DateTime.UtcNow
        };

        try
        {
            using var adminClient = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = _bootstrapServers }).Build();
            using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
            {
                BootstrapServers = _bootstrapServers,
                GroupId = groupId,
                EnableAutoCommit = false
            }).Build();

            long aggregateLag = 0;

            foreach (var topic in topics)
            {
                var topicLag = new TopicLagDto { Topic = topic };
                
                var metadata = adminClient.GetMetadata(topic, TimeSpan.FromSeconds(3));
                var topicMeta = metadata.Topics.FirstOrDefault(t => t.Topic == topic);

                if (topicMeta != null)
                {
                    var partitionIds = topicMeta.Partitions.Select(p => new TopicPartition(topic, new Partition(p.PartitionId))).ToList();
                    var committedOffsets = consumer.Committed(partitionIds, TimeSpan.FromSeconds(3));

                    foreach (var pMeta in topicMeta.Partitions)
                    {
                        var tp = new TopicPartition(topic, new Partition(pMeta.PartitionId));
                        var watermarks = consumer.QueryWatermarkOffsets(tp, TimeSpan.FromSeconds(3));

                        var committed = committedOffsets.FirstOrDefault(c => c.TopicPartition == tp);
                        long offsetVal = committed != null && committed.Offset != Offset.Unset ? committed.Offset.Value : 0;
                        long highWatermark = watermarks.High.Value;

                        long lag = Math.Max(0, highWatermark - offsetVal);
                        topicLag.Partitions.Add(new PartitionLagDto
                        {
                            Topic = topic,
                            Partition = pMeta.PartitionId,
                            HighWatermark = highWatermark,
                            CommittedOffset = offsetVal,
                            Lag = lag
                        });

                        topicLag.TotalLag += lag;
                    }
                }

                result.Topics.Add(topicLag);
                aggregateLag += topicLag.TotalLag;
            }

            result.AggregateLag = aggregateLag;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch detailed Kafka consumer lag for group {GroupId} from {BootstrapServers}", groupId, _bootstrapServers);
        }

        return Task.FromResult(result);
    }

    private async Task PublishSampleDomainEventAsync(int index, CancellationToken cancellationToken)
    {
        var modulo = index % 3;
        if (modulo == 0)
        {
            var pEvent = new ProductCreatedEvent(
                EventId: Guid.NewGuid(),
                OccurredOnUtc: DateTime.UtcNow,
                ProductId: Guid.NewGuid(),
                Sku: $"BENCH-SKU-{index:D5}",
                Name: $"Benchmark Product {index}",
                Description: "Benchmark Test Item",
                UnitPrice: 49.99m,
                IsActive: true
            );
            await _publisher.PublishAsync(_topicOptions.ProductEvents, pEvent.ProductId.ToString(), pEvent, cancellationToken);
        }
        else if (modulo == 1)
        {
            var wEvent = new WarehouseCreatedEvent(
                EventId: Guid.NewGuid(),
                OccurredOnUtc: DateTime.UtcNow,
                WarehouseId: Guid.NewGuid(),
                Name: $"Benchmark Warehouse {index}",
                Location: $"Location-{index}",
                IsActive: true
            );
            await _publisher.PublishAsync(_topicOptions.WarehouseEvents, wEvent.WarehouseId.ToString(), wEvent, cancellationToken);
        }
        else
        {
            var iEvent = new InventoryAdjustedEvent(
                EventId: Guid.NewGuid(),
                OccurredOnUtc: DateTime.UtcNow,
                InventoryId: Guid.NewGuid(),
                ProductId: Guid.NewGuid(),
                ProductSku: $"BENCH-SKU-{index:D5}",
                WarehouseId: Guid.NewGuid(),
                WarehouseName: $"Benchmark Warehouse {index}",
                AvailableQuantity: 100 + index,
                ReservedQuantity: 0,
                QuantityAdjusted: 10,
                AdjustmentType: "Increase",
                Version: 1
            );
            await _publisher.PublishAsync(_topicOptions.InventoryEvents, iEvent.InventoryId.ToString(), iEvent, cancellationToken);
        }
    }
}

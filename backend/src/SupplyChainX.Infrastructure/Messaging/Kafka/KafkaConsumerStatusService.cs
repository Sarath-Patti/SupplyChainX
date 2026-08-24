using System.Diagnostics.Metrics;
using SupplyChainX.Application.Common.Interfaces;
using SupplyChainX.Application.Common.Models;

namespace SupplyChainX.Infrastructure.Messaging.Kafka;

/// <summary>
/// Thread-safe operational metrics and state tracking service for Kafka event consumers.
/// Uses Interlocked atomic counters, lock-protected state, and System.Diagnostics.Metrics instruments.
/// </summary>
public class KafkaConsumerStatusService : IKafkaConsumerStatusService
{
    private readonly object _lock = new();

    private bool _isRunning;
    private string _consumerGroupId = string.Empty;
    private List<string> _subscribedTopics = new();
    private DateTime? _lastEventConsumedAtUtc;
    private DateTime? _lastEventProcessedAtUtc;
    private DateTime? _lastProcessingFailureAtUtc;
    private string? _lastProcessingFailureReason;

    private long _consumedCount;
    private long _processedCount;
    private long _duplicateCount;
    private long _failureCount;
    private long _retryCount;
    private long _dlqCount;
    private long _malformedCount;
    private long _dlqSuccessCount;
    private long _dlqFailureCount;

    // Standard System.Diagnostics.Metrics Meter & Instruments
    public static readonly Meter SupplyChainXMeter = new("SupplyChainX.Metrics", "1.0.0");

    private static readonly Counter<long> ConsumedCounter = SupplyChainXMeter.CreateCounter<long>("supplychainx_events_consumed_total", "events");
    private static readonly Counter<long> ProcessedCounter = SupplyChainXMeter.CreateCounter<long>("supplychainx_events_processed_total", "events");
    private static readonly Counter<long> DuplicateCounter = SupplyChainXMeter.CreateCounter<long>("supplychainx_events_duplicate_skipped_total", "events");
    private static readonly Counter<long> FailureCounter = SupplyChainXMeter.CreateCounter<long>("supplychainx_events_failed_total", "events");
    private static readonly Counter<long> RetryCounter = SupplyChainXMeter.CreateCounter<long>("supplychainx_events_retries_total", "attempts");
    private static readonly Counter<long> DlqCounter = SupplyChainXMeter.CreateCounter<long>("supplychainx_events_dlq_published_total", "events");
    private static readonly Counter<long> MalformedCounter = SupplyChainXMeter.CreateCounter<long>("supplychainx_events_malformed_total", "events");

    public KafkaConsumerStatusDto GetStatus()
    {
        lock (_lock)
        {
            return new KafkaConsumerStatusDto
            {
                IsRunning = _isRunning,
                ConsumerGroupId = _consumerGroupId,
                SubscribedTopics = _subscribedTopics.ToList(),
                LastEventConsumedAtUtc = _lastEventConsumedAtUtc,
                LastEventProcessedAtUtc = _lastEventProcessedAtUtc,
                LastProcessingFailureAtUtc = _lastProcessingFailureAtUtc,
                LastProcessingFailureReason = _lastProcessingFailureReason,
                Metrics = new KafkaMetricsDto
                {
                    ConsumedCount = Interlocked.Read(ref _consumedCount),
                    ProcessedCount = Interlocked.Read(ref _processedCount),
                    DuplicateCount = Interlocked.Read(ref _duplicateCount),
                    FailureCount = Interlocked.Read(ref _failureCount),
                    RetryCount = Interlocked.Read(ref _retryCount),
                    DlqCount = Interlocked.Read(ref _dlqCount),
                    MalformedCount = Interlocked.Read(ref _malformedCount),
                    DlqSuccessCount = Interlocked.Read(ref _dlqSuccessCount),
                    DlqFailureCount = Interlocked.Read(ref _dlqFailureCount)
                }
            };
        }
    }

    public void SetConsumerState(bool isRunning, string consumerGroupId, IEnumerable<string> topics)
    {
        lock (_lock)
        {
            _isRunning = isRunning;
            _consumerGroupId = consumerGroupId ?? string.Empty;
            _subscribedTopics = topics?.ToList() ?? new List<string>();
        }
    }

    public void RecordConsumed(string topic, Guid? eventId, string? eventType)
    {
        Interlocked.Increment(ref _consumedCount);
        ConsumedCounter.Add(1);
        lock (_lock)
        {
            _lastEventConsumedAtUtc = DateTime.UtcNow;
        }
    }

    public void RecordProcessed(string topic, Guid eventId, string eventType)
    {
        Interlocked.Increment(ref _processedCount);
        ProcessedCounter.Add(1);
        lock (_lock)
        {
            _lastEventProcessedAtUtc = DateTime.UtcNow;
        }
    }

    public void RecordDuplicate(string topic, Guid eventId, string eventType)
    {
        Interlocked.Increment(ref _duplicateCount);
        DuplicateCounter.Add(1);
    }

    public void RecordRetry(string topic, Guid eventId, string eventType, int attempt)
    {
        Interlocked.Increment(ref _retryCount);
        RetryCounter.Add(1);
    }

    public void RecordFailure(string topic, Guid eventId, string eventType, Exception ex)
    {
        Interlocked.Increment(ref _failureCount);
        FailureCounter.Add(1);
        lock (_lock)
        {
            _lastProcessingFailureAtUtc = DateTime.UtcNow;
            _lastProcessingFailureReason = ex?.Message ?? "Event processing failure";
        }
    }

    public void RecordDlqSuccess(string topic, string dlqTopic, Guid eventId, string eventType)
    {
        Interlocked.Increment(ref _dlqCount);
        Interlocked.Increment(ref _dlqSuccessCount);
        DlqCounter.Add(1);
    }

    public void RecordDlqFailure(string topic, string dlqTopic, Guid eventId, string eventType, Exception ex)
    {
        Interlocked.Increment(ref _dlqFailureCount);
        lock (_lock)
        {
            _lastProcessingFailureAtUtc = DateTime.UtcNow;
            _lastProcessingFailureReason = $"DLQ publication failed: {ex?.Message}";
        }
    }

    public void RecordMalformed(string topic, long offset)
    {
        Interlocked.Increment(ref _malformedCount);
        MalformedCounter.Add(1);
    }
}

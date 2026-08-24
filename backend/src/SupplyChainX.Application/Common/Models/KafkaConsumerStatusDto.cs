namespace SupplyChainX.Application.Common.Models;

public class KafkaConsumerStatusDto
{
    public bool IsRunning { get; set; }
    public string ConsumerGroupId { get; set; } = string.Empty;
    public List<string> SubscribedTopics { get; set; } = new();
    public DateTime? LastEventConsumedAtUtc { get; set; }
    public DateTime? LastEventProcessedAtUtc { get; set; }
    public DateTime? LastProcessingFailureAtUtc { get; set; }
    public string? LastProcessingFailureReason { get; set; }
    public KafkaMetricsDto Metrics { get; set; } = new();
}

public class KafkaMetricsDto
{
    public long ConsumedCount { get; set; }
    public long ProcessedCount { get; set; }
    public long DuplicateCount { get; set; }
    public long FailureCount { get; set; }
    public long RetryCount { get; set; }
    public long DlqCount { get; set; }
    public long MalformedCount { get; set; }
    public long DlqSuccessCount { get; set; }
    public long DlqFailureCount { get; set; }
}

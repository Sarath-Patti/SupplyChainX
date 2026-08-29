namespace SupplyChainX.Application.Common.Models;

public class BenchmarkWorkloadRequest
{
    public int EventCount { get; set; } = 30;
    public int DelayBetweenEventsMs { get; set; } = 10;
}

public class BenchmarkBurstRequest
{
    public int BurstEventCount { get; set; } = 150;
}

public class PartitionLagDto
{
    public string Topic { get; set; } = string.Empty;
    public int Partition { get; set; }
    public long HighWatermark { get; set; }
    public long CommittedOffset { get; set; }
    public long Lag { get; set; }
}

public class TopicLagDto
{
    public string Topic { get; set; } = string.Empty;
    public long TotalLag { get; set; }
    public List<PartitionLagDto> Partitions { get; set; } = new();
}

public class KafkaLagStatusDto
{
    public string ConsumerGroupId { get; set; } = string.Empty;
    public long AggregateLag { get; set; }
    public List<TopicLagDto> Topics { get; set; } = new();
    public DateTime SampledAtUtc { get; set; } = DateTime.UtcNow;
}

public class BenchmarkExecutionResultDto
{
    public string WorkloadType { get; set; } = string.Empty;
    public int EventsProduced { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime CompletedAtUtc { get; set; }
    public double TotalDurationSeconds { get; set; }
    public long PeakConsumerLag { get; set; }
    public long FinalConsumerLag { get; set; }
    public long EventsConsumed { get; set; }
    public long EventsProcessed { get; set; }
    public long Failures { get; set; }
}

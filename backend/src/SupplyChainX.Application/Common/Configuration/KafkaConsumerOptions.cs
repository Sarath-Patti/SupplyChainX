namespace SupplyChainX.Application.Common.Configuration;

public class KafkaConsumerOptions
{
    public const string SectionName = "Kafka:Consumer";

    public string ConsumerGroupId { get; set; } = "supplychainx-event-consumers";
    public bool EnableAutoCommit { get; set; } = false;
    public string AutoOffsetReset { get; set; } = "Earliest";
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
    public bool UseExponentialBackoff { get; set; } = true;
}

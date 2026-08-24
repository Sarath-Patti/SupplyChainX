namespace SupplyChainX.Application.Common.Configuration;

public class KafkaRetryOptions
{
    public const string SectionName = "Kafka:Retry";

    public int MaxRetryAttempts { get; set; } = 3;
    public int InitialDelayMs { get; set; } = 1000;
    public bool UseExponentialBackoff { get; set; } = true;
}

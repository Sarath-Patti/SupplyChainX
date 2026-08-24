using SupplyChainX.Application.Common.Models;

namespace SupplyChainX.Application.Common.Interfaces;

/// <summary>
/// Thread-safe operational metrics and state tracking interface for Kafka consumer background services.
/// Decouples operational observability from infrastructure implementation details.
/// </summary>
public interface IKafkaConsumerStatusService
{
    KafkaConsumerStatusDto GetStatus();

    void SetConsumerState(bool isRunning, string consumerGroupId, IEnumerable<string> topics);
    void RecordConsumed(string topic, Guid? eventId, string? eventType);
    void RecordProcessed(string topic, Guid eventId, string eventType);
    void RecordDuplicate(string topic, Guid eventId, string eventType);
    void RecordRetry(string topic, Guid eventId, string eventType, int attempt);
    void RecordFailure(string topic, Guid eventId, string eventType, Exception ex);
    void RecordDlqSuccess(string topic, string dlqTopic, Guid eventId, string eventType);
    void RecordDlqFailure(string topic, string dlqTopic, Guid eventId, string eventType, Exception ex);
    void RecordMalformed(string topic, long offset);
}

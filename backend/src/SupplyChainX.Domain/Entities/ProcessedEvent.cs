namespace SupplyChainX.Domain.Entities;

/// <summary>
/// Domain entity representing an event that has been processed by Kafka consumers.
/// Persisted in PostgreSQL to enforce idempotency and prevent duplicate event processing.
/// </summary>
public class ProcessedEvent
{
    public Guid EventId { get; private set; }
    public string EventType { get; private set; } = null!;
    public DateTime ProcessedAtUtc { get; private set; }

    private ProcessedEvent() { }

    public ProcessedEvent(Guid eventId, string eventType)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("EventId cannot be empty.", nameof(eventId));
        }

        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new ArgumentException("EventType cannot be empty or whitespace.", nameof(eventType));
        }

        EventId = eventId;
        EventType = eventType.Trim();
        ProcessedAtUtc = DateTime.UtcNow;
    }
}

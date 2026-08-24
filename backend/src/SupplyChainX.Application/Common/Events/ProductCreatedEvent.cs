namespace SupplyChainX.Application.Common.Events;

public record ProductCreatedEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid ProductId,
    string Sku,
    string Name,
    string? Description,
    decimal UnitPrice,
    bool IsActive
)
{
    public string EventType => nameof(ProductCreatedEvent);
    public string EventVersion => "1.0";
}

namespace SupplyChainX.Application.Common.Events;

public record ProductUpdatedEvent(
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
    public string EventType => nameof(ProductUpdatedEvent);
    public string EventVersion => "1.0";
}

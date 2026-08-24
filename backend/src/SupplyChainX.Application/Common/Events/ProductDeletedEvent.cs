namespace SupplyChainX.Application.Common.Events;

public record ProductDeletedEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid ProductId,
    string Sku
)
{
    public string EventType => nameof(ProductDeletedEvent);
    public string EventVersion => "1.0";
}

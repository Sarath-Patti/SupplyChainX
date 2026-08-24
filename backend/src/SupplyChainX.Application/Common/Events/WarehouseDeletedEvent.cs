namespace SupplyChainX.Application.Common.Events;

public record WarehouseDeletedEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid WarehouseId,
    string Name
)
{
    public string EventType => nameof(WarehouseDeletedEvent);
    public string EventVersion => "1.0";
}

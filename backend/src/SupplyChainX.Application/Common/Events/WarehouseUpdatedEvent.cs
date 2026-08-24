namespace SupplyChainX.Application.Common.Events;

public record WarehouseUpdatedEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid WarehouseId,
    string Name,
    string Location,
    bool IsActive
)
{
    public string EventType => nameof(WarehouseUpdatedEvent);
    public string EventVersion => "1.0";
}

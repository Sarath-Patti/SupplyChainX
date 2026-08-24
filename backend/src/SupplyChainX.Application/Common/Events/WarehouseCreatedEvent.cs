namespace SupplyChainX.Application.Common.Events;

public record WarehouseCreatedEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid WarehouseId,
    string Name,
    string Location,
    bool IsActive
)
{
    public string EventType => nameof(WarehouseCreatedEvent);
    public string EventVersion => "1.0";
}

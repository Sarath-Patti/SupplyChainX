namespace SupplyChainX.Application.Common.Events;

public record InventoryAdjustedEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid InventoryId,
    Guid ProductId,
    string? ProductSku,
    Guid WarehouseId,
    string? WarehouseName,
    int AvailableQuantity,
    int ReservedQuantity,
    int QuantityAdjusted,
    string AdjustmentType,
    uint Version
)
{
    public string EventType => nameof(InventoryAdjustedEvent);
    public string EventVersion => "1.0";
}

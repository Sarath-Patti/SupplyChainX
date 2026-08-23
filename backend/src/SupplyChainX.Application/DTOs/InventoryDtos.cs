namespace SupplyChainX.Application.DTOs;

public enum InventoryAdjustmentType
{
    Increase = 1,
    Decrease = 2,
    Reserve = 3,
    Release = 4
}

public record InventoryDto(
    Guid Id,
    Guid ProductId,
    string? ProductSku,
    string? ProductName,
    Guid WarehouseId,
    string? WarehouseName,
    int AvailableQuantity,
    int ReservedQuantity,
    int MinimumStockThreshold,
    uint Version,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);

public record AdjustInventoryRequest(
    Guid ProductId,
    Guid WarehouseId,
    int Quantity,
    InventoryAdjustmentType AdjustmentType
);

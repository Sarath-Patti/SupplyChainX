namespace SupplyChainX.Application.DTOs;

public record WarehouseDto(
    Guid Id,
    string Name,
    string Location,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);

public record CreateWarehouseRequest(
    string Name,
    string Location,
    bool IsActive = true
);

public record UpdateWarehouseRequest(
    string Name,
    string Location,
    bool IsActive
);

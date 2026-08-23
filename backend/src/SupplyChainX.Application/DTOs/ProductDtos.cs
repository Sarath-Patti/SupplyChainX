namespace SupplyChainX.Application.DTOs;

public record ProductDto(
    Guid Id,
    string Sku,
    string Name,
    string? Description,
    decimal UnitPrice,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);

public record CreateProductRequest(
    string Sku,
    string Name,
    string? Description,
    decimal UnitPrice,
    bool IsActive = true
);

public record UpdateProductRequest(
    string Sku,
    string Name,
    string? Description,
    decimal UnitPrice,
    bool IsActive
);

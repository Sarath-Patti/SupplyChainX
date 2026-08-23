using SupplyChainX.Application.DTOs;

namespace SupplyChainX.Application.Services;

public interface IWarehouseService
{
    Task<WarehouseDto> CreateWarehouseAsync(CreateWarehouseRequest request, CancellationToken cancellationToken = default);
    Task<WarehouseDto> GetWarehouseByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<WarehouseDto>> GetWarehousesAsync(PaginationParams paginationParams, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<WarehouseDto> UpdateWarehouseAsync(Guid id, UpdateWarehouseRequest request, CancellationToken cancellationToken = default);
    Task DeleteWarehouseAsync(Guid id, CancellationToken cancellationToken = default);
}

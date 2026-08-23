using SupplyChainX.Application.DTOs;

namespace SupplyChainX.Application.Services;

public interface IInventoryService
{
    Task<PagedResult<InventoryDto>> GetInventoryAsync(PaginationParams paginationParams, Guid? productId = null, Guid? warehouseId = null, CancellationToken cancellationToken = default);
    Task<InventoryDto> GetInventoryByProductAndWarehouseAsync(Guid productId, Guid warehouseId, CancellationToken cancellationToken = default);
    Task<InventoryDto> AdjustInventoryAsync(AdjustInventoryRequest request, CancellationToken cancellationToken = default);
}

using SupplyChainX.Application.DTOs;

namespace SupplyChainX.Application.Services;

public interface IProductService
{
    Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductDto> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<ProductDto>> GetProductsAsync(PaginationParams paginationParams, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<ProductDto> UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task DeleteProductAsync(Guid id, CancellationToken cancellationToken = default);
}

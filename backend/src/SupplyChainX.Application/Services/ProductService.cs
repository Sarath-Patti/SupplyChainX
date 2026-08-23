using Microsoft.EntityFrameworkCore;
using SupplyChainX.Application.Common.Interfaces;
using SupplyChainX.Application.DTOs;
using SupplyChainX.Domain.Entities;
using SupplyChainX.Domain.Exceptions;

namespace SupplyChainX.Application.Services;

public class ProductService : IProductService
{
    private readonly ISupplyChainXDbContext _dbContext;

    public ProductService(ISupplyChainXDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedSku = request.Sku.Trim().ToUpperInvariant();
        var skuExists = await _dbContext.Products.AnyAsync(p => p.Sku == normalizedSku, cancellationToken);
        if (skuExists)
        {
            throw new ConflictException($"Product with SKU '{normalizedSku}' already exists.");
        }

        var product = new Product(request.Sku, request.Name, request.Description, request.UnitPrice, request.IsActive);
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(product);
    }

    public async Task<ProductDto> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product == null)
        {
            throw new NotFoundException(nameof(Product), id);
        }

        return MapToDto(product);
    }

    public async Task<PagedResult<ProductDto>> GetProductsAsync(PaginationParams paginationParams, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Products.AsNoTracking().AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(paginationParams.Search))
        {
            var search = paginationParams.Search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(search) || p.Sku.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip((paginationParams.Page - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .Select(p => MapToDto(p))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductDto>(items, paginationParams.Page, paginationParams.PageSize, totalCount);
    }

    public async Task<ProductDto> UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product == null)
        {
            throw new NotFoundException(nameof(Product), id);
        }

        var normalizedSku = request.Sku.Trim().ToUpperInvariant();
        if (product.Sku != normalizedSku)
        {
            var skuExists = await _dbContext.Products.AnyAsync(p => p.Sku == normalizedSku && p.Id != id, cancellationToken);
            if (skuExists)
            {
                throw new ConflictException($"Product with SKU '{normalizedSku}' already exists.");
            }
        }

        product.Update(request.Sku, request.Name, request.Description, request.UnitPrice, request.IsActive);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(product);
    }

    public async Task DeleteProductAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product == null)
        {
            throw new NotFoundException(nameof(Product), id);
        }

        var hasInventory = await _dbContext.Inventories.AnyAsync(i => i.ProductId == id, cancellationToken);
        if (hasInventory)
        {
            throw new ConflictException($"Cannot delete product '{id}' because associated inventory records exist.");
        }

        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static ProductDto MapToDto(Product p) =>
        new(p.Id, p.Sku, p.Name, p.Description, p.UnitPrice, p.IsActive, p.CreatedAtUtc, p.UpdatedAtUtc);
}

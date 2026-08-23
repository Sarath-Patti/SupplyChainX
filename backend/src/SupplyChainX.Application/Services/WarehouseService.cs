using Microsoft.EntityFrameworkCore;
using SupplyChainX.Application.Common.Interfaces;
using SupplyChainX.Application.DTOs;
using SupplyChainX.Domain.Entities;
using SupplyChainX.Domain.Exceptions;

namespace SupplyChainX.Application.Services;

public class WarehouseService : IWarehouseService
{
    private readonly ISupplyChainXDbContext _dbContext;

    public WarehouseService(ISupplyChainXDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WarehouseDto> CreateWarehouseAsync(CreateWarehouseRequest request, CancellationToken cancellationToken = default)
    {
        var warehouse = new Warehouse(request.Name, request.Location, request.IsActive);
        _dbContext.Warehouses.Add(warehouse);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(warehouse);
    }

    public async Task<WarehouseDto> GetWarehouseByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var warehouse = await _dbContext.Warehouses.AsNoTracking().FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (warehouse == null)
        {
            throw new NotFoundException(nameof(Warehouse), id);
        }

        return MapToDto(warehouse);
    }

    public async Task<PagedResult<WarehouseDto>> GetWarehousesAsync(PaginationParams paginationParams, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Warehouses.AsNoTracking().AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(w => w.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(paginationParams.Search))
        {
            var search = paginationParams.Search.Trim().ToLower();
            query = query.Where(w => w.Name.ToLower().Contains(search) || w.Location.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(w => w.CreatedAtUtc)
            .Skip((paginationParams.Page - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .Select(w => MapToDto(w))
            .ToListAsync(cancellationToken);

        return new PagedResult<WarehouseDto>(items, paginationParams.Page, paginationParams.PageSize, totalCount);
    }

    public async Task<WarehouseDto> UpdateWarehouseAsync(Guid id, UpdateWarehouseRequest request, CancellationToken cancellationToken = default)
    {
        var warehouse = await _dbContext.Warehouses.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (warehouse == null)
        {
            throw new NotFoundException(nameof(Warehouse), id);
        }

        warehouse.Update(request.Name, request.Location, request.IsActive);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(warehouse);
    }

    public async Task DeleteWarehouseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var warehouse = await _dbContext.Warehouses.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (warehouse == null)
        {
            throw new NotFoundException(nameof(Warehouse), id);
        }

        var hasInventory = await _dbContext.Inventories.AnyAsync(i => i.WarehouseId == id, cancellationToken);
        if (hasInventory)
        {
            throw new ConflictException($"Cannot delete warehouse '{id}' because associated inventory records exist.");
        }

        _dbContext.Warehouses.Remove(warehouse);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static WarehouseDto MapToDto(Warehouse w) =>
        new(w.Id, w.Name, w.Location, w.IsActive, w.CreatedAtUtc, w.UpdatedAtUtc);
}

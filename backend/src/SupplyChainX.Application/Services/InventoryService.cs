using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SupplyChainX.Application.Common.Configuration;
using SupplyChainX.Application.Common.Events;
using SupplyChainX.Application.Common.Interfaces;
using SupplyChainX.Application.DTOs;
using SupplyChainX.Domain.Entities;
using SupplyChainX.Domain.Exceptions;

namespace SupplyChainX.Application.Services;

public class InventoryService : IInventoryService
{
    private readonly ISupplyChainXDbContext _dbContext;
    private readonly IEventPublisher _eventPublisher;
    private readonly KafkaTopicOptions _topicOptions;

    public InventoryService(
        ISupplyChainXDbContext dbContext,
        IEventPublisher eventPublisher,
        IOptions<KafkaTopicOptions> topicOptions)
    {
        _dbContext = dbContext;
        _eventPublisher = eventPublisher;
        _topicOptions = topicOptions.Value;
    }

    public async Task<PagedResult<InventoryDto>> GetInventoryAsync(PaginationParams paginationParams, Guid? productId = null, Guid? warehouseId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Inventories
            .AsNoTracking()
            .Include(i => i.Product)
            .Include(i => i.Warehouse)
            .AsQueryable();

        if (productId.HasValue && productId.Value != Guid.Empty)
        {
            query = query.Where(i => i.ProductId == productId.Value);
        }

        if (warehouseId.HasValue && warehouseId.Value != Guid.Empty)
        {
            query = query.Where(i => i.WarehouseId == warehouseId.Value);
        }

        if (!string.IsNullOrWhiteSpace(paginationParams.Search))
        {
            var search = paginationParams.Search.Trim().ToLower();
            query = query.Where(i =>
                (i.Product != null && (i.Product.Name.ToLower().Contains(search) || i.Product.Sku.ToLower().Contains(search))) ||
                (i.Warehouse != null && i.Warehouse.Name.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(i => i.CreatedAtUtc)
            .Skip((paginationParams.Page - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .Select(i => MapToDto(i))
            .ToListAsync(cancellationToken);

        return new PagedResult<InventoryDto>(items, paginationParams.Page, paginationParams.PageSize, totalCount);
    }

    public async Task<InventoryDto> GetInventoryByProductAndWarehouseAsync(Guid productId, Guid warehouseId, CancellationToken cancellationToken = default)
    {
        var inventory = await _dbContext.Inventories
            .AsNoTracking()
            .Include(i => i.Product)
            .Include(i => i.Warehouse)
            .FirstOrDefaultAsync(i => i.ProductId == productId && i.WarehouseId == warehouseId, cancellationToken);

        if (inventory == null)
        {
            throw new NotFoundException(nameof(Inventory), $"ProductId: {productId}, WarehouseId: {warehouseId}");
        }

        return MapToDto(inventory);
    }

    public async Task<InventoryDto> AdjustInventoryAsync(AdjustInventoryRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Verify Product & Warehouse exist
        var productExists = await _dbContext.Products.AnyAsync(p => p.Id == request.ProductId, cancellationToken);
        if (!productExists)
        {
            throw new NotFoundException(nameof(Product), request.ProductId);
        }

        var warehouseExists = await _dbContext.Warehouses.AnyAsync(w => w.Id == request.WarehouseId, cancellationToken);
        if (!warehouseExists)
        {
            throw new NotFoundException(nameof(Warehouse), request.WarehouseId);
        }

        // 2. Fetch or create Inventory record
        var inventory = await _dbContext.Inventories
            .FirstOrDefaultAsync(i => i.ProductId == request.ProductId && i.WarehouseId == request.WarehouseId, cancellationToken);

        if (inventory == null)
        {
            if (request.AdjustmentType != InventoryAdjustmentType.Increase)
            {
                throw new DomainException($"Cannot {request.AdjustmentType} stock for non-existent inventory record. Stock must first be increased/added.");
            }

            inventory = new Inventory(request.ProductId, request.WarehouseId, initialAvailable: 0);
            _dbContext.Inventories.Add(inventory);
        }

        // 3. Apply Domain Stock Adjustment Rule
        switch (request.AdjustmentType)
        {
            case InventoryAdjustmentType.Increase:
                inventory.IncreaseStock(request.Quantity);
                break;
            case InventoryAdjustmentType.Decrease:
                inventory.DecreaseStock(request.Quantity);
                break;
            case InventoryAdjustmentType.Reserve:
                inventory.ReserveStock(request.Quantity);
                break;
            case InventoryAdjustmentType.Release:
                inventory.ReleaseReservation(request.Quantity);
                break;
            default:
                throw new DomainException($"Unsupported inventory adjustment type: {request.AdjustmentType}");
        }

        // 4. Save Changes with Optimistic Concurrency Token
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Re-query with includes for DTO mapping and Event payload
        var updatedInventory = await _dbContext.Inventories
            .AsNoTracking()
            .Include(i => i.Product)
            .Include(i => i.Warehouse)
            .FirstAsync(i => i.Id == inventory.Id, cancellationToken);

        // Publish InventoryAdjustedEvent after successful DB persist
        var @event = new InventoryAdjustedEvent(
            EventId: Guid.NewGuid(),
            OccurredOnUtc: DateTime.UtcNow,
            InventoryId: updatedInventory.Id,
            ProductId: updatedInventory.ProductId,
            ProductSku: updatedInventory.Product?.Sku,
            WarehouseId: updatedInventory.WarehouseId,
            WarehouseName: updatedInventory.Warehouse?.Name,
            AvailableQuantity: updatedInventory.AvailableQuantity,
            ReservedQuantity: updatedInventory.ReservedQuantity,
            QuantityAdjusted: request.Quantity,
            AdjustmentType: request.AdjustmentType.ToString(),
            Version: updatedInventory.Version
        );

        await _eventPublisher.PublishAsync(_topicOptions.InventoryEvents, updatedInventory.Id.ToString(), @event, cancellationToken);

        return MapToDto(updatedInventory);
    }

    private static InventoryDto MapToDto(Inventory i) =>
        new(
            i.Id,
            i.ProductId,
            i.Product?.Sku,
            i.Product?.Name,
            i.WarehouseId,
            i.Warehouse?.Name,
            i.AvailableQuantity,
            i.ReservedQuantity,
            i.MinimumStockThreshold,
            i.Version,
            i.CreatedAtUtc,
            i.UpdatedAtUtc
        );
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SupplyChainX.Application.Common.Configuration;
using SupplyChainX.Application.Common.Events;
using SupplyChainX.Application.Common.Interfaces;
using SupplyChainX.Application.DTOs;
using SupplyChainX.Domain.Entities;
using SupplyChainX.Domain.Exceptions;

namespace SupplyChainX.Application.Services;

public class WarehouseService : IWarehouseService
{
    private readonly ISupplyChainXDbContext _dbContext;
    private readonly IEventPublisher _eventPublisher;
    private readonly KafkaTopicOptions _topicOptions;

    public WarehouseService(
        ISupplyChainXDbContext dbContext,
        IEventPublisher eventPublisher,
        IOptions<KafkaTopicOptions> topicOptions)
    {
        _dbContext = dbContext;
        _eventPublisher = eventPublisher;
        _topicOptions = topicOptions.Value;
    }

    public async Task<WarehouseDto> CreateWarehouseAsync(CreateWarehouseRequest request, CancellationToken cancellationToken = default)
    {
        var warehouse = new Warehouse(request.Name, request.Location, request.IsActive);
        _dbContext.Warehouses.Add(warehouse);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var @event = new WarehouseCreatedEvent(
            EventId: Guid.NewGuid(),
            OccurredOnUtc: DateTime.UtcNow,
            WarehouseId: warehouse.Id,
            Name: warehouse.Name,
            Location: warehouse.Location,
            IsActive: warehouse.IsActive
        );

        await _eventPublisher.PublishAsync(_topicOptions.WarehouseEvents, warehouse.Id.ToString(), @event, cancellationToken);

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

        var @event = new WarehouseUpdatedEvent(
            EventId: Guid.NewGuid(),
            OccurredOnUtc: DateTime.UtcNow,
            WarehouseId: warehouse.Id,
            Name: warehouse.Name,
            Location: warehouse.Location,
            IsActive: warehouse.IsActive
        );

        await _eventPublisher.PublishAsync(_topicOptions.WarehouseEvents, warehouse.Id.ToString(), @event, cancellationToken);

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

        var deletedName = warehouse.Name;
        _dbContext.Warehouses.Remove(warehouse);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var @event = new WarehouseDeletedEvent(
            EventId: Guid.NewGuid(),
            OccurredOnUtc: DateTime.UtcNow,
            WarehouseId: id,
            Name: deletedName
        );

        await _eventPublisher.PublishAsync(_topicOptions.WarehouseEvents, id.ToString(), @event, cancellationToken);
    }

    private static WarehouseDto MapToDto(Warehouse w) =>
        new(w.Id, w.Name, w.Location, w.IsActive, w.CreatedAtUtc, w.UpdatedAtUtc);
}

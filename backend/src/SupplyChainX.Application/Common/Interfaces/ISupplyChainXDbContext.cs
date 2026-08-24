using Microsoft.EntityFrameworkCore;
using SupplyChainX.Domain.Entities;

namespace SupplyChainX.Application.Common.Interfaces;

public interface ISupplyChainXDbContext
{
    DbSet<Product> Products { get; }
    DbSet<Warehouse> Warehouses { get; }
    DbSet<Inventory> Inventories { get; }
    DbSet<ProcessedEvent> ProcessedEvents { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

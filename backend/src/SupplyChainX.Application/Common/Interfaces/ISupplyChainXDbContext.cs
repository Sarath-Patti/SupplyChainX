using Microsoft.EntityFrameworkCore;
using SupplyChainX.Domain.Entities;

namespace SupplyChainX.Application.Common.Interfaces;

/// <summary>
/// Abstraction for database operations used by Application services without depending on Infrastructure.
/// </summary>
public interface ISupplyChainXDbContext
{
    DbSet<Product> Products { get; }
    DbSet<Warehouse> Warehouses { get; }
    DbSet<Inventory> Inventories { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

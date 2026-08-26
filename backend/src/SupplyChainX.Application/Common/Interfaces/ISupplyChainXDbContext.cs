using Microsoft.EntityFrameworkCore;
using SupplyChainX.Domain.Entities;

namespace SupplyChainX.Application.Common.Interfaces;

public interface ISupplyChainXDbContext
{
    DbSet<Product> Products { get; }
    DbSet<Warehouse> Warehouses { get; }
    DbSet<Inventory> Inventories { get; }
    DbSet<ProcessedEvent> ProcessedEvents { get; }
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

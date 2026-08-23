using Microsoft.EntityFrameworkCore;
using SupplyChainX.Application.Common.Interfaces;
using SupplyChainX.Domain.Entities;

namespace SupplyChainX.Infrastructure.Persistence;

/// <summary>
/// Infrastructure EF Core DbContext for SupplyChainX implementing Application's ISupplyChainXDbContext.
/// </summary>
public class SupplyChainXDbContext : DbContext, ISupplyChainXDbContext
{
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Warehouse> Warehouses { get; set; } = null!;
    public DbSet<Inventory> Inventories { get; set; } = null!;

    public SupplyChainXDbContext(DbContextOptions<SupplyChainXDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SupplyChainXDbContext).Assembly);
    }
}

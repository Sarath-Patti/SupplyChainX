using Microsoft.EntityFrameworkCore;

namespace SupplyChainX.Infrastructure.Persistence;

/// <summary>
/// Infrastructure EF Core DbContext baseline for SupplyChainX.
/// Business DbSets and domain entity mappings will be configured in subsequent milestones.
/// </summary>
public class SupplyChainXDbContext : DbContext
{
    public SupplyChainXDbContext(DbContextOptions<SupplyChainXDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}

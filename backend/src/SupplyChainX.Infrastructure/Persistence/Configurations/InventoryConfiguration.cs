using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupplyChainX.Domain.Entities;

namespace SupplyChainX.Infrastructure.Persistence.Configurations;

public class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        builder.ToTable("Inventories");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.ProductId)
            .IsRequired();

        builder.Property(i => i.WarehouseId)
            .IsRequired();

        // Composite Unique Index for ProductId + WarehouseId
        builder.HasIndex(i => new { i.ProductId, i.WarehouseId })
            .IsUnique();

        builder.Property(i => i.AvailableQuantity)
            .IsRequired();

        builder.Property(i => i.ReservedQuantity)
            .IsRequired();

        builder.Property(i => i.MinimumStockThreshold)
            .IsRequired();

        // Optimistic Concurrency Control Token
        builder.Property(i => i.Version)
            .IsConcurrencyToken()
            .IsRequired();

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Warehouse)
            .WithMany()
            .HasForeignKey(i => i.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupplyChainX.Domain.Entities;

namespace SupplyChainX.Infrastructure.Persistence.Configurations;

public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("Warehouses");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(w => w.Name);

        builder.Property(w => w.Location)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(w => w.IsActive)
            .IsRequired();

        builder.Property(w => w.CreatedAtUtc)
            .IsRequired();
    }
}

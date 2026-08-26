using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupplyChainX.Domain.Entities;

namespace SupplyChainX.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public static readonly Guid AdminRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid OperatorRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid ViewerRoleId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.Description)
            .HasMaxLength(250);

        builder.HasIndex(r => r.Name)
            .IsUnique();

        builder.HasData(
            new
            {
                Id = AdminRoleId,
                Name = Role.Admin,
                Description = "Full system administration access"
            },
            new
            {
                Id = OperatorRoleId,
                Name = Role.Operator,
                Description = "Product, Warehouse, and Inventory operational access"
            },
            new
            {
                Id = ViewerRoleId,
                Name = Role.Viewer,
                Description = "Read-only access across system resources"
            }
        );
    }
}

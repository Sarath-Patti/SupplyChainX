using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupplyChainX.Domain.Entities;

namespace SupplyChainX.Infrastructure.Persistence.Configurations;

public class ProcessedEventConfiguration : IEntityTypeConfiguration<ProcessedEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedEvent> builder)
    {
        builder.ToTable("ProcessedEvents");

        builder.HasKey(e => e.EventId);

        builder.Property(e => e.EventId)
            .ValueGeneratedNever();

        builder.Property(e => e.EventType)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(e => e.ProcessedAtUtc)
            .IsRequired();
    }
}

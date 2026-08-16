using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SPIP.Domain.Entities;

namespace SPIP.Infrastructure.Persistence.Configurations;

public class InvoiceProcessingLogConfiguration : IEntityTypeConfiguration<InvoiceProcessingLog>
{
    public void Configure(EntityTypeBuilder<InvoiceProcessingLog> builder)
    {
        builder.Property(l => l.EventType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(l => l.Message)
            .HasMaxLength(2000);

        builder.HasIndex(l => new { l.InvoiceId, l.CreatedAt });
    }
}

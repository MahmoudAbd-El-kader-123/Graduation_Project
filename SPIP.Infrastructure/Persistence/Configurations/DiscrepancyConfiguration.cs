using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SPIP.Domain.Entities;

namespace SPIP.Infrastructure.Persistence.Configurations;

public class DiscrepancyConfiguration : IEntityTypeConfiguration<Discrepancy>
{
    public void Configure(EntityTypeBuilder<Discrepancy> builder)
    {
        builder.Property(d => d.FieldName).HasMaxLength(100);
        builder.Property(d => d.ExpectedValue).HasMaxLength(200);
        builder.Property(d => d.ActualValue).HasMaxLength(200);
        
        builder.HasOne(d => d.Invoice)
            .WithMany(i => i.Discrepancies)
            .HasForeignKey(d => d.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.InvoiceItem)
            .WithMany()
            .HasForeignKey(d => d.InvoiceItemId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}


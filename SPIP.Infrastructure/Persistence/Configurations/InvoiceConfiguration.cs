using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SPIP.Domain.Entities;

namespace SPIP.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.Property(i => i.InvoiceNumber)
            .HasMaxLength(100);

        builder.Property(i => i.VendorName)
            .HasMaxLength(200);

        builder.Property(i => i.Currency)
            .HasMaxLength(10)
            .HasDefaultValue("USD");

        builder.Property(i => i.Subtotal)
            .HasPrecision(18, 2);

        builder.Property(i => i.Vat)
            .HasPrecision(18, 2);

        builder.Property(i => i.TotalAmount)
            .HasPrecision(18, 2);

        builder.HasIndex(i => i.UploadedByUserId);
        builder.HasIndex(i => i.Status);

        builder.HasOne(i => i.UploadedByUser)
            .WithMany()
            .HasForeignKey(i => i.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

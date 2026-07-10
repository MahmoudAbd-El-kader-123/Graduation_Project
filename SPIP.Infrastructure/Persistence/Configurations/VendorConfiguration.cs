using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SPIP.Domain.Entities;

namespace SPIP.Infrastructure.Persistence.Configurations;

public class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    public void Configure(EntityTypeBuilder<Vendor> builder)
    {
        builder.ToTable("Vendors");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(v => v.ContactEmail)
            .HasMaxLength(200);

        builder.Property(v => v.ContactPhone)
            .HasMaxLength(30);

        builder.HasMany(v => v.Products)
            .WithOne(p => p.Vendor)
            .HasForeignKey(p => p.VendorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(v => v.PurchaseOrders)
            .WithOne(po => po.Vendor)
            .HasForeignKey(po => po.VendorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(v => v.Invoices)
            .WithOne(i => i.Vendor)
            .HasForeignKey(i => i.VendorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

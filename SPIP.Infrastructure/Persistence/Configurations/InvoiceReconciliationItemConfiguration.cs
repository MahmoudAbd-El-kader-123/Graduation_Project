using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SPIP.Domain.Entities;

namespace SPIP.Infrastructure.Persistence.Configurations;

public class InvoiceReconciliationItemConfiguration : IEntityTypeConfiguration<InvoiceReconciliationItem>
{
    public void Configure(EntityTypeBuilder<InvoiceReconciliationItem> builder)
    {
        builder.Property(row => row.PurchaseOrderSku).HasMaxLength(50);
        builder.Property(row => row.InvoiceSku).HasMaxLength(50);
        builder.Property(row => row.ProductName).HasMaxLength(200);
        builder.Property(row => row.ExpectedUnitPrice).HasPrecision(18, 2);
        builder.Property(row => row.ActualUnitPrice).HasPrecision(18, 2);
        builder.Property(row => row.ExpectedAmount).HasPrecision(18, 2);
        builder.Property(row => row.ActualAmount).HasPrecision(18, 2);

        builder.HasIndex(row => row.InvoiceId);
        builder.HasIndex(row => row.PurchaseOrderItemId);
        builder.HasIndex(row => row.InvoiceItemId);

        builder.HasOne(row => row.Invoice)
            .WithMany(invoice => invoice.ReconciliationItems)
            .HasForeignKey(row => row.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(row => row.PurchaseOrderItem)
            .WithMany()
            .HasForeignKey(row => row.PurchaseOrderItemId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(row => row.InvoiceItem)
            .WithMany()
            .HasForeignKey(row => row.InvoiceItemId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

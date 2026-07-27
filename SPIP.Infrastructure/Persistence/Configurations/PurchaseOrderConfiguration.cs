using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SPIP.Domain.Entities;

namespace SPIP.Infrastructure.Persistence.Configurations;

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.Property(po => po.TotalAmount)
            .HasPrecision(18, 2);

        // Use Restrict to avoid multiple cascade paths from User:
        // User -> PurchaseOrder (cascade) -> Approval (cascade)
        // User -> Approval (already Restrict in ApprovalConfiguration)
        builder.HasOne(po => po.RequestedByUser)
            .WithMany(u => u.PurchaseOrders)
            .HasForeignKey(po => po.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

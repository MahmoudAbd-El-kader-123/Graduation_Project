using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SPIP.Domain.Entities;

namespace SPIP.Infrastructure.Persistence.Configurations;

public class ApprovalConfiguration : IEntityTypeConfiguration<Approval>
{
    public void Configure(EntityTypeBuilder<Approval> builder)
    {
        builder.HasOne(a => a.PurchaseOrder)
            .WithMany(po => po.Approvals)
            .HasForeignKey(a => a.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Use Restrict to break the multiple cascade path:
        // User -> PurchaseOrder -> Approval (cascade)
        // User -> Approval (would also cascade, causing SQL Server error 1785)
        builder.HasOne(a => a.ApproverUser)
            .WithMany(u => u.Approvals)
            .HasForeignKey(a => a.ApproverUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

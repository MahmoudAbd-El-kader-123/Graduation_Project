using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SPIP.Domain.Entities;

namespace SPIP.Infrastructure.Persistence.Configurations;

public class PurchaseOrderItemConfiguration : IEntityTypeConfiguration<PurchaseOrderItem>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderItem> builder)
    {
        builder.Property(poi => poi.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(poi => poi.LineTotal)
            .HasPrecision(18, 2);
    }
}

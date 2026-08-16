using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SPIP.Domain.Entities;

namespace SPIP.Infrastructure.Persistence.Configurations;

public class VendorPriceHistoryConfiguration : IEntityTypeConfiguration<VendorPriceHistory>
{
    public void Configure(EntityTypeBuilder<VendorPriceHistory> builder)
    {
        builder.Property(v => v.Price)
            .HasPrecision(18, 2);
    }
}

using SPIP.Domain.Common;

namespace SPIP.Domain.Entities;

public class VendorPriceHistory : BaseEntity
{
    public int VendorId { get; set; }
    public Vendor? Vendor { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public decimal Price { get; set; }
    public DateTime EffectiveDate { get; set; }
}

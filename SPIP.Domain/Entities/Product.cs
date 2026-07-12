using SPIP.Domain.Common;

namespace SPIP.Domain.Entities;

public class Product : BaseEntity
{
    public string? ErpId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string? Barcode { get; set; }
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
    public string Uom { get; set; } = "PCS";
    public int VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    public ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();
    public ICollection<VendorPriceHistory> PriceHistories { get; set; } = new List<VendorPriceHistory>();
}

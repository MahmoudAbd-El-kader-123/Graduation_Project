using SPIP.Domain.Common;

namespace SPIP.Domain.Entities;

public class Vendor : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public bool IsApproved { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<VendorPriceHistory> PriceHistories { get; set; } = new List<VendorPriceHistory>();
}

using SPIP.Domain.Common;
using SPIP.Domain.Enums;

namespace SPIP.Domain.Entities;

public class InvoiceReconciliationItem : BaseEntity
{
    public int InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    public int? PurchaseOrderItemId { get; set; }
    public PurchaseOrderItem? PurchaseOrderItem { get; set; }
    public int? InvoiceItemId { get; set; }
    public InvoiceItem? InvoiceItem { get; set; }
    public string? PurchaseOrderSku { get; set; }
    public string? InvoiceSku { get; set; }
    public string? ProductName { get; set; }
    public int? ExpectedQuantity { get; set; }
    public int? ActualQuantity { get; set; }
    public decimal? ExpectedUnitPrice { get; set; }
    public decimal? ActualUnitPrice { get; set; }
    public decimal? ExpectedAmount { get; set; }
    public decimal? ActualAmount { get; set; }
    public ReconciliationItemStatus Status { get; set; }
    public ICollection<Discrepancy> Discrepancies { get; set; } = new List<Discrepancy>();
}

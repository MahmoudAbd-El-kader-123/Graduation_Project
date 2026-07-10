using SPIP.Domain.Common;
using SPIP.Domain.Enums;

namespace SPIP.Domain.Entities;

public class Invoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public int VendorId { get; set; }
    public Vendor? Vendor { get; set; }
    public int? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;
    public decimal TotalAmount { get; set; }
    public DateTime InvoiceDate { get; set; }

    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    public ICollection<UploadedFile> UploadedFiles { get; set; } = new List<UploadedFile>();
    public ICollection<Discrepancy> Discrepancies { get; set; } = new List<Discrepancy>();
    public ICollection<AIExtractionResult> AIExtractionResults { get; set; } = new List<AIExtractionResult>();
}

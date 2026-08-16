using SPIP.Application.DTOs.Invoice;

namespace SPIP.Application.DTOs.ReconciliationReport;

public sealed class ManagerReconciliationReportDto
{
    public ReconciliationReportInvoiceDto Invoice { get; set; } = new();
    public InvoiceReconciliationDto Reconciliation { get; set; } = new();
    public string? StatusMessage { get; set; }
}

public sealed class ReconciliationReportInvoiceDto
{
    public int InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public int? PurchaseOrderId { get; set; }
    public string? PurchaseOrderNumber { get; set; }
    public string? VendorName { get; set; }
    public string? UploadedByUserEmail { get; set; }
    public DateTime UploadedAt { get; set; }
    public DateTime? InvoiceDate { get; set; }
}

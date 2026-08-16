namespace SPIP.Application.DTOs.ReconciliationReport;

public sealed class ReconciliationReportSummaryDto
{
    public int InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public int? PurchaseOrderId { get; set; }
    public string? PurchaseOrderNumber { get; set; }
    public string? VendorName { get; set; }
    public string? UploadedByUserEmail { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool HasDiscrepancies { get; set; }
    public int DiscrepancyCount { get; set; }
    public int DifferentItemCount { get; set; }
    public int MissingFromInvoiceCount { get; set; }
    public int MissingFromPurchaseOrderCount { get; set; }
    public DateTime UploadedAt { get; set; }
    public string? StatusMessage { get; set; }
}

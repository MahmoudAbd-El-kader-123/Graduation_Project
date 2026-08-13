namespace SPIP.Application.DTOs.Invoice;

public class InvoiceReconciliationDto
{
    public int InvoiceId { get; set; }
    public int? PurchaseOrderId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsReconciled { get; set; }
    public bool HasDiscrepancies { get; set; }
    public int DiscrepancyCount { get; set; }
    public List<ReconciliationItemDto> Items { get; set; } = [];
    public List<DiscrepancyDto> Discrepancies { get; set; } = [];
}

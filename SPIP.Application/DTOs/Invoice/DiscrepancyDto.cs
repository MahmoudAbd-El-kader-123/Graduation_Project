namespace SPIP.Application.DTOs.Invoice;

public class DiscrepancyDto
{
    public int Id { get; set; }
    public string DiscrepancyType { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public int? ReconciliationItemId { get; set; }
    public int? PurchaseOrderItemId { get; set; }
    public int? InvoiceItemId { get; set; }
    public string? PurchaseOrderSku { get; set; }
    public string? InvoiceSku { get; set; }
    public string? ProductName { get; set; }
    public string ExpectedValue { get; set; } = string.Empty;
    public string ActualValue { get; set; } = string.Empty;
    public bool IsResolved { get; set; }
}

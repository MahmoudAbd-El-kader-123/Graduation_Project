namespace SPIP.Application.DTOs.Invoice;

public class ReconciliationItemDto
{
    public int Id { get; set; }
    public int? PurchaseOrderItemId { get; set; }
    public int? InvoiceItemId { get; set; }
    public string? PurchaseOrderSku { get; set; }
    public string? InvoiceSku { get; set; }
    public string? ProductName { get; set; }
    public string ExpectedQuantity { get; set; } = "N/A";
    public string ActualQuantity { get; set; } = "N/A";
    public string ExpectedUnitPrice { get; set; } = "N/A";
    public string ActualUnitPrice { get; set; } = "N/A";
    public string ExpectedAmount { get; set; } = "N/A";
    public string ActualAmount { get; set; } = "N/A";
    public string Status { get; set; } = string.Empty;
    public List<DiscrepancyDto> Discrepancies { get; set; } = [];
}

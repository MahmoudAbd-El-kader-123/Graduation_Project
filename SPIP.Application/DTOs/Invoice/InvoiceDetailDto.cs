namespace SPIP.Application.DTOs.Invoice;

public class InvoiceDetailDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public int VendorId { get; set; }
    public int? PurchaseOrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal Vat { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime UploadedAt { get; set; }
    public List<InvoiceItemDto> Items { get; set; } = [];
    public List<DiscrepancyDto> Discrepancies { get; set; } = [];
    public List<InvoiceProcessingLogDto> ProcessingLogs { get; set; } = [];
}

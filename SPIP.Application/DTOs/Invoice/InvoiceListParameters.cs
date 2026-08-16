namespace SPIP.Application.DTOs.Invoice;

public class InvoiceListParameters
{
    public string? SearchTerm { get; set; }
    public string? Status { get; set; }
    public int? VendorId { get; set; }
    public int? PurchaseOrderId { get; set; }
    public bool? HasDiscrepancies { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

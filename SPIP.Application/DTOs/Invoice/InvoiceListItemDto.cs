namespace SPIP.Application.DTOs.Invoice;

public class InvoiceListItemDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime UploadedAt { get; set; }
    public string? UploadedByUserEmail { get; set; }
    public string? LastError { get; set; }
}

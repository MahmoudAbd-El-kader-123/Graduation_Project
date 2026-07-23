namespace SPIP.Application.DTOs.Invoice;

public class InvoiceListParameters
{
    public string? Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

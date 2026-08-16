namespace SPIP.Application.DTOs.Invoice;

public class InvoiceItemDto
{
    public int Id { get; set; }
    public string SupplierSku { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

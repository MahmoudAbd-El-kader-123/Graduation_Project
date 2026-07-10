namespace SPIP.Application.DTOs.Product;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public decimal UnitPrice { get; set; }
    public int VendorId { get; set; }
}

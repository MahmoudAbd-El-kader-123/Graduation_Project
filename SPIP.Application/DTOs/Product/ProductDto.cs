using SPIP.Shared.Pagination;

namespace SPIP.Application.DTOs.Product;

public class ProductDto
{
    public int Id { get; set; }
    public string? ErpId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string? Barcode { get; set; }
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
    public string Uom { get; set; } = "PCS";
    public int VendorId { get; set; }
    public string? VendorName { get; set; }
}

public class CreateProductDto
{
    public string? ErpId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string? Barcode { get; set; }
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
    public string Uom { get; set; } = "PCS";
    public int VendorId { get; set; }
}

public class ProductParameters : PaginationRequest
{
    public string? SearchTerm { get; set; }
    public int? VendorId { get; set; }
}

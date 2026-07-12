using SPIP.Shared.Pagination;

namespace SPIP.Application.DTOs.Vendor;

public class VendorDto
{
    public int Id { get; set; }
    public string? ErpId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TaxRegistrationNumber { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public bool IsApproved { get; set; }
}

public class CreateVendorDto
{
    public string? ErpId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TaxRegistrationNumber { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
}

public class VendorParameters : PaginationRequest
{
    public string? SearchTerm { get; set; }
    public bool? IsApproved { get; set; }
}

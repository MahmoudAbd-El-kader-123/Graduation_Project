namespace SPIP.Application.DTOs.Vendor;

public class VendorDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public bool IsApproved { get; set; }
}

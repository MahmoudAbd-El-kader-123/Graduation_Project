namespace SPIP.Application.DTOs.VendorColumnMapping;

public class VendorColumnMappingDto
{
    public int Id { get; set; }
    public int VendorId { get; set; }
    public string SystemField { get; set; } = string.Empty;
    public string ExcelColumn { get; set; } = string.Empty;
}

public class CreateVendorColumnMappingDto
{
    public int VendorId { get; set; }
    public string SystemField { get; set; } = string.Empty;
    public string ExcelColumn { get; set; } = string.Empty;
}

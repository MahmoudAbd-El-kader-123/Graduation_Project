using SPIP.Domain.Common;

namespace SPIP.Domain.Entities;

public class VendorColumnMapping : BaseEntity
{
    public int VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    public string SystemField { get; set; } = string.Empty;
    public string ExcelColumn { get; set; } = string.Empty;
}

using SPIP.Domain.Common;

namespace SPIP.Domain.Entities;

public class Discrepancy : BaseEntity
{
    public int InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string ExpectedValue { get; set; } = string.Empty;
    public string ActualValue { get; set; } = string.Empty;
    public bool IsResolved { get; set; }
}

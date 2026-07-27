using SPIP.Domain.Common;
using SPIP.Domain.Enums;

namespace SPIP.Domain.Entities;

public class Discrepancy : BaseEntity
{
    public int InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    public DiscrepancyType DiscrepancyType { get; set; }
    public int? InvoiceItemId { get; set; }
    public InvoiceItem? InvoiceItem { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string ExpectedValue { get; set; } = string.Empty;
    public string ActualValue { get; set; } = string.Empty;
    public bool IsResolved { get; set; }
}

using SPIP.Domain.Common;
using SPIP.Domain.Enums;

namespace SPIP.Domain.Entities;

public class InvoiceProcessingLog : BaseEntity
{
    public int InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    public InvoiceStatus? FromStatus { get; set; }
    public InvoiceStatus ToStatus { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? Message { get; set; }
}

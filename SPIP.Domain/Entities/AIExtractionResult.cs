using SPIP.Domain.Common;

namespace SPIP.Domain.Entities;

public class AIExtractionResult : BaseEntity
{
    public int InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    public string RawExtractedJson { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public string ModelUsed { get; set; } = string.Empty;
}

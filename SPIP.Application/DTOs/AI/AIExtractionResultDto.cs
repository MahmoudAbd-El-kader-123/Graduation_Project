namespace SPIP.Application.DTOs.AI;

public class AIExtractionResultDto
{
    public int InvoiceId { get; set; }
    public string RawExtractedJson { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
}

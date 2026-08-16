namespace SPIP.Application.DTOs.AI;

public class AIExtractionResponseDto
{
    public string? SchemaVersion { get; set; }
    public string? ModelUsed { get; set; }
    public double? ConfidenceScore { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public string InvoiceDate { get; set; } = string.Empty;
    public string? Currency { get; set; }
    public decimal? Subtotal { get; set; }
    public decimal? Vat { get; set; }
    public decimal Total { get; set; }
    public List<AIExtractionItemDto> Items { get; set; } = [];
}

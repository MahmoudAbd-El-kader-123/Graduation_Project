namespace SPIP.Application.DTOs.Invoice;

public class InvoiceProcessingLogDto
{
    public int Id { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; }
}

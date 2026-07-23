namespace SPIP.Application.DTOs.Invoice;

public class DiscrepancyDto
{
    public int Id { get; set; }
    public string DiscrepancyType { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public string ExpectedValue { get; set; } = string.Empty;
    public string ActualValue { get; set; } = string.Empty;
    public bool IsResolved { get; set; }
}

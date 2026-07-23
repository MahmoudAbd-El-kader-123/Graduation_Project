namespace SPIP.Application.DTOs.AI;

public class AIExtractionItemDto
{
    public string SupplierSku { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
}

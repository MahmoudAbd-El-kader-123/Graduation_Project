namespace SPIP.Application.DTOs.Approval;

public class ApprovalDto
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }
    public int ApproverUserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Comments { get; set; }
}

using SPIP.Domain.Common;
using SPIP.Domain.Enums;

namespace SPIP.Domain.Entities;

public class Approval : BaseEntity
{
    public int PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public int ApproverUserId { get; set; }
    public User? ApproverUser { get; set; }
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    public string? Comments { get; set; }

    public ICollection<ApprovalHistory> Histories { get; set; } = new List<ApprovalHistory>();
}

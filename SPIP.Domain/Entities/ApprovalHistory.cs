using SPIP.Domain.Common;
using SPIP.Domain.Enums;

namespace SPIP.Domain.Entities;

public class ApprovalHistory : BaseEntity
{
    public int ApprovalId { get; set; }
    public Approval? Approval { get; set; }
    public ApprovalStatus PreviousStatus { get; set; }
    public ApprovalStatus NewStatus { get; set; }
    public string? Notes { get; set; }
}

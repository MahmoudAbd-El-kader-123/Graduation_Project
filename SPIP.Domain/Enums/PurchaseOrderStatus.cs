namespace SPIP.Domain.Enums;

public enum PurchaseOrderStatus
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    Rejected = 4,
    Fulfilled = 5,
    Cancelled = 6
}

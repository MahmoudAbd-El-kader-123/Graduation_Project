namespace SPIP.Domain.Enums;

public enum ReconciliationItemStatus
{
    Matched = 1,
    Different = 2,
    MissingFromInvoice = 3,
    MissingFromPurchaseOrder = 4
}

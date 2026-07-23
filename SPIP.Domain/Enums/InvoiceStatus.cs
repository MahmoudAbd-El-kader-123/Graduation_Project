namespace SPIP.Domain.Enums;

public enum InvoiceStatus
{
    Uploaded = 1,
    Queued = 2,
    Processing = 3,
    Extracted = 4,
    Validated = 5,
    Compared = 6,
    Completed = 7,
    Failed = 8,
    NeedsReview = 9
}

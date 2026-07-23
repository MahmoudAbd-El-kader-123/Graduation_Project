using SPIP.Domain.Common;

namespace SPIP.Domain.Entities;

public class UploadedFile : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long FileSizeBytes { get; set; }
    public int? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
}

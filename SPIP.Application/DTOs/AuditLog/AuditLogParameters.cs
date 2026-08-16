using SPIP.Shared.Pagination;

namespace SPIP.Application.DTOs.AuditLog;

public class AuditLogParameters : PaginationRequest
{
    public string? EntityName { get; set; }
    public string? Action { get; set; }
    public int? UserId { get; set; }
}

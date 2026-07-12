using SPIP.Application.DTOs.AuditLog;
using SPIP.Shared.Pagination;
using SPIP.Shared.Result;

namespace SPIP.Application.Interfaces.Services;

public interface IAuditLogService
{
    Task<Result<PagedResult<AuditLogDto>>> GetPagedAsync(AuditLogParameters parameters);
    Task<Result<AuditLogDto>> GetByIdAsync(int id);
}

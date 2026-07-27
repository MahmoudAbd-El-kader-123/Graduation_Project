using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPIP.Application.DTOs.AuditLog;
using SPIP.Application.Interfaces.Services;
using SPIP.Domain.Constants;
using SPIP.Shared.Pagination;
using SPIP.Shared.Responses;

namespace SPIP.API.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Policy = Permissions.Reports.View)]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<AuditLogDto>>>> GetPaged([FromQuery] AuditLogParameters parameters)
    {
        var result = await _auditLogService.GetPagedAsync(parameters);
        return result.Succeeded
            ? Ok(ApiResponse<PagedResult<AuditLogDto>>.SuccessResponse(result.Data!))
            : BadRequest(ApiResponse<PagedResult<AuditLogDto>>.FailureResponse(result.Error!));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<AuditLogDto>>> GetById(int id)
    {
        var result = await _auditLogService.GetByIdAsync(id);
        return result.Succeeded
            ? Ok(ApiResponse<AuditLogDto>.SuccessResponse(result.Data!))
            : NotFound(ApiResponse<AuditLogDto>.FailureResponse(result.Error!));
    }
}

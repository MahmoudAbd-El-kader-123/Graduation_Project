using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPIP.Application.DTOs.Dashboard;
using SPIP.Application.Interfaces.Services;
using SPIP.Domain.Constants;
using SPIP.Shared.Responses;

namespace SPIP.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = Permissions.Dashboard.View)]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<DashboardStatsDto>>> GetStats()
    {
        var result = await _dashboardService.GetStatsAsync();
        return result.Succeeded
            ? Ok(ApiResponse<DashboardStatsDto>.SuccessResponse(result.Data!))
            : BadRequest(ApiResponse<DashboardStatsDto>.FailureResponse(result.Error!));
    }
}

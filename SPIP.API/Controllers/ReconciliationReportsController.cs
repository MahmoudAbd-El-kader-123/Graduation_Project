using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPIP.Application.DTOs.ReconciliationReport;
using SPIP.Application.Interfaces.Services;
using SPIP.Domain.Constants;
using SPIP.Shared.Pagination;
using SPIP.Shared.Responses;

namespace SPIP.API.Controllers;

[ApiController]
[Route("api/reconciliation-reports")]
[Authorize(Policy = Permissions.ReconciliationReports.ViewAll)]
public sealed class ReconciliationReportsController : ControllerBase
{
    private readonly IReconciliationReportService _reportService;

    public ReconciliationReportsController(IReconciliationReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ReconciliationReportSummaryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ReconciliationReportSummaryDto>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<PagedResult<ReconciliationReportSummaryDto>>>> GetPage(
        [FromQuery] ReconciliationReportParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetPageAsync(parameters, cancellationToken);
        return result.Succeeded
            ? Ok(ApiResponse<PagedResult<ReconciliationReportSummaryDto>>.SuccessResponse(result.Data!))
            : BadRequest(ApiResponse<PagedResult<ReconciliationReportSummaryDto>>.FailureResponse(
                "Invalid reconciliation report query.", result.Errors));
    }

    [HttpGet("{invoiceId:int}")]
    [ProducesResponseType(typeof(ApiResponse<ManagerReconciliationReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<ManagerReconciliationReportDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ManagerReconciliationReportDto>>> GetDetail(
        int invoiceId,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetDetailAsync(invoiceId, cancellationToken);
        return result.Succeeded
            ? Ok(ApiResponse<ManagerReconciliationReportDto>.SuccessResponse(result.Data!))
            : NotFound(ApiResponse<ManagerReconciliationReportDto>.FailureResponse(result.Error!));
    }
}

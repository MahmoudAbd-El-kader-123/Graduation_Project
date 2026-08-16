using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPIP.Application.DTOs.Invoice;
using SPIP.Application.Interfaces.Services;
using SPIP.Domain.Constants;
using SPIP.Shared.Pagination;
using SPIP.Shared.Responses;

namespace SPIP.API.Controllers;

[ApiController]
[Route("api/invoices")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;

    public InvoicesController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    /// <summary>
    /// Uploads an invoice file and queues it for background processing.
    /// </summary>
    [HttpPost("upload")]
    [Authorize(Policy = Permissions.Invoices.Upload)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceUploadResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceUploadResultDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<InvoiceUploadResultDto>>> Upload([FromForm] UploadInvoiceRequest request)
    {
        var result = await _invoiceService.UploadInvoiceAsync(request);
        return result.Succeeded
            ? Ok(ApiResponse<InvoiceUploadResultDto>.SuccessResponse(result.Data!, "Invoice uploaded and queued for processing."))
            : BadRequest(ApiResponse<InvoiceUploadResultDto>.FailureResponse(result.Error!));
    }

    /// <summary>
    /// Gets invoice details, extracted data, discrepancies, and processing history.
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = Permissions.Invoices.View)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<InvoiceDetailDto>>> GetById(int id)
    {
        var result = await _invoiceService.GetByIdAsync(id);
        if (!result.Succeeded)
        {
            if (result.Error == "Access denied.")
                return Forbid();

            return NotFound(ApiResponse<InvoiceDetailDto>.FailureResponse(result.Error!));
        }

        return Ok(ApiResponse<InvoiceDetailDto>.SuccessResponse(result.Data!));
    }

    /// <summary>
    /// Gets the reconciliation status and PO-versus-invoice discrepancies.
    /// </summary>
    [HttpGet("{id:int}/reconciliation")]
    [Authorize(Policy = Permissions.Invoices.View)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceReconciliationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceReconciliationDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<InvoiceReconciliationDto>>> GetReconciliation(int id)
    {
        var result = await _invoiceService.GetReconciliationAsync(id);
        if (!result.Succeeded)
        {
            if (result.Error == "Access denied.")
                return Forbid();

            return NotFound(ApiResponse<InvoiceReconciliationDto>.FailureResponse(result.Error!));
        }

        return Ok(ApiResponse<InvoiceReconciliationDto>.SuccessResponse(result.Data!));
    }

    /// <summary>
    /// Downloads the original uploaded invoice file.
    /// </summary>
    [HttpGet("{id:int}/download")]
    [Authorize(Policy = Permissions.Invoices.Download)]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(int id)
    {
        var result = await _invoiceService.DownloadFileAsync(id);
        if (!result.Succeeded)
        {
            if (result.Error == "Access denied.")
                return Forbid();

            return NotFound(ApiResponse<string>.FailureResponse(result.Error!));
        }

        var (stream, contentType, fileName) = result.Data!;
        return File(stream, contentType, fileName);
    }

    /// <summary>
    /// Lists invoices with optional status filtering and pagination for administrators.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = Permissions.Invoices.ViewAll)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<InvoiceListItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<InvoiceListItemDto>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<PagedResult<InvoiceListItemDto>>>> GetAll([FromQuery] InvoiceListParameters parameters)
    {
        var result = await _invoiceService.GetAllPagedAsync(parameters);
        return result.Succeeded
            ? Ok(ApiResponse<PagedResult<InvoiceListItemDto>>.SuccessResponse(result.Data!))
            : BadRequest(ApiResponse<PagedResult<InvoiceListItemDto>>.FailureResponse(result.Error!));
    }
}

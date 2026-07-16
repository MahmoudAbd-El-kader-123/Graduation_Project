using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SPIP.Application.DTOs.PurchaseOrder;
using SPIP.Application.Interfaces.Services;
using SPIP.Domain.Constants;
using SPIP.Shared.Pagination;
using SPIP.Shared.Responses;

namespace SPIP.API.Controllers;

public class ImportPurchaseOrderRequest
{
    public IFormFile File { get; set; } = null!;
    public int VendorId { get; set; }
}

[ApiController]
[Route("api/purchase-orders")]
[Authorize]
public class PurchaseOrdersController : ControllerBase
{
    private readonly IPurchaseOrderService _poService;
    private readonly IClosedXmlImportService _importService;

    public PurchaseOrdersController(IPurchaseOrderService poService, IClosedXmlImportService importService)
    {
        _poService = poService;
        _importService = importService;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.POImports.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<PurchaseOrderDto>>>> GetPaged([FromQuery] PurchaseOrderParameters parameters)
    {
        var result = await _poService.GetPagedAsync(parameters);
        return result.Succeeded
            ? Ok(ApiResponse<PagedResult<PurchaseOrderDto>>.SuccessResponse(result.Data!))
            : BadRequest(ApiResponse<PagedResult<PurchaseOrderDto>>.FailureResponse(result.Error!));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = Permissions.POImports.View)]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> GetById(int id)
    {
        var result = await _poService.GetByIdAsync(id);
        return result.Succeeded
            ? Ok(ApiResponse<PurchaseOrderDto>.SuccessResponse(result.Data!))
            : NotFound(ApiResponse<PurchaseOrderDto>.FailureResponse(result.Error!));
    }

    [HttpPost("import")]
    [Authorize(Policy = Permissions.POImports.Import)]
    public async Task<ActionResult<ApiResponse<int>>> Import([FromForm] ImportPurchaseOrderRequest request)
    {
        if (request.File == null || request.File.Length == 0)
            return BadRequest(ApiResponse<int>.FailureResponse("No file uploaded."));

        using var stream = request.File.OpenReadStream();
        var result = await _poService.ImportFromExcelAsync(stream, request.File.FileName, request.VendorId);
        return result.Succeeded
            ? Ok(ApiResponse<int>.SuccessResponse(result.Data!, "Purchase order imported successfully."))
            : BadRequest(ApiResponse<int>.FailureResponse(result.Error!));
    }

    [HttpPost("import-preview")]
    // [Authorize(Policy = Permissions.POImports.Import)] // Or a new permission if desired
    public async Task<ActionResult<ApiResponse<SPIP.Application.DTOs.Import.ExcelPreviewDto>>> ImportPreview(IFormFile file, [FromQuery] int rowsToExtract = 50)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<SPIP.Application.DTOs.Import.ExcelPreviewDto>.FailureResponse("No file uploaded."));

        using var stream = file.OpenReadStream();
        var result = await _importService.GetExcelPreviewAsync(stream, rowsToExtract);
        
        return result.Succeeded
            ? Ok(ApiResponse<SPIP.Application.DTOs.Import.ExcelPreviewDto>.SuccessResponse(result.Data!, "Preview generated successfully."))
            : BadRequest(ApiResponse<SPIP.Application.DTOs.Import.ExcelPreviewDto>.FailureResponse(result.Error!));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.POImports.Delete)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        var result = await _poService.DeleteAsync(id);
        return result.Succeeded
            ? Ok(ApiResponse<bool>.SuccessResponse(true, "Purchase order deleted successfully."))
            : BadRequest(ApiResponse<bool>.FailureResponse(result.Error!));
    }
}

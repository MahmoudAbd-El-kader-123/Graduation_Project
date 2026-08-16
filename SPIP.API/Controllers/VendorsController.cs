using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPIP.Application.DTOs.Vendor;
using SPIP.Application.Interfaces.Services;
using SPIP.Domain.Constants;
using SPIP.Shared.Pagination;
using SPIP.Shared.Responses;

namespace SPIP.API.Controllers;

[ApiController]
[Route("api/vendors")]
[Authorize]
public class VendorsController : ControllerBase
{
    private readonly IVendorService _vendorService;

    public VendorsController(IVendorService vendorService)
    {
        _vendorService = vendorService;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.Vendors.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<VendorDto>>>> GetPaged([FromQuery] VendorParameters parameters)
    {
        var result = await _vendorService.GetPagedAsync(parameters);
        return result.Succeeded
            ? Ok(ApiResponse<PagedResult<VendorDto>>.SuccessResponse(result.Data!))
            : BadRequest(ApiResponse<PagedResult<VendorDto>>.FailureResponse(result.Error!));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = Permissions.Vendors.View)]
    public async Task<ActionResult<ApiResponse<VendorDto>>> GetById(int id)
    {
        var result = await _vendorService.GetByIdAsync(id);
        return result.Succeeded
            ? Ok(ApiResponse<VendorDto>.SuccessResponse(result.Data!))
            : NotFound(ApiResponse<VendorDto>.FailureResponse(result.Error!));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Vendors.Create)]
    public async Task<ActionResult<ApiResponse<VendorDto>>> Create([FromBody] CreateVendorDto dto)
    {
        var result = await _vendorService.CreateAsync(dto);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, ApiResponse<VendorDto>.SuccessResponse(result.Data!))
            : BadRequest(ApiResponse<VendorDto>.FailureResponse(result.Error!));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.Vendors.Update)]
    public async Task<ActionResult<ApiResponse<VendorDto>>> Update(int id, [FromBody] CreateVendorDto dto)
    {
        var result = await _vendorService.UpdateAsync(id, dto);
        return result.Succeeded
            ? Ok(ApiResponse<VendorDto>.SuccessResponse(result.Data!))
            : NotFound(ApiResponse<VendorDto>.FailureResponse(result.Error!));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.Vendors.Delete)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        var result = await _vendorService.DeleteAsync(id);
        return result.Succeeded
            ? Ok(ApiResponse<bool>.SuccessResponse(true, "Vendor deleted successfully."))
            : BadRequest(ApiResponse<bool>.FailureResponse(result.Error!));
    }

    [HttpPut("{id:int}/toggle-approval")]
    [Authorize(Policy = Permissions.Vendors.Update)]
    public async Task<ActionResult<ApiResponse<bool>>> ToggleApproval(int id)
    {
        var result = await _vendorService.ToggleApprovalStatusAsync(id);
        return result.Succeeded
            ? Ok(ApiResponse<bool>.SuccessResponse(result.Data!))
            : NotFound(ApiResponse<bool>.FailureResponse(result.Error!));
    }
}

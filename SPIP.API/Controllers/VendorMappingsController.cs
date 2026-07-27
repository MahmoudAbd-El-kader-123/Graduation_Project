using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPIP.Application.DTOs.VendorColumnMapping;
using SPIP.Application.Interfaces.Services;
using SPIP.Domain.Constants;
using SPIP.Shared.Responses;

namespace SPIP.API.Controllers;

[ApiController]
[Route("api/vendor-mappings")]
[Authorize]
public class VendorMappingsController : ControllerBase
{
    private readonly IVendorColumnMappingService _mappingService;

    public VendorMappingsController(IVendorColumnMappingService mappingService)
    {
        _mappingService = mappingService;
    }

    [HttpGet("vendor/{vendorId:int}")]
    [Authorize(Policy = Permissions.VendorMappings.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<VendorColumnMappingDto>>>> GetByVendorId(int vendorId)
    {
        var result = await _mappingService.GetByVendorIdAsync(vendorId);
        return result.Succeeded
            ? Ok(ApiResponse<IReadOnlyList<VendorColumnMappingDto>>.SuccessResponse(result.Data!))
            : BadRequest(ApiResponse<IReadOnlyList<VendorColumnMappingDto>>.FailureResponse(result.Error!));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.VendorMappings.Manage)]
    public async Task<ActionResult<ApiResponse<VendorColumnMappingDto>>> CreateOrUpdate([FromBody] CreateVendorColumnMappingDto dto)
    {
        var result = await _mappingService.CreateOrUpdateAsync(dto);
        return result.Succeeded
            ? Ok(ApiResponse<VendorColumnMappingDto>.SuccessResponse(result.Data!))
            : BadRequest(ApiResponse<VendorColumnMappingDto>.FailureResponse(result.Error!));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.VendorMappings.Manage)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        var result = await _mappingService.DeleteAsync(id);
        return result.Succeeded
            ? Ok(ApiResponse<bool>.SuccessResponse(true, "Mapping deleted successfully."))
            : BadRequest(ApiResponse<bool>.FailureResponse(result.Error!));
    }
}

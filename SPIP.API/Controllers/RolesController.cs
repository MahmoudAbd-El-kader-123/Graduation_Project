using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPIP.Application.DTOs.Role;
using SPIP.Application.Interfaces.Services;
using SPIP.Domain.Constants;
using SPIP.Shared.Pagination;
using SPIP.Shared.Responses;

namespace SPIP.API.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.Roles.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<RoleDto>>>> GetPaged([FromQuery] RoleParameters parameters)
    {
        var result = await _roleService.GetPagedAsync(parameters);
        return result.Succeeded
            ? Ok(ApiResponse<PagedResult<RoleDto>>.SuccessResponse(result.Data!))
            : BadRequest(ApiResponse<PagedResult<RoleDto>>.FailureResponse(result.Error!));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.Roles.View)]
    public async Task<ActionResult<ApiResponse<RoleDto>>> GetById(Guid id)
    {
        var result = await _roleService.GetByIdAsync(id);
        return result.Succeeded
            ? Ok(ApiResponse<RoleDto>.SuccessResponse(result.Data!))
            : NotFound(ApiResponse<RoleDto>.FailureResponse(result.Error!));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Roles.Create)]
    public async Task<ActionResult<ApiResponse<RoleDto>>> Create([FromBody] CreateRoleDto dto)
    {
        var result = await _roleService.CreateAsync(dto);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, ApiResponse<RoleDto>.SuccessResponse(result.Data!))
            : BadRequest(ApiResponse<RoleDto>.FailureResponse(result.Error!));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.Roles.Update)]
    public async Task<ActionResult<ApiResponse<RoleDto>>> Update(Guid id, [FromBody] CreateRoleDto dto)
    {
        var result = await _roleService.UpdateAsync(id, dto);
        return result.Succeeded
            ? Ok(ApiResponse<RoleDto>.SuccessResponse(result.Data!))
            : NotFound(ApiResponse<RoleDto>.FailureResponse(result.Error!));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.Roles.Delete)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        var result = await _roleService.DeleteAsync(id);
        return result.Succeeded
            ? Ok(ApiResponse<bool>.SuccessResponse(true, "Role deleted successfully."))
            : BadRequest(ApiResponse<bool>.FailureResponse(result.Error!));
    }

    [HttpPost("{id:guid}/permissions")]
    [Authorize(Policy = Permissions.Roles.Update)]
    public async Task<ActionResult<ApiResponse<bool>>> AssignPermissions(Guid id, [FromBody] AssignPermissionsDto dto)
    {
        var result = await _roleService.AssignPermissionsAsync(id, dto);
        return result.Succeeded
            ? Ok(ApiResponse<bool>.SuccessResponse(true, "Permissions assigned successfully."))
            : BadRequest(ApiResponse<bool>.FailureResponse(result.Error!));
    }
}

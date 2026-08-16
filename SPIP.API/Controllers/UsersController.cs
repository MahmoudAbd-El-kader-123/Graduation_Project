using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPIP.Application.DTOs.User;
using SPIP.Application.Interfaces.Services;
using SPIP.Shared.Responses;
using SPIP.Shared.Pagination;
using SPIP.Domain.Constants;

namespace SPIP.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = Permissions.Users.View)]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetPaged([FromQuery] UserParameters parameters)
    {
        var result = await _userService.GetPagedAsync(parameters);
        return result.Succeeded
            ? Ok(ApiResponse<PagedResult<UserDto>>.SuccessResponse(result.Data!))
            : NotFound(ApiResponse<PagedResult<UserDto>>.FailureResponse(result.Error!));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetById(int id)
    {
        var result = await _userService.GetByIdAsync(id);
        return result.Succeeded
            ? Ok(ApiResponse<UserDto>.SuccessResponse(result.Data!))
            : NotFound(ApiResponse<UserDto>.FailureResponse(result.Error!));
    }



    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Update(int id, [FromBody] UpdateUserDto dto)
    {
        var result = await _userService.UpdateAsync(id, dto);
        return result.Succeeded
            ? Ok(ApiResponse<UserDto>.SuccessResponse(result.Data!))
            : NotFound(ApiResponse<UserDto>.FailureResponse(result.Error!));
    }

    [HttpPut("{id:int}/toggle-status")]
    [Authorize(Policy = Permissions.Users.Update)]
    public async Task<ActionResult<ApiResponse<bool>>> ToggleStatus(int id)
    {
        var result = await _userService.ToggleActiveStatusAsync(id);
        return result.Succeeded
            ? Ok(ApiResponse<bool>.SuccessResponse(result.Data!))
            : NotFound(ApiResponse<bool>.FailureResponse(result.Error!));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.Users.Deactivate)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        var result = await _userService.DeleteAsync(id);
        return result.Succeeded
            ? Ok(ApiResponse<bool>.SuccessResponse(true, "User deleted successfully."))
            : NotFound(ApiResponse<bool>.FailureResponse(result.Error!));
    }
}

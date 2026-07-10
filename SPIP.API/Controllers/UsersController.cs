using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPIP.Application.DTOs.User;
using SPIP.Application.Interfaces.Services;
using SPIP.Shared.Responses;

namespace SPIP.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> GetAll()
    {
        var result = await _userService.GetAllAsync();
        return result.Succeeded
            ? Ok(ApiResponse<IEnumerable<UserDto>>.SuccessResponse(result.Data!))
            : NotFound(ApiResponse<IEnumerable<UserDto>>.FailureResponse(result.Error!));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetById(int id)
    {
        var result = await _userService.GetByIdAsync(id);
        return result.Succeeded
            ? Ok(ApiResponse<UserDto>.SuccessResponse(result.Data!))
            : NotFound(ApiResponse<UserDto>.FailureResponse(result.Error!));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> Create([FromBody] CreateUserDto dto)
    {
        var result = await _userService.CreateAsync(dto);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, ApiResponse<UserDto>.SuccessResponse(result.Data!))
            : BadRequest(ApiResponse<UserDto>.FailureResponse(result.Error!));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Update(int id, [FromBody] UpdateUserDto dto)
    {
        var result = await _userService.UpdateAsync(id, dto);
        return result.Succeeded
            ? Ok(ApiResponse<UserDto>.SuccessResponse(result.Data!))
            : NotFound(ApiResponse<UserDto>.FailureResponse(result.Error!));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        var result = await _userService.DeleteAsync(id);
        return result.Succeeded
            ? Ok(ApiResponse<bool>.SuccessResponse(true, "User deleted successfully."))
            : NotFound(ApiResponse<bool>.FailureResponse(result.Error!));
    }
}

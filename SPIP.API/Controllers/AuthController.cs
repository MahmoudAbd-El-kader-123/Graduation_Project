using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SPIP.Application.DTOs.Auth;
using SPIP.Application.Interfaces.Services;
using SPIP.Shared.Responses;

namespace SPIP.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IValidator<RegisterRequestDto> _registerValidator;
    private readonly IValidator<LoginRequestDto> _loginValidator;

    public AuthController(
        IAuthService authService,
        IValidator<RegisterRequestDto> registerValidator,
        IValidator<LoginRequestDto> loginValidator)
    {
        _authService = authService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register([FromBody] RegisterRequestDto request)
    {
        var validationResult = await _registerValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage);
            return BadRequest(ApiResponse<AuthResponseDto>.FailureResponse("Validation failed.", errors));
        }

        var result = await _authService.RegisterAsync(request);
        return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(result, "User registered successfully."));
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginRequestDto request)
    {
        var validationResult = await _loginValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage);
            return BadRequest(ApiResponse<AuthResponseDto>.FailureResponse("Validation failed.", errors));
        }

        var result = await _authService.LoginAsync(request);
        return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(result, "Login successful."));
    }
}

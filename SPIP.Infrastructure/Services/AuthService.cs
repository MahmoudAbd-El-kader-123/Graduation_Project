using Microsoft.AspNetCore.Identity;
using SPIP.Application.DTOs.Auth;
using SPIP.Application.Interfaces.Services;
using SPIP.Infrastructure.Identity;
using SPIP.Infrastructure.Persistence.Context;

namespace SPIP.Infrastructure.Services;

public class AuthService : IAuthService
{
    private const string DefaultRegistrationRole = "Accountant";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ITokenService _tokenService;
    private readonly ApplicationDbContext _context;
    private readonly SPIP.Application.Common.JwtSettings _jwtSettings;

    public AuthService(
        UserManager<ApplicationUser> userManager, 
        RoleManager<IdentityRole<Guid>> roleManager, 
        ITokenService tokenService, 
        ApplicationDbContext context,
        Microsoft.Extensions.Options.IOptions<SPIP.Application.Common.JwtSettings> jwtOptions)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _context = context;
        _jwtSettings = jwtOptions.Value;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            throw new InvalidOperationException("A user with this email already exists.");
        }

        var user = new ApplicationUser
        {
            UserName = request.UserName,
            Email = request.Email,
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(" | ", createResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Registration failed: {errors}");
        }

        await _userManager.AddToRoleAsync(user, DefaultRegistrationRole);

        var roleObj = await _roleManager.FindByNameAsync(DefaultRegistrationRole);
        var roleGuid = roleObj?.Id ?? Guid.Empty;

        var domainUser = new SPIP.Domain.Entities.User
        {
            IdentityId = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            RoleId = roleGuid,
            RoleName = DefaultRegistrationRole,
            IsActive = true
        };
        _context.Users_Domain.Add(domainUser);
        await _context.SaveChangesAsync();

        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await GetPermissionsForRolesAsync(roles);
        var token = _tokenService.GenerateToken(user.Id, user.Email!, user.UserName!, roles, permissions);

        var refreshToken = _tokenService.GenerateRefreshToken();
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
        await _userManager.UpdateAsync(user);

        return new AuthResponseDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            UserName = user.UserName!,
            Email = user.Email!,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            Token = token,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            Roles = roles,
            Permissions = permissions
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await GetPermissionsForRolesAsync(roles);
        var token = _tokenService.GenerateToken(user.Id, user.Email!, user.UserName!, roles, permissions);

        var refreshToken = _tokenService.GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
        await _userManager.UpdateAsync(user);

        return new AuthResponseDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            UserName = user.UserName!,
            Email = user.Email!,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            Token = token,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            Roles = roles,
            Permissions = permissions
        };
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request)
    {
        var tokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = _jwtSettings.Audience,
            ValidateIssuer = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_jwtSettings.Key)),
            ValidateLifetime = false // CRITICAL: We allow expired tokens here!
        };

        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        
        try
        {
            // This validates the SIGNATURE to ensure a hacker didn't tamper with the expired token
            var principal = tokenHandler.ValidateToken(request.Token, tokenValidationParameters, out var securityToken);
            
            var userIdString = principal.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier || c.Type == "sub")?.Value;

            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid token claims.");
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null || user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                throw new UnauthorizedAccessException("Invalid client request.");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var permissions = await GetPermissionsForRolesAsync(roles);
            var newAccessToken = _tokenService.GenerateToken(user.Id, user.Email!, user.UserName!, roles, permissions);
            var newRefreshToken = _tokenService.GenerateRefreshToken();
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
            await _userManager.UpdateAsync(user);

            return new AuthResponseDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                UserName = user.UserName!,
                Email = user.Email!,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Token = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                Roles = roles,
                Permissions = permissions
            };
        }
        catch (Exception)
        {
            throw new UnauthorizedAccessException("Invalid or tampered token.");
        }
    }

    private async Task<IList<string>> GetPermissionsForRolesAsync(IList<string> roles)
    {
        var permissions = new List<string>();
        foreach (var roleName in roles)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                var claims = await _roleManager.GetClaimsAsync(role);
                permissions.AddRange(claims.Where(c => c.Type == "Permission").Select(c => c.Value));
            }
        }
        return permissions.Distinct().ToList();
    }
}

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

    public AuthService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole<Guid>> roleManager, ITokenService tokenService, ApplicationDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _context = context;
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

        var domainUser = new SPIP.Domain.Entities.User
        {
            IdentityId = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            Role = SPIP.Domain.Enums.UserRole.Accountant,
            IsActive = true
        };
        _context.Users_Domain.Add(domainUser);
        await _context.SaveChangesAsync();

        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await GetPermissionsForRolesAsync(roles);
        var token = _tokenService.GenerateToken(user.Id, user.Email!, user.UserName!, roles, permissions);

        return new AuthResponseDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            UserName = user.UserName!,
            Email = user.Email!,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            Roles = roles
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

        return new AuthResponseDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            UserName = user.UserName!,
            Email = user.Email!,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            Roles = roles
        };
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

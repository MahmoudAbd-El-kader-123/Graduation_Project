using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SPIP.Application.DTOs.Role;
using SPIP.Application.Interfaces.Services;
using SPIP.Shared.Pagination;
using SPIP.Shared.Result;
using System.Security.Claims;

namespace SPIP.Infrastructure.Services;

public class RoleService : IRoleService
{
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public RoleService(RoleManager<IdentityRole<Guid>> roleManager)
    {
        _roleManager = roleManager;
    }

    public async Task<Result<PagedResult<RoleDto>>> GetPagedAsync(RoleParameters p)
    {
        var query = _roleManager.Roles.AsQueryable();

        if (!string.IsNullOrWhiteSpace(p.SearchTerm))
        {
            var search = p.SearchTerm.ToLower();
            query = query.Where(r => r.Name != null && r.Name.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync();
        var roles = await query
            .OrderBy(r => r.Name)
            .Skip((p.PageNumber - 1) * p.PageSize)
            .Take(p.PageSize)
            .ToListAsync();

        var roleDtos = new List<RoleDto>();
        foreach (var role in roles)
        {
            var claims = await _roleManager.GetClaimsAsync(role);
            roleDtos.Add(new RoleDto
            {
                Id = role.Id,
                Name = role.Name!,
                Permissions = claims.Where(c => c.Type == "Permission").Select(c => c.Value).ToList()
            });
        }

        return Result<PagedResult<RoleDto>>.Success(new PagedResult<RoleDto>
        {
            Items = roleDtos,
            TotalCount = totalCount,
            PageNumber = p.PageNumber,
            PageSize = p.PageSize
        });
    }

    public async Task<Result<RoleDto>> GetByIdAsync(Guid id)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null) return Result<RoleDto>.Failure("Role not found.");

        var claims = await _roleManager.GetClaimsAsync(role);
        return Result<RoleDto>.Success(new RoleDto
        {
            Id = role.Id,
            Name = role.Name!,
            Permissions = claims.Where(c => c.Type == "Permission").Select(c => c.Value).ToList()
        });
    }

    public async Task<Result<RoleDto>> CreateAsync(CreateRoleDto dto)
    {
        var role = new IdentityRole<Guid> { Name = dto.Name };
        var result = await _roleManager.CreateAsync(role);
        
        if (!result.Succeeded)
            return Result<RoleDto>.Failure(string.Join(" | ", result.Errors.Select(e => e.Description)));

        return Result<RoleDto>.Success(new RoleDto { Id = role.Id, Name = role.Name! });
    }

    public async Task<Result<RoleDto>> UpdateAsync(Guid id, CreateRoleDto dto)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null) return Result<RoleDto>.Failure("Role not found.");

        role.Name = dto.Name;
        var result = await _roleManager.UpdateAsync(role);

        if (!result.Succeeded)
            return Result<RoleDto>.Failure(string.Join(" | ", result.Errors.Select(e => e.Description)));

        var claims = await _roleManager.GetClaimsAsync(role);
        return Result<RoleDto>.Success(new RoleDto
        {
            Id = role.Id,
            Name = role.Name!,
            Permissions = claims.Where(c => c.Type == "Permission").Select(c => c.Value).ToList()
        });
    }

    public async Task<Result<bool>> DeleteAsync(Guid id)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null) return Result<bool>.Failure("Role not found.");

        // Do not delete default roles
        if (role.Name == "Admin" || role.Name == "Manager" || role.Name == "Accountant")
        {
            return Result<bool>.Failure("Cannot delete default system roles.");
        }

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
            return Result<bool>.Failure(string.Join(" | ", result.Errors.Select(e => e.Description)));

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> AssignPermissionsAsync(Guid id, AssignPermissionsDto dto)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null) return Result<bool>.Failure("Role not found.");

        var existingClaims = await _roleManager.GetClaimsAsync(role);
        foreach (var claim in existingClaims.Where(c => c.Type == "Permission"))
        {
            await _roleManager.RemoveClaimAsync(role, claim);
        }

        foreach (var permission in dto.Permissions.Distinct())
        {
            await _roleManager.AddClaimAsync(role, new Claim("Permission", permission));
        }

        return Result<bool>.Success(true);
    }
}

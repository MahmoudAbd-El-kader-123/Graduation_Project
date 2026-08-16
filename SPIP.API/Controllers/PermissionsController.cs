using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SPIP.Application.DTOs.Permission;
using SPIP.Domain.Constants;
using SPIP.Infrastructure.Persistence.Context;
using SPIP.Shared.Responses;

namespace SPIP.API.Controllers;

[ApiController]
[Route("api/permissions")]
[Authorize] // Ensure user is logged in
public class PermissionsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PermissionsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    // Accessible by anyone with Roles.View or Roles.Manage so they can see permissions to assign
    [Authorize(Policy = Permissions.Roles.View)] 
    public async Task<ActionResult<ApiResponse<List<PermissionGroupDto>>>> GetAllGrouped()
    {
        var allPermissions = await _context.PermissionCatalogs
            .OrderBy(p => p.Module)
            .ThenBy(p => p.DisplayName)
            .ToListAsync();

        var grouped = allPermissions
            .GroupBy(p => p.Module)
            .Select(g => new PermissionGroupDto
            {
                ModuleName = g.Key,
                Permissions = g.Select(p => new PermissionDto
                {
                    Id = p.Id,
                    SystemName = p.SystemName,
                    DisplayName = p.DisplayName
                }).ToList()
            })
            .ToList();

        return Ok(ApiResponse<List<PermissionGroupDto>>.SuccessResponse(grouped));
    }
}

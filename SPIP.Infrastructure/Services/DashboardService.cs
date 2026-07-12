using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SPIP.Application.DTOs.Dashboard;
using SPIP.Application.Interfaces.Services;
using SPIP.Domain.Constants;
using SPIP.Infrastructure.Identity;
using SPIP.Infrastructure.Persistence.Context;
using SPIP.Shared.Result;
using System.Reflection;

namespace SPIP.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public DashboardService(ApplicationDbContext context, RoleManager<IdentityRole<Guid>> roleManager)
    {
        _context = context;
        _roleManager = roleManager;
    }

    public async Task<Result<DashboardStatsDto>> GetStatsAsync()
    {
        var users = await _context.Users_Domain.ToListAsync();
        var rolesCount = await _roleManager.Roles.CountAsync();

        var totalPermissions = typeof(Permissions).GetNestedTypes()
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
            .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string))
            .Count();

        var stats = new DashboardStatsDto
        {
            TotalUsers = users.Count,
            ActiveUsers = users.Count(u => u.IsActive),
            InactiveUsers = users.Count(u => !u.IsActive),
            AdminUsers = users.Count(u => u.Role == Domain.Enums.UserRole.Admin),
            ManagerUsers = users.Count(u => u.Role == Domain.Enums.UserRole.Manager),
            AccountantUsers = users.Count(u => u.Role == Domain.Enums.UserRole.Accountant),
            TotalRoles = rolesCount,
            TotalPermissions = totalPermissions
        };

        return Result<DashboardStatsDto>.Success(stats);
    }
}

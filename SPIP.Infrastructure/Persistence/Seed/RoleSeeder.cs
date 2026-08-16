using Microsoft.AspNetCore.Identity;
using SPIP.Domain.Constants;
using System.Reflection;
using System.Security.Claims;

namespace SPIP.Infrastructure.Persistence.Seed;

public static class RoleSeeder
{
    private static readonly string[] DefaultRoles = { "Admin", "Accountant", "Manager" };

    public static async Task SeedAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        foreach (var roleName in DefaultRoles)
        {
            var exists = await roleManager.RoleExistsAsync(roleName);
            if (!exists)
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            }
        }

        // Seed all permissions to Admin
        var adminRole = await roleManager.FindByNameAsync("Admin");
        if (adminRole != null)
        {
            var existingClaims = await roleManager.GetClaimsAsync(adminRole);
            var allPermissions = typeof(Permissions).GetNestedTypes()
                .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
                .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string))
                .Select(x => (string)x.GetRawConstantValue()!)
                .ToList();

            foreach (var permission in allPermissions)
            {
                if (!existingClaims.Any(c => c.Type == "Permission" && c.Value == permission))
                {
                    await roleManager.AddClaimAsync(adminRole, new Claim("Permission", permission));
                }
            }
        }

        var managerRole = await roleManager.FindByNameAsync("Manager");
        if (managerRole != null)
        {
            var managerClaims = await roleManager.GetClaimsAsync(managerRole);
            if (!managerClaims.Any(claim =>
                    claim.Type == "Permission" &&
                    claim.Value == Permissions.ReconciliationReports.ViewAll))
            {
                await roleManager.AddClaimAsync(
                    managerRole,
                    new Claim("Permission", Permissions.ReconciliationReports.ViewAll));
            }
        }
    }
}

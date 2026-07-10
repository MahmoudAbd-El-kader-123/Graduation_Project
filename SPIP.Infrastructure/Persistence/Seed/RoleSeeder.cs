using Microsoft.AspNetCore.Identity;

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
    }
}

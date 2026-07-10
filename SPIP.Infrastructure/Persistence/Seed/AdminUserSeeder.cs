using Microsoft.AspNetCore.Identity;
using SPIP.Infrastructure.Identity;

namespace SPIP.Infrastructure.Persistence.Seed;

public static class AdminUserSeeder
{
    private const string AdminEmail = "admin@spip.com";
    private const string AdminPassword = "Admin@123";

    public static async Task SeedAsync(UserManager<ApplicationUser> userManager)
    {
        var existingAdmin = await userManager.FindByEmailAsync(AdminEmail);
        if (existingAdmin is not null)
            return;

        var adminUser = new ApplicationUser
        {
            UserName = AdminEmail,
            Email = AdminEmail,
            FullName = "System Administrator",
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(adminUser, AdminPassword);

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}

using Microsoft.AspNetCore.Identity;
using SPIP.Infrastructure.Identity;
using SPIP.Infrastructure.Persistence.Context;

namespace SPIP.Infrastructure.Persistence.Seed;

public static class AdminUserSeeder
{
    private const string AdminEmail = "admin@spip.com";
    private const string AdminPassword = "Admin@123";

    public static async Task SeedAsync(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
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

            var domainUser = new SPIP.Domain.Entities.User
            {
                IdentityId = adminUser.Id,
                FullName = adminUser.FullName,
                Email = adminUser.Email,
                Role = SPIP.Domain.Enums.UserRole.Admin,
                IsActive = true
            };
            
            context.Users_Domain.Add(domainUser);
            await context.SaveChangesAsync();
        }
    }
}

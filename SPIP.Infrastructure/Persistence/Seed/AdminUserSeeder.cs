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
        
        if (existingAdmin == null)
        {
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
                existingAdmin = adminUser;
            }
        }

        if (existingAdmin != null)
        {
            var adminRole = await userManager.GetRolesAsync(existingAdmin);
            var rolesList = adminRole.ToList();
            var roleName = rolesList.FirstOrDefault() ?? "Admin";
            var roleGuid = Guid.Empty; // Ideally we fetch this from RoleManager, but Seeders might not have it injected.
            
            // To properly get RoleId, we can find it via EF context.Roles or we can inject RoleManager.
            // Since we only have context and userManager, we can lookup the role from DbContext.
            var dbRole = context.Roles.FirstOrDefault(r => r.Name == roleName);
            if (dbRole != null)
            {
                roleGuid = dbRole.Id;
            }

            var domainUserExists = context.Users_Domain.Any(u => u.IdentityId == existingAdmin.Id);
            if (!domainUserExists)
            {
                var domainUser = new SPIP.Domain.Entities.User
                {
                    IdentityId = existingAdmin.Id,
                    FullName = existingAdmin.FullName,
                    Email = existingAdmin.Email!,
                    RoleId = roleGuid,
                    RoleName = roleName,
                    IsActive = true
                };
                
                context.Users_Domain.Add(domainUser);
                await context.SaveChangesAsync();
            }
        }
    }
}

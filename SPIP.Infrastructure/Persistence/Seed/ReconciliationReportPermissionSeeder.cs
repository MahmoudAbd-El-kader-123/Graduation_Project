using Microsoft.EntityFrameworkCore;
using SPIP.Domain.Constants;
using SPIP.Domain.Entities;
using SPIP.Infrastructure.Persistence.Context;

namespace SPIP.Infrastructure.Persistence.Seed;

public static class ReconciliationReportPermissionSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        var systemName = Permissions.ReconciliationReports.ViewAll;
        if (await context.PermissionCatalogs.AnyAsync(permission => permission.SystemName == systemName))
            return;

        context.PermissionCatalogs.Add(new PermissionCatalog
        {
            Module = nameof(Permissions.ReconciliationReports),
            SystemName = systemName,
            DisplayName = "ViewAll ReconciliationReports"
        });
        await context.SaveChangesAsync();
    }
}

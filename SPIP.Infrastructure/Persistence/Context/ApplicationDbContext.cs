using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SPIP.Domain.Entities;
using SPIP.Infrastructure.Identity;
using SPIP.Domain.Constants;
using System.Reflection;

namespace SPIP.Infrastructure.Persistence.Context;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users_Domain => Set<User>();
    public DbSet<Role> Roles_Domain => Set<Role>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<VendorColumnMapping> VendorColumnMappings => Set<VendorColumnMapping>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<UploadedFile> UploadedFiles => Set<UploadedFile>();
    public DbSet<AIExtractionResult> AIExtractionResults => Set<AIExtractionResult>();
    public DbSet<Discrepancy> Discrepancies => Set<Discrepancy>();
    public DbSet<InvoiceProcessingLog> InvoiceProcessingLogs => Set<InvoiceProcessingLog>();
    public DbSet<Approval> Approvals => Set<Approval>();
    public DbSet<ApprovalHistory> ApprovalHistories => Set<ApprovalHistory>();
    public DbSet<VendorPriceHistory> VendorPriceHistories => Set<VendorPriceHistory>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<PermissionCatalog> PermissionCatalogs => Set<PermissionCatalog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        builder.Entity<PermissionCatalog>().HasData(GetPermissionSeeds());

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(Domain.Common.BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                builder.Entity(entityType.ClrType).HasQueryFilter(
                    CreateIsDeletedFilter(entityType.ClrType));
            }
        }
    }

    private static List<PermissionCatalog> GetPermissionSeeds()
    {
        var seeds = new List<PermissionCatalog>();
        int idCounter = 1;
        var permissionsType = typeof(Permissions);
        var nestedTypes = permissionsType.GetNestedTypes(BindingFlags.Public | BindingFlags.Static);

        foreach (var nestedType in nestedTypes)
        {
            var moduleName = nestedType.Name;
            var fields = nestedType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            foreach (var field in fields)
            {
                if (field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
                {
                    var systemName = (string)field.GetRawConstantValue()!;
                    var actionName = field.Name;
                    seeds.Add(new PermissionCatalog
                    {
                        Id = idCounter++,
                        Module = moduleName,
                        SystemName = systemName,
                        DisplayName = $"{actionName} {moduleName}"
                    });
                }
            }
        }
        return seeds;
    }

    private static System.Linq.Expressions.LambdaExpression CreateIsDeletedFilter(Type type)
    {
        var parameter = System.Linq.Expressions.Expression.Parameter(type, "e");
        var property = System.Linq.Expressions.Expression.Property(parameter, nameof(Domain.Common.BaseEntity.IsDeleted));
        var condition = System.Linq.Expressions.Expression.Equal(property, System.Linq.Expressions.Expression.Constant(false));
        return System.Linq.Expressions.Expression.Lambda(condition, parameter);
    }
}

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SPIP.Application.Common;
using SPIP.Application.Interfaces.AI;
using SPIP.Application.Interfaces.Repositories;
using SPIP.Application.Interfaces.Services;
using SPIP.Application.Interfaces.Storage;
using SPIP.Infrastructure.Authentication;
using SPIP.Infrastructure.Configuration;
using SPIP.Infrastructure.Identity;
using SPIP.Infrastructure.Persistence.Context;
using SPIP.Infrastructure.Repositories;
using SPIP.Infrastructure.Services;
using SPIP.Application.Services;
using System.Text;

namespace SPIP.Infrastructure.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<SPIP.Infrastructure.Persistence.Interceptors.AuditInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<SPIP.Infrastructure.Persistence.Interceptors.AuditInterceptor>();
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                   .AddInterceptors(interceptor);
        });

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization(options =>
        {
            // Dynamically register all permissions as policies
            var permissions = typeof(SPIP.Domain.Constants.Permissions).GetNestedTypes()
                .SelectMany(t => t.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy))
                .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string))
                .Select(x => (string)x.GetRawConstantValue()!)
                .ToList();

            foreach (var permission in permissions)
            {
                options.AddPolicy(permission, policy => policy.RequireClaim("Permission", permission));
            }
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IVendorRepository, VendorRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IVendorColumnMappingRepository, VendorColumnMappingRepository>();
        services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IInvoiceProcessingLogRepository, InvoiceProcessingLogRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IVendorService, VendorService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IVendorColumnMappingService, VendorColumnMappingService>();
        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
        services.AddScoped<IClosedXmlImportService, ClosedXmlImportService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IInvoiceProcessingService, InvoiceProcessingService>();
        services.AddScoped<IReconciliationService, ReconciliationService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        services.AddOptions<AIServiceSettings>()
            .Bind(configuration.GetSection(AIServiceSettings.SectionName))
            .Validate(settings =>
                    !string.IsNullOrWhiteSpace(settings.BaseUrl) &&
                    Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out _),
                "AIService:BaseUrl must be an absolute URL.")
            .Validate(settings =>
                    !string.IsNullOrWhiteSpace(settings.ExtractionEndpoint) &&
                    Uri.TryCreate(settings.ExtractionEndpoint, UriKind.Relative, out _),
                "AIService:ExtractionEndpoint must be a relative URL.")
            .Validate(settings => settings.TimeoutSeconds > 0,
                "AIService:TimeoutSeconds must be greater than zero.")
            .ValidateOnStart();

        services.AddHttpClient<IAIExtractionService, AIExtractionService>((sp, client) =>
        {
            var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AIServiceSettings>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
        });

        return services;
    }
}

using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SPIP.API.Extensions;
using SPIP.Application.DependencyInjection;
using SPIP.Infrastructure.DependencyInjection;
using SPIP.Infrastructure.Identity;
using SPIP.Infrastructure.Persistence.Context;
using SPIP.Infrastructure.Persistence.Seed;

namespace SPIP.API;

public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            var builder = WebApplication.CreateBuilder(args);

            ConfigureLogging(builder);
            ConfigureServices(builder);

            var app = builder.Build();

            ConfigurePipeline(app);
            
            await ApplyPendingMigrationsAsync(app);

            await SeedIdentityDataAsync(app);

            app.Run();
        }
        catch (Exception ex) when (ex is not HostAbortedException)
        {
            Log.Fatal(ex, "Application terminated unexpectedly during startup");
            Console.Error.WriteLine($"=== STARTUP CRASH ===");
            Console.Error.WriteLine(ex.ToString());
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
    private static async Task ApplyPendingMigrationsAsync(WebApplication app)
    {
        try
        {
            using var scope = app.Services.CreateScope();

            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var pendingMigrations = (
                await context.Database.GetPendingMigrationsAsync()
            ).ToList();

            if (!pendingMigrations.Any())
            {
                Log.Information("No pending database migrations found.");
                return;
            }

            Log.Information(
                "Applying {Count} pending database migration(s): {Migrations}",
                pendingMigrations.Count,
                string.Join(", ", pendingMigrations)
            );

            await context.Database.MigrateAsync();

            Log.Information("Database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            Log.Error(
                ex,
                "An error occurred while applying pending database migrations"
            );

            Console.Error.WriteLine("=== MIGRATION ERROR ===");
            Console.Error.WriteLine(ex);

            throw;
        }
    }

    private static async Task SeedIdentityDataAsync(WebApplication app)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var provider = scope.ServiceProvider;

            var roleManager = provider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = provider.GetRequiredService<SPIP.Infrastructure.Persistence.Context.ApplicationDbContext>();

            await RoleSeeder.SeedAsync(roleManager);
            await ReconciliationReportPermissionSeeder.SeedAsync(context);
            await AdminUserSeeder.SeedAsync(userManager, context);

            Log.Information("Identity data seeded successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while seeding identity data");
            Console.Error.WriteLine($"=== SEEDING ERROR ===");
            Console.Error.WriteLine(ex.ToString());
            throw;
        }
    }

    private static void ConfigureLogging(WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

        builder.Host.UseSerilog();
    }

    private static void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddControllers();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<SPIP.Application.Interfaces.Services.ICurrentUserService, SPIP.API.Services.CurrentUserService>();

        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);

        var hangfireConnection = builder.Configuration.GetConnectionString("HangfireConnection")
            ?? builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("HangfireConnection or DefaultConnection is not configured.");
        builder.Services.AddHangfire(config => config.UseSqlServerStorage(hangfireConnection));
        builder.Services.AddHangfireServer();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });

        builder.Services.AddSwaggerWithJwt();

        builder.Services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<Microsoft.AspNetCore.Http.HttpContext, string>(httpContext =>
                System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString(),
                    factory: partition => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 400,
                        QueueLimit = 0,
                        Window = TimeSpan.FromMinutes(1)
                    }));

            options.AddPolicy("AuthPolicy", httpContext =>
                System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString(),
                    factory: partition => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 5,
                        QueueLimit = 0,
                        Window = TimeSpan.FromMinutes(1)
                    }));

            // Dedicated AI chat rate-limit policy.
            // Partitioned by authenticated user ID (not IP) because AI calls are expensive and per-user.
            // Token bucket: allows short bursts (up to 20) while enforcing 10 req/min sustained.
            options.AddPolicy("AIChatPolicy", httpContext =>
            {
                // Use the authenticated user's NameIdentifier claim as the partition key.
                // Fall back to IP if the user is somehow unauthenticated (the [Authorize] attribute
                // prevents this in practice, but the fallback is defensive).
                var partitionKey = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                                   ?? httpContext.Connection.RemoteIpAddress?.ToString()
                                   ?? "anonymous";

                return System.Threading.RateLimiting.RateLimitPartition.GetTokenBucketLimiter(
                    partitionKey,
                    _ => new System.Threading.RateLimiting.TokenBucketRateLimiterOptions
                    {
                        TokenLimit = 20,                             // burst capacity
                        ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                        TokensPerPeriod = 10,                        // 10 req/min sustained
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });

            options.RejectionStatusCode = 429;
        });
    }

    private static void ConfigurePipeline(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseGlobalExceptionHandling();

        app.UseHttpsRedirection();

        app.UseCors("AllowFrontend");

        app.UseRateLimiter();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapHangfireDashboard("/hangfire").RequireAuthorization(SPIP.Domain.Constants.Permissions.Invoices.ViewAll);
        app.MapControllers();
    }
}

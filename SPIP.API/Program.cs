using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using SPIP.API.Extensions;
using SPIP.Application.DependencyInjection;
using SPIP.Infrastructure.DependencyInjection;
using SPIP.Infrastructure.Identity;
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

        app.MapControllers();
    }
}

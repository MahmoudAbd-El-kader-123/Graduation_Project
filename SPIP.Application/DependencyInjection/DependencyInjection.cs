using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SPIP.Application.Interfaces.Services;
using SPIP.Application.Services;

namespace SPIP.Application.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(DependencyInjection).Assembly);

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IVendorService, VendorService>();

        return services;
    }
}

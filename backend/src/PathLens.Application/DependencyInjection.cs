using Microsoft.Extensions.DependencyInjection;
using PathLens.Application.Reports;

namespace PathLens.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CoverageService>();
        return services;
    }
}

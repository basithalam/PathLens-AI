using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PathLens.Infrastructure.Health;
using PathLens.Infrastructure.Persistence;

namespace PathLens.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<PathLensDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.UseNetTopologySuite()));

        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database");

        return services;
    }
}

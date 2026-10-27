using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PathLens.Application.Reports;
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

        // NetTopologySuite/PostGIS is deferred to Phase 4: the local PostgreSQL
        // install has no PostGIS, so geometry must not enter the model yet.
        services.AddDbContext<PathLensDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database");

        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<ReportService>();

        return services;
    }
}

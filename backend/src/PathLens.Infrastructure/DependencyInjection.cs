using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PathLens.Application.Ai;
using PathLens.Application.Comparisons;
using PathLens.Application.Evidence;
using PathLens.Application.Reports;
using PathLens.Application.Storage;
using PathLens.Infrastructure.Ai;
using PathLens.Infrastructure.Analysis;
using PathLens.Infrastructure.Health;
using PathLens.Infrastructure.Persistence;
using PathLens.Infrastructure.Storage;

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

        services.AddSingleton<IObjectStorage, LocalObjectStorage>();
        services.AddScoped<IEvidenceRepository, EvidenceRepository>();
        services.AddScoped<EvidenceService>();
        services.AddScoped<IComparisonRepository, ComparisonRepository>();
        services.AddScoped<ComparisonService>();

        // Routing: real OSRM by default; MockRouting only when explicitly set
        // (tests). Provider failures surface as unavailable, never as fake routes.
        services.Configure<Routing.RoutingOptions>(configuration.GetSection(Routing.RoutingOptions.SectionName));
        services.AddHttpClient(nameof(Routing.OsrmRoutingService));
        if (string.Equals(configuration["Routing:Provider"], "Mock", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<Application.Routing.IRoutingService, Routing.MockRoutingService>();
        }
        else
        {
            services.AddScoped<Application.Routing.IRoutingService>(serviceProvider => new Routing.OsrmRoutingService(
                serviceProvider.GetRequiredService<IHttpClientFactory>(),
                serviceProvider.GetRequiredService<IOptions<Routing.RoutingOptions>>()));
        }

        services.AddScoped<Application.Routing.IRouteRecommendationRepository, RouteRecommendationRepository>();
        services.AddScoped<Application.Routing.RouteRecommendationService>();

        services.Configure<Auth.JwtOptions>(configuration.GetSection(Auth.JwtOptions.SectionName));
        services.AddScoped<Application.Users.IUserRepository, UserRepository>();
        services.AddScoped<Application.Users.IAuthService, Auth.AuthService>();
        services.AddScoped<Application.Reviews.IReviewRepository, ReviewRepository>();
        services.AddScoped<Application.Reviews.ReviewService>();

        // AI provider: real Meta by default; Mock only when Ai:Provider=Mock is
        // set explicitly (tests / labelled demo). The key stays server-side.
        services.Configure<MetaAiOptions>(configuration.GetSection(MetaAiOptions.SectionName));
        services.AddHttpClient(nameof(MetaImageAnalysisService));
        var aiProvider = configuration["Ai:Provider"];
        if (string.Equals(aiProvider, "Mock", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IImageAnalysisService, MockImageAnalysisService>();
        }
        else
        {
            services.AddScoped<IImageAnalysisService>(serviceProvider => new MetaImageAnalysisService(
                serviceProvider.GetRequiredService<IHttpClientFactory>(),
                serviceProvider.GetRequiredService<IOptions<MetaAiOptions>>()));
        }

        services.AddHostedService<AnalysisJobProcessor>();

        return services;
    }
}

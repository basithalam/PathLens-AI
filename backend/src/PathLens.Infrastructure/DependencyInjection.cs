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

        // H3.1 PostGIS dual-mode: enable NetTopologySuite translation so ST_MakePoint/
        // ST_DWithin SQL can be composed; actual extension is probed at runtime via
        // PostGisHelper (Docker PostGIS uses it, local 5433 falls back to lat/lon).
        services.AddDbContext<PathLensDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, o => o.UseNetTopologySuite());
            // Keep legacy timestamp behaviour; Npgsql 8+ warns otherwise
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        });

        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database")
            .AddCheck<OsrmHealthCheck>("osrm")
            .AddCheck<AiProviderHealthCheck>("ai-provider");

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
        services.Configure<Application.Routing.RouteEvaluationOptions>(configuration.GetSection(Application.Routing.RouteEvaluationOptions.SectionName));
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
        services.AddScoped<FeedbackService>();

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

        // SAM 3.1 obstruction measurement. Off unless Sam31:Enabled=true, so an unprobed or unbilled
        // model can never silently change what the demo shows; with it off, measurement returns
        // "not measured" and the classification stands alone.
        services.Configure<Sam31Options>(configuration.GetSection(Sam31Options.SectionName));
        services.PostConfigure<Sam31Options>(options =>
        {
            if (string.IsNullOrWhiteSpace(options.ApiKey))
            {
                options.FallbackApiKey = configuration["MetaAi:ApiKey"];
            }
        });
        services.AddHttpClient(nameof(Sam31SceneMeasurementService));
        var samEnabled = string.Equals(configuration["Sam31:Enabled"], "true", StringComparison.OrdinalIgnoreCase) &&
                         !string.Equals(aiProvider, "Mock", StringComparison.OrdinalIgnoreCase);
        if (samEnabled)
        {
            services.AddScoped<ISceneMeasurementService>(serviceProvider => new Sam31SceneMeasurementService(
                serviceProvider.GetRequiredService<IHttpClientFactory>(),
                serviceProvider.GetRequiredService<IOptions<Sam31Options>>()));
        }
        else
        {
            services.AddScoped<ISceneMeasurementService, MockSceneMeasurementService>();
        }

        services.Configure<Cameras.CameraOptions>(configuration.GetSection(Cameras.CameraOptions.SectionName));
        services.AddHttpClient(nameof(Cameras.CameraIngestionService));
        services.AddScoped<Application.Cameras.ICameraRepository, CameraRepository>();
        services.AddScoped<Application.Cameras.CameraService>();
        services.AddScoped<Cameras.CameraIngestionService>();
        services.AddScoped<Cameras.CameraEvidencePostProcessor>();
        services.AddHostedService<Cameras.CameraPollingWorker>();

        return services;
    }
}

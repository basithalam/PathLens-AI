using PathLens.Application.Reports;
using PathLens.Domain.Reports;

namespace PathLens.Api.Endpoints;

public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports").WithTags("Reports");

        group.MapPost("/", async (
            CreateReportRequest request,
            ReportService service,
            CancellationToken cancellationToken) =>
        {
            var errors = ReportRequestValidator.ValidateCreate(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors, title: "Validation failed", statusCode: 400);
            }

            var created = await service.CreateAsync(request, cancellationToken);
            return Results.Created($"/api/reports/{created.Id}", created);
        });

        group.MapGet("/", async (
            string? search,
            string? obstacleType,
            string? verificationStatus,
            int? page,
            int? pageSize,
            string? sort,
            ReportService service,
            CancellationToken cancellationToken) =>
        {
            var errors = new Dictionary<string, string[]>();
            ObstacleType? obstacle = null;
            if (!string.IsNullOrWhiteSpace(obstacleType))
            {
                if (Enum.TryParse<ObstacleType>(obstacleType, ignoreCase: true, out var parsed))
                {
                    obstacle = parsed;
                }
                else
                {
                    errors["obstacleType"] = ["Unknown obstacle type."];
                }
            }

            VerificationStatus? verification = null;
            if (!string.IsNullOrWhiteSpace(verificationStatus))
            {
                if (Enum.TryParse<VerificationStatus>(verificationStatus, ignoreCase: true, out var parsed))
                {
                    verification = parsed;
                }
                else
                {
                    errors["verificationStatus"] = ["Unknown verification status."];
                }
            }

            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors, title: "Validation failed", statusCode: 400);
            }

            var result = await service.ListAsync(
                new ReportQuery(search, obstacle, verification, page ?? 1, pageSize ?? 20, sort ?? "newest"),
                cancellationToken);
            return Results.Ok(result);
        });

        group.MapGet("/nearby", async (
            double latitude,
            double longitude,
            double? radiusMeters,
            int? limit,
            ReportService service,
            CancellationToken cancellationToken) =>
        {
            var errors = new Dictionary<string, string[]>();
            if (!AccessibilityReport.IsValidLatitude(latitude))
            {
                errors["latitude"] = ["Latitude must be between -90 and 90."];
            }

            if (!AccessibilityReport.IsValidLongitude(longitude))
            {
                errors["longitude"] = ["Longitude must be between -180 and 180."];
            }

            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors, title: "Validation failed", statusCode: 400);
            }

            var reports = await service.NearbyAsync(
                latitude, longitude, radiusMeters ?? 1000, limit ?? 50, cancellationToken);
            return Results.Ok(reports);
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            ReportService service,
            CancellationToken cancellationToken) =>
        {
            var report = await service.GetAsync(id, cancellationToken);
            return report is null
                ? Results.Problem(title: "Report not found", statusCode: 404)
                : Results.Ok(report);
        });

        group.MapPatch("/{id:guid}", async (
            Guid id,
            UpdateReportRequest request,
            ReportService service,
            CancellationToken cancellationToken) =>
        {
            var errors = ReportRequestValidator.ValidateUpdate(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors, title: "Validation failed", statusCode: 400);
            }

            var updated = await service.UpdateAsync(id, request, cancellationToken);
            return updated is null
                ? Results.Problem(title: "Report not found", statusCode: 404)
                : Results.Ok(updated);
        });

        return app;
    }
}

using PathLens.Application.Reports;
using PathLens.Domain.Reports;

namespace PathLens.Api.Endpoints;

public static class MapEndpoints
{
    public static IEndpointRouteBuilder MapMapEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/map/reports", async (
            double minLatitude,
            double maxLatitude,
            double minLongitude,
            double maxLongitude,
            string? obstacleType,
            string? verificationStatus,
            int? limit,
            ReportService service,
            CancellationToken cancellationToken) =>
        {
            var errors = new Dictionary<string, string[]>();

            if (!AccessibilityReport.IsValidLatitude(minLatitude) || !AccessibilityReport.IsValidLatitude(maxLatitude))
            {
                errors["latitude"] = ["Latitudes must be between -90 and 90."];
            }

            if (!AccessibilityReport.IsValidLongitude(minLongitude) || !AccessibilityReport.IsValidLongitude(maxLongitude))
            {
                errors["longitude"] = ["Longitudes must be between -180 and 180."];
            }

            if (minLatitude > maxLatitude)
            {
                errors["minLatitude"] = ["minLatitude must not be greater than maxLatitude."];
            }

            if (minLongitude > maxLongitude)
            {
                errors["minLongitude"] = ["minLongitude must not be greater than maxLongitude."];
            }

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

            var reports = await service.InViewportAsync(
                minLatitude, maxLatitude, minLongitude, maxLongitude,
                obstacle, verification, limit ?? 200, cancellationToken);
            return Results.Ok(reports);
        }).WithTags("Map");

        app.MapGet("/api/map/summary", async (
            ReportService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.SummaryAsync(cancellationToken)))
            .WithTags("Map");

        app.MapGet("/api/map/coverage", async (
            double minLatitude,
            double maxLatitude,
            double minLongitude,
            double maxLongitude,
            double? cellSizeMeters,
            string? obstacleType,
            string? verificationStatus,
            CoverageService service,
            CancellationToken cancellationToken) =>
        {
            var errors = new Dictionary<string, string[]>();

            if (!AccessibilityReport.IsValidLatitude(minLatitude) || !AccessibilityReport.IsValidLatitude(maxLatitude))
                errors["latitude"] = ["Latitudes must be between -90 and 90."];
            if (!AccessibilityReport.IsValidLongitude(minLongitude) || !AccessibilityReport.IsValidLongitude(maxLongitude))
                errors["longitude"] = ["Longitudes must be between -180 and 180."];
            if (minLatitude > maxLatitude)
                errors["minLatitude"] = ["minLatitude must not be greater than maxLatitude."];
            if (minLongitude > maxLongitude)
                errors["minLongitude"] = ["minLongitude must not be greater than maxLongitude."];
            if (cellSizeMeters is not null && (cellSizeMeters < 10 || cellSizeMeters > 1000))
                errors["cellSizeMeters"] = ["cellSizeMeters must be between 10 and 1000."];

            ObstacleType? obstacle = null;
            if (!string.IsNullOrWhiteSpace(obstacleType))
            {
                if (Enum.TryParse<ObstacleType>(obstacleType, true, out var parsed)) obstacle = parsed;
                else errors["obstacleType"] = ["Unknown obstacle type."];
            }
            VerificationStatus? verification = null;
            if (!string.IsNullOrWhiteSpace(verificationStatus))
            {
                if (Enum.TryParse<VerificationStatus>(verificationStatus, true, out var parsed)) verification = parsed;
                else errors["verificationStatus"] = ["Unknown verification status."];
            }

            if (errors.Count > 0)
                return Results.ValidationProblem(errors, title: "Validation failed", statusCode: 400);

            var result = await service.GetCoverageAsync(
                minLatitude, maxLatitude, minLongitude, maxLongitude,
                cellSizeMeters, obstacleType, verificationStatus, cancellationToken);
            return Results.Ok(result);
        }).WithTags("Map");

        return app;
    }
}

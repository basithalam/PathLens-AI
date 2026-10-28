using PathLens.Application.Routing;
using PathLens.Domain.Reports;

namespace PathLens.Api.Endpoints;

public static class RouteEndpoints
{
    public static IEndpointRouteBuilder MapRouteEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/routes/recommend", async (
            RecommendRouteRequest request,
            RouteRecommendationService service,
            CancellationToken cancellationToken) =>
        {
            var errors = new Dictionary<string, string[]>();

            if (!AccessibilityReport.IsValidLatitude(request.OriginLatitude) ||
                !AccessibilityReport.IsValidLatitude(request.DestinationLatitude))
            {
                errors["latitude"] = ["Latitudes must be between -90 and 90."];
            }

            if (!AccessibilityReport.IsValidLongitude(request.OriginLongitude) ||
                !AccessibilityReport.IsValidLongitude(request.DestinationLongitude))
            {
                errors["longitude"] = ["Longitudes must be between -180 and 180."];
            }

            if (request.OriginLatitude == request.DestinationLatitude &&
                request.OriginLongitude == request.DestinationLongitude)
            {
                errors["destination"] = ["Origin and destination must be different places."];
            }

            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors, title: "Validation failed", statusCode: 400);
            }

            try
            {
                var recommendation = await service.RecommendAsync(
                    new GeoPoint(request.OriginLatitude, request.OriginLongitude),
                    new GeoPoint(request.DestinationLatitude, request.DestinationLongitude),
                    cancellationToken,
                    request.Preferences);
                return Results.Created($"/api/routes/{recommendation.Id}", recommendation);
            }
            catch (RouteProviderUnavailableException ex)
            {
                return Results.Problem(
                    title: "Routing provider unavailable",
                    detail: ex.Message,
                    statusCode: 502);
            }
        }).WithTags("Routes");

        app.MapGet("/api/routes/{id:guid}", async (
            Guid id,
            RouteRecommendationService service,
            CancellationToken cancellationToken) =>
        {
            var recommendation = await service.GetAsync(id, cancellationToken);
            return recommendation is null
                ? Results.Problem(title: "Route recommendation not found", statusCode: 404)
                : Results.Ok(recommendation);
        }).WithTags("Routes");

        return app;
    }
}

using PathLens.Application.Cameras;
using PathLens.Infrastructure.Cameras;

namespace PathLens.Api.Endpoints;

public static class CameraEndpoints
{
    public const string AdminPolicy = "AdminOnly";

    public static IEndpointRouteBuilder MapCameraEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/cameras", async (
            CameraService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.ListAsync(cancellationToken)))
            .WithTags("Cameras");

        app.MapGet("/api/cameras/{id:guid}", async (
            Guid id,
            CameraService service,
            CancellationToken cancellationToken) =>
        {
            var camera = await service.GetAsync(id, cancellationToken);
            return camera is null
                ? Results.Problem(title: "Camera source not found", statusCode: 404)
                : Results.Ok(camera);
        }).WithTags("Cameras");

        app.MapPost("/api/cameras", async (
            CreateCameraRequest request,
            CameraService service,
            CancellationToken cancellationToken) =>
        {
            var errors = CameraService.ValidateCreate(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors, title: "Validation failed", statusCode: 400);
            }

            try
            {
                var created = await service.CreateAsync(request, cancellationToken);
                return Results.Created($"/api/cameras/{created.Id}", created);
            }
            catch (InvalidOperationException ex)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]> { ["permissionNote"] = [ex.Message] },
                    title: "Validation failed", statusCode: 400);
            }
        }).RequireAuthorization(AdminPolicy).WithTags("Cameras");

        app.MapPatch("/api/cameras/{id:guid}", async (
            Guid id,
            UpdateCameraRequest request,
            CameraService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var updated = await service.UpdateAsync(id, request, cancellationToken);
                return updated is null
                    ? Results.Problem(title: "Camera source not found", statusCode: 404)
                    : Results.Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]> { ["permissionNote"] = [ex.Message] },
                    title: "Validation failed", statusCode: 400);
            }
        }).RequireAuthorization(AdminPolicy).WithTags("Cameras");

        app.MapPost("/api/cameras/{id:guid}/poll", async (
            Guid id,
            ICameraRepository repository,
            CameraIngestionService ingestion,
            CancellationToken cancellationToken) =>
        {
            var source = await repository.GetAsync(id, cancellationToken);
            if (source is null)
            {
                return Results.Problem(title: "Camera source not found", statusCode: 404);
            }

            var result = await ingestion.PollSourceAsync(source, cancellationToken);
            return Results.Ok(new
            {
                outcome = result.Outcome.ToString(),
                evidenceId = result.EvidenceId,
                message = result.Message,
            });
        }).RequireAuthorization(AdminPolicy).WithTags("Cameras");

        return app;
    }
}

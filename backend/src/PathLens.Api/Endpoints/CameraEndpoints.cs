using Microsoft.Extensions.Options;
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

        app.MapPost("/api/cameras/import", async (
            string city,
            int limit,
            IHttpClientFactory httpFactory,
            IOptions<CameraOptions> camOptions,
            CameraService service,
            CancellationToken ct) =>
        {
            var key = camOptions.Value.DatumfeedApiKey;
            if (string.IsNullOrWhiteSpace(key)) return Results.Problem("Datumfeed API key not configured (Cameras:DatumfeedApiKey).", statusCode: 400);
            var safeCity = Uri.EscapeDataString(city.Trim());
            var n = Math.Clamp(limit, 1, 50);
            var url = $"https://datumfeed.com/api/cameras?city={safeCity}&limit={n}";
            var http = httpFactory.CreateClient();
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key.Trim());
            var resp = await http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode) return Results.Problem($"Datumfeed returned {(int)resp.StatusCode}.", statusCode: 502);
            var json = await resp.Content.ReadAsStringAsync(ct);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("cameras", out var arr) || arr.ValueKind != System.Text.Json.JsonValueKind.Array)
                return Results.Problem("Unexpected Datumfeed response (no cameras array).", statusCode: 502);
            int imported = 0, skipped = 0;
            foreach (var el in arr.EnumerateArray())
            {
                try
                {
                    if (!el.TryGetProperty("id", out var id) || !el.TryGetProperty("feedUrl", out var feed)) continue;
                    if (el.TryGetProperty("active", out var act) && !act.GetBoolean()) continue;
                    var lat = el.TryGetProperty("lat", out var la) ? la.GetDouble() : 0;
                    var lon = el.TryGetProperty("lon", out var lo) ? lo.GetDouble() : 0;
                    var name = el.TryGetProperty("name", out var nm) ? nm.GetString() ?? id.GetString()! : id.GetString()!;
                    var cityVal = city;
                    if (el.TryGetProperty("registry", out var reg) && reg.TryGetProperty("city", out var rc)) cityVal = rc.GetString() ?? city;
                    var prov = cityVal.ToLowerInvariant() switch { var s when s.Contains("sanfrancisco") || s.Contains("california") => "Caltrans", _ => "Datumfeed" };
                    var reqCreate = new CreateCameraRequest(prov, id.GetString()!, name, cityVal, lat, lon, feed.GetString()!, null, 60, $"Datumfeed import {DateTime.UtcNow:yyyy-MM-dd} — licensed via Datumfeed", true);
                    var v = CameraService.ValidateCreate(reqCreate); if (v.Count > 0) { skipped++; continue; }
                    await service.CreateAsync(reqCreate, ct); imported++;
                } catch (Microsoft.EntityFrameworkCore.DbUpdateException) { skipped++; } catch { skipped++; }
            }
            return Results.Ok(new { imported, skipped, city });
        }).RequireAuthorization(AdminPolicy).WithTags("Cameras");

        return app;
    }
}

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

        group.MapPost("/{id:guid}/feedback", async (
            Guid id,
            CreateFeedbackRequest request,
            HttpContext httpContext,
            FeedbackService feedbackService,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Kind))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["kind"] = ["Feedback kind is required."] }, statusCode: 400);

            var actorId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value is string sub && Guid.TryParse(sub, out var parsed) ? parsed : (Guid?)null;

            try
            {
                var result = await feedbackService.CreateAsync(id, request, actorId, cancellationToken);
                if (result is null) return Results.Problem(title: "Report not found", statusCode: 404);
                return Results.Created($"/api/reports/{id}/feedback/{result.Id}", result);
            }
            catch (ArgumentException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["kind"] = [ex.Message] }, statusCode: 400);
            }
        });

        // H4.4 Weekly feedback → AI error grouping: summary for reviewers
        group.MapGet("/feedback/summary", async (
            PathLens.Infrastructure.Persistence.PathLensDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            // Aggregate audit events where Action is Feedback* (includes WrongOrStale)
            var all = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(dbContext.ReviewAuditEvents.Where(e => e.Action.StartsWith("Feedback")), cancellationToken);

            var byKind = all
                .GroupBy(e => e.NewValue ?? "Unknown")
                .Select(g => new { kind = g.Key, count = g.Count(), action = g.First().Action })
                .OrderByDescending(x => x.count)
                .ToList();

            var wrongOrStale = all.Where(e => e.Action == "FeedbackWrongOrStale").ToList();
            // Group WrongOrStale by note keyword heuristics (wrong type vs visibility vs stale vs other)
            string classify(string? note)
            {
                if (string.IsNullOrWhiteSpace(note)) return "no-note";
                var n = note.ToLowerInvariant();
                if (n.Contains("type") || n.Contains("stairs") || n.Contains("ramp") || n.Contains("surface")) return "wrong-type";
                if (n.Contains("visible") || n.Contains("photo") || n.Contains("angle") || n.Contains("light")) return "visibility";
                if (n.Contains("old") || n.Contains("stale") || n.Contains("time") || n.Contains("change")) return "stale";
                return "other";
            }

            var wrongGroups = wrongOrStale.GroupBy(e => classify(e.Note)).Select(g => new { cause = g.Key, count = g.Count() }).ToList();

            return Results.Ok(new
            {
                totalFeedback = all.Count,
                byKind,
                wrongOrStaleTotal = wrongOrStale.Count,
                wrongOrStaleByCause = wrongGroups,
                note = "WrongOrStale is grouped weekly by cause (wrong-type vs visibility vs stale). Review notes, not counts alone, drive AI prompt/model fixes."
            });
        });

        return app;
    }
}

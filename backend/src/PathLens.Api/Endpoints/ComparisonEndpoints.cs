using PathLens.Application.Comparisons;
using PathLens.Domain.Reports;

namespace PathLens.Api.Endpoints;

public static class ComparisonEndpoints
{
    public static IEndpointRouteBuilder MapComparisonEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/comparisons", async (
            CreateComparisonRequest request,
            ComparisonService service,
            CancellationToken cancellationToken) =>
        {
            var created = await service.CreateAsync(
                request.EarlierEvidenceId, request.LaterEvidenceId, cancellationToken);

            return created.Error switch
            {
                ComparisonError.None => Results.Created(
                    $"/api/comparisons/{created.Response!.Id}", created.Response),
                ComparisonError.SelfComparison => Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        ["laterEvidenceId"] = ["An evidence item cannot be compared with itself."],
                    },
                    title: "Validation failed", statusCode: 400),
                ComparisonError.DifferentReports => Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        ["evidence"] = ["Both evidence items must belong to the same report. Evidence from different reports is not compared automatically."],
                    },
                    title: "Validation failed", statusCode: 400),
                _ => Results.Problem(title: "Evidence not found", statusCode: 404),
            };
        }).WithTags("Comparisons");

        app.MapGet("/api/comparisons/{id:guid}", async (
            Guid id,
            ComparisonService service,
            CancellationToken cancellationToken) =>
        {
            var comparison = await service.GetAsync(id, cancellationToken);
            return comparison is null
                ? Results.Problem(title: "Comparison not found", statusCode: 404)
                : Results.Ok(comparison);
        }).WithTags("Comparisons");

        app.MapGet("/api/reports/{reportId:guid}/comparisons", async (
            Guid reportId,
            ComparisonService service,
            CancellationToken cancellationToken) =>
        {
            var comparisons = await service.ListForReportAsync(reportId, cancellationToken);
            return comparisons is null
                ? Results.Problem(title: "Report not found", statusCode: 404)
                : Results.Ok(comparisons);
        }).WithTags("Comparisons");

        app.MapPost("/api/comparisons/{id:guid}/review", async (
            Guid id,
            ReviewComparisonRequest request,
            System.Security.Claims.ClaimsPrincipal principal,
            ComparisonService service,
            Application.Reviews.ReviewService reviewService,
            CancellationToken cancellationToken) =>
        {
            var actorId = AuthEndpoints.CurrentUserId(principal);
            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]> { ["reason"] = ["A review reason is required."] },
                    title: "Validation failed", statusCode: 400);
            }

            if (!Enum.TryParse<EvidenceComparisonOutcome>(request.ReviewOutcome, ignoreCase: true, out var outcome))
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        ["reviewOutcome"] =
                            [$"Outcome must be one of: {string.Join(", ", Enum.GetNames<EvidenceComparisonOutcome>())}."],
                    },
                    title: "Validation failed", statusCode: 400);
            }

            var before = await service.GetAsync(id, cancellationToken);
            var reviewed = await service.ReviewAsync(id, outcome, request.Reason, cancellationToken, actorId);
            if (reviewed is not null && actorId is not null && before is not null)
            {
                await reviewService.RecordComparisonReviewAsync(
                    actorId.Value, id, before.EffectiveOutcome, reviewed.EffectiveOutcome,
                    request.Reason, cancellationToken);
            }

            return reviewed is null
                ? Results.Problem(title: "Comparison not found", statusCode: 404)
                : Results.Ok(reviewed);
        }).RequireAuthorization(AuthEndpoints.ReviewerPolicy).WithTags("Comparisons");

        return app;
    }
}

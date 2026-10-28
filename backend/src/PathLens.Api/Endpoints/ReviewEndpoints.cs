using System.Security.Claims;
using PathLens.Application.Reviews;
using PathLens.Domain.Reports;

namespace PathLens.Api.Endpoints;

public static class ReviewEndpoints
{
    public static IEndpointRouteBuilder MapReviewEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/reports/{id:guid}/review", async (
            Guid id,
            ReviewReportRequest request,
            ClaimsPrincipal principal,
            ReviewService service,
            CancellationToken cancellationToken) =>
        {
            var actorId = AuthEndpoints.CurrentUserId(principal);
            if (actorId is null)
            {
                return Results.Problem(title: "A reviewer identity is required", statusCode: 401);
            }

            if (!Enum.TryParse<VerificationStatus>(request.VerificationStatus, ignoreCase: true, out var status) ||
                status == VerificationStatus.Unverified)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        ["verificationStatus"] = ["Verification status must be HumanVerified, Disputed, or Rejected."],
                    },
                    title: "Validation failed", statusCode: 400);
            }

            var reviewed = await service.ReviewReportAsync(
                actorId.Value, id, status, request.Note, cancellationToken);
            return reviewed is null
                ? Results.Problem(title: "Report not found", statusCode: 404)
                : Results.Ok(reviewed);
        }).RequireAuthorization(AuthEndpoints.ReviewerPolicy).WithTags("Reviews");

        app.MapGet("/api/reviewer/queue", async (
            ReviewService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetQueueAsync(cancellationToken)))
            .RequireAuthorization(AuthEndpoints.ReviewerPolicy)
            .WithTags("Reviews");

        app.MapGet("/api/reviewer/audit", async (
            int? limit,
            ReviewService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.ListRecentAuditAsync(limit ?? 50, cancellationToken)))
            .RequireAuthorization(AuthEndpoints.ReviewerPolicy)
            .WithTags("Reviews");

        app.MapGet("/api/reports/{id:guid}/reviews", async (
            Guid id,
            ReviewService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.ListAuditForEntityAsync("Report", id, cancellationToken)))
            .WithTags("Reviews");

        return app;
    }
}

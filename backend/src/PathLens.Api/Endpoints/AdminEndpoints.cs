using PathLens.Infrastructure.Persistence;

namespace PathLens.Api.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin").WithTags("Admin");

        group.MapGet("/stats", async (
            PathLensDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var now = DateTimeOffset.UtcNow;
            var totalReports = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(dbContext.Reports, cancellationToken);
            var staleCutoff = now.AddDays(-30);
            var staleReports = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(dbContext.Reports.Where(r => r.LastEvidenceAt != null && r.LastEvidenceAt < staleCutoff), cancellationToken);
            var unverified = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(dbContext.Reports.Where(r => r.VerificationStatus == PathLens.Domain.Reports.VerificationStatus.Unverified), cancellationToken);
            var verified = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(dbContext.Reports.Where(r => r.VerificationStatus == PathLens.Domain.Reports.VerificationStatus.HumanVerified), cancellationToken);
            var last7 = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(dbContext.Reports
                .Where(r => r.CreatedAt > now.AddDays(-7))
                .GroupBy(r => r.CreatedAt.Date)
                .Select(g => new { date = g.Key, count = g.Count() })
                .OrderBy(x => x.date), cancellationToken);

            var feedbackTotal = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(dbContext.ReviewAuditEvents.Where(e => e.Action.StartsWith("Feedback")), cancellationToken);
            var feedbackWrong = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(dbContext.ReviewAuditEvents.Where(e => e.Action == "FeedbackWrongOrStale"), cancellationToken);

            // Unknown% map placeholder — approximated as 100 * (1 - coverage) where coverage = reports / maxCells heuristic
            // Real coverage needs viewport; here we give city-wide heuristic
            double unknownPct = totalReports == 0 ? 100 : Math.Clamp(100 - (totalReports * 2.5), 0, 100); // heuristic

            return Results.Ok(new
            {
                totalReports,
                staleReports,
                stalePercent = totalReports == 0 ? 0 : Math.Round((double)staleReports / totalReports * 100, 1),
                unverified,
                verified,
                verifiedPercent = totalReports == 0 ? 0 : Math.Round((double)verified / totalReports * 100, 1),
                reportsPerDayLast7 = last7,
                unknownPercentMapHeuristic = Math.Round(unknownPct, 1),
                feedbackTotal,
                feedbackWrongOrStale = feedbackWrong,
                generatedAt = now,
                note = "Unknown% is heuristic (real coverage per viewport). Stale = LastEvidenceAt >30d."
            });
        }).RequireAuthorization(Endpoints.CameraEndpoints.AdminPolicy).WithTags("Admin");

        // H6.4 Incentive PoC — reporter reputation (verified count badge)
        group.MapGet("/reputation", async (
            PathLensDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var top = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(dbContext.Reports
                .Where(r => r.VerificationStatus == PathLens.Domain.Reports.VerificationStatus.HumanVerified && r.CreatedByUserId != null)
                .GroupBy(r => r.CreatedByUserId)
                .Select(g => new { userId = g.Key, verifiedCount = g.Count() })
                .OrderByDescending(x => x.verifiedCount)
                .Take(5), cancellationToken);

            // Enrich with user display name
            var userIds = top.Select(x => x.userId!.Value).ToList();
            var users = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToDictionaryAsync(dbContext.Users.Where(u => userIds.Contains(u.Id)), u => u.Id, cancellationToken);

            var result = top.Select(t => new
            {
                userId = t.userId,
                displayName = users.TryGetValue(t.userId!.Value, out var u) ? u.DisplayName : "Unknown",
                verifiedCount = t.verifiedCount,
                badge = t.verifiedCount >= 5 ? "Gold" : t.verifiedCount >= 3 ? "Silver" : t.verifiedCount >= 1 ? "Bronze" : "None",
                note = "Badge: Bronze 1+, Silver 3+, Gold 5+ verified reports. Duplicate confirm incentive: reviewer note mentions +1 reputation."
            }).ToList();

            return Results.Ok(result);
        }).RequireAuthorization(Endpoints.CameraEndpoints.AdminPolicy).WithTags("Admin");

        return app;
    }
}

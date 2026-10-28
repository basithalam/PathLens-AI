using System.Text.Json;
using PathLens.Application.Reports;
using PathLens.Domain.Routing;

namespace PathLens.Application.Routing;

public interface IRouteRecommendationRepository
{
    Task AddAsync(RouteRecommendationRecord record, CancellationToken cancellationToken);
    Task<RouteRecommendationRecord?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed class RouteRecommendationService(
    IRoutingService routingService,
    IReportRepository reportRepository,
    IRouteRecommendationRepository recommendationRepository)
{
    /// <summary>Reports closer than this to the route count as known obstacles on it.</summary>
    public const double ObstacleRadiusMeters = 50;

    /// <summary>Reports within this distance count as evidence coverage (not obstacles).</summary>
    public const double CoverageRadiusMeters = 100;

    /// <summary>Evidence older than this is called stale in explanations — never silently fresh.</summary>
    public const int StaleEvidenceDays = 30;

    public async Task<RouteRecommendationResponse> RecommendAsync(
        GeoPoint origin, GeoPoint destination, CancellationToken cancellationToken)
    {
        var providerResult = await routingService.GetRoutesAsync(origin, destination, cancellationToken);

        // Fetch reports around the whole routing area once.
        var minLat = Math.Min(origin.Latitude, destination.Latitude) - 0.01;
        var maxLat = Math.Max(origin.Latitude, destination.Latitude) + 0.01;
        var minLon = Math.Min(origin.Longitude, destination.Longitude) - 0.01;
        var maxLon = Math.Max(origin.Longitude, destination.Longitude) + 0.01;

        var areaReports = await reportRepository.InViewportAsync(
            minLat, maxLat, minLon, maxLon, null, null, 500, cancellationToken);

        var reportPoints = areaReports
            .Select(report => new GeoPoint(report.Latitude, report.Longitude))
            .ToList();

        var now = DateTimeOffset.UtcNow;

        var evaluated = providerResult.Routes.Select(candidate =>
        {
            var nearby = areaReports
                .Select(report => new
                {
                    Report = report,
                    Distance = RouteGeometry.DistanceToRouteMeters(
                        new GeoPoint(report.Latitude, report.Longitude), candidate.Geometry),
                })
                .Where(candidateReport => candidateReport.Distance <= ObstacleRadiusMeters)
                .OrderBy(candidateReport => candidateReport.Distance)
                .Select(candidateReport => new NearbyReportInfo(
                    candidateReport.Report.Id,
                    candidateReport.Report.Title,
                    candidateReport.Report.ObstacleType.ToString(),
                    candidateReport.Report.VerificationStatus.ToString(),
                    Math.Round(candidateReport.Distance, 1),
                    candidateReport.Report.LastEvidenceAt ?? candidateReport.Report.CreatedAt,
                    candidateReport.Report.Latitude,
                    candidateReport.Report.Longitude,
                    candidateReport.Report.CameraSourceId is not null))
                .ToList();

            var unknownPercent = RouteGeometry.UnknownLengthPercent(
                candidate.Geometry, reportPoints, CoverageRadiusMeters);

            DateTimeOffset? newestEvidence = nearby.Count == 0 ? null : nearby.Max(info => info.EvidenceAt);

            var explanation = new List<string>
            {
                $"Distance {candidate.DistanceMeters / 1000:0.00} km, estimated {(candidate.DurationSeconds / 60):0} minutes from the routing provider.",
            };

            if (nearby.Count == 0)
            {
                explanation.Add("No obstacle reports are known on this route. That means unknown, not clear: nobody may have reported here yet.");
            }
            else
            {
                explanation.Add($"{nearby.Count} known obstacle report(s) lie within {(int)ObstacleRadiusMeters} m of this route.");
                foreach (var obstacle in nearby.Take(3))
                {
                    var ageDays = Math.Max(0, (now - obstacle.EvidenceAt).TotalDays);
                    var ageText = ageDays < 1
                        ? "evidence less than a day old"
                        : $"evidence {ageDays:0} day(s) old";
                    if (ageDays > StaleEvidenceDays)
                    {
                        ageText += $" — stale (over {StaleEvidenceDays} days; conditions may have changed)";
                    }

                    var certainty = obstacle.VerificationStatus == "HumanVerified"
                        ? "certain (human-verified)"
                        : obstacle.IsCameraCandidate
                            ? "uncertain (camera-detected candidate, not human-verified)"
                            : "uncertain (not human-verified)";

                    explanation.Add(
                        $"- {obstacle.Title} ({obstacle.ObstacleType}, {obstacle.VerificationStatus}) about {obstacle.DistanceMeters:0} m from the route; {ageText}; {certainty}.");
                }

                var certainCount = nearby.Count(info => info.VerificationStatus == "HumanVerified");
                var staleCount = nearby.Count(info => (now - info.EvidenceAt).TotalDays > StaleEvidenceDays);
                explanation.Add(
                    $"Certainty split for this route: {certainCount} certain (human-verified) obstacle(s), " +
                    $"{nearby.Count - certainCount} uncertain (unverified, AI-only, disputed, or camera-detected). " +
                    (staleCount > 0
                        ? $"{staleCount} obstacle(s) rely on stale evidence (over {StaleEvidenceDays} days old)."
                        : "No obstacle here relies on stale evidence."));
            }

            explanation.Add(unknownPercent >= 99
                ? "Almost none of this route has nearby evidence coverage, so most of it is unknown."
                : $"About {unknownPercent:0}% of this route has no report within {(int)CoverageRadiusMeters} m and is therefore unknown, not proven clear.");

            if (newestEvidence is not null)
            {
                var ageDays = Math.Max(0, (now - newestEvidence.Value).TotalDays);
                explanation.Add(ageDays < 1
                    ? "The newest nearby evidence is less than a day old."
                    : $"The newest nearby evidence is about {ageDays:0} day(s) old; conditions may have changed since.");
            }

            return new EvaluatedRoute(
                0,
                candidate.Geometry,
                candidate.DistanceMeters,
                candidate.DurationSeconds,
                nearby,
                unknownPercent,
                newestEvidence,
                explanation);
        }).ToList();

        // Explainable ranking: fewest known obstacles, then fewest
        // human-verified obstacles, then shortest distance. Never "safest".
        var ranked = evaluated
            .OrderBy(route => route.KnownObstacles.Count)
            .ThenBy(route => route.KnownObstacles.Count(info => info.VerificationStatus == "HumanVerified"))
            .ThenBy(route => route.DistanceMeters)
            .Select((route, index) => route with { Index = index })
            .ToList();

        if (ranked.Count > 1)
        {
            ranked[0] = ranked[0] with
            {
                Explanation = ranked[0].Explanation
                    .Prepend("Ranked first because it has the fewest known obstacle reports among the candidates — not because it is proven safe.")
                    .ToList(),
            };

            // Say, for every neighbouring pair, exactly which ranking key
            // separated them — obstacle count, verified count, or distance.
            for (var i = 0; i < ranked.Count - 1; i++)
            {
                var above = ranked[i];
                var below = ranked[i + 1];
                var (aboveLine, belowLine) = RankingReason(above, below);

                ranked[i] = above with { Explanation = above.Explanation.Append(aboveLine).ToList() };
                ranked[i + 1] = below with { Explanation = below.Explanation.Append(belowLine).ToList() };
            }
        }

        var responseId = Guid.NewGuid();
        var response = new RouteRecommendationResponse(
            responseId,
            providerResult.Provider,
            providerResult.Profile,
            origin,
            destination,
            ranked,
            [
                "Routes use a walking profile. Wheelchair suitability of the routing profile itself has not been validated, so no route here is labelled wheelchair-safe.",
                "A route with no obstacle reports is unknown, not clear.",
                "Report locations are user-supplied claims and may be inaccurate.",
            ],
            now);

        var record = RouteRecommendationRecord.Create(
            responseId,
            origin.Latitude, origin.Longitude,
            destination.Latitude, destination.Longitude,
            providerResult.Profile,
            providerResult.Provider,
            JsonSerializer.Serialize(response),
            now);

        await recommendationRepository.AddAsync(record, cancellationToken);
        await recommendationRepository.SaveChangesAsync(cancellationToken);

        return response;
    }

    private static (string AboveLine, string BelowLine) RankingReason(EvaluatedRoute above, EvaluatedRoute below)
    {
        var aboveName = $"Route {above.Index + 1}";
        var belowName = $"Route {below.Index + 1}";

        if (below.KnownObstacles.Count != above.KnownObstacles.Count)
        {
            return (
                $"{aboveName} ranks above {belowName} because it has fewer known obstacle reports " +
                $"({above.KnownObstacles.Count} vs {below.KnownObstacles.Count}). Fewer known obstacles is not proof of safety.",
                $"{belowName} ranks below {aboveName} because it has more known obstacle reports " +
                $"({below.KnownObstacles.Count} vs {above.KnownObstacles.Count}).");
        }

        var aboveVerified = above.KnownObstacles.Count(info => info.VerificationStatus == "HumanVerified");
        var belowVerified = below.KnownObstacles.Count(info => info.VerificationStatus == "HumanVerified");
        if (belowVerified != aboveVerified)
        {
            return (
                $"{aboveName} ranks above {belowName} because both have {above.KnownObstacles.Count} known obstacle(s) " +
                $"but {aboveName} has fewer human-verified ones ({aboveVerified} vs {belowVerified}).",
                $"{belowName} ranks below {aboveName} because it has more human-verified obstacles " +
                $"({belowVerified} vs {aboveVerified}) at the same total count.");
        }

        if (Math.Abs(below.DistanceMeters - above.DistanceMeters) > 0.5)
        {
            return (
                $"{aboveName} ranks above {belowName} because their known-obstacle counts tie " +
                $"({above.KnownObstacles.Count}) and {aboveName} is shorter " +
                $"({above.DistanceMeters / 1000:0.00} km vs {below.DistanceMeters / 1000:0.00} km).",
                $"{belowName} ranks below {aboveName} because their known-obstacle counts tie " +
                $"and {belowName} is longer ({below.DistanceMeters / 1000:0.00} km vs {above.DistanceMeters / 1000:0.00} km).");
        }

        return (
            $"{aboveName} and {belowName} tie on known obstacles and distance; their order is not meaningful.",
            $"{belowName} and {aboveName} tie on known obstacles and distance; their order is not meaningful.");
    }

    public async Task<RouteRecommendationResponse?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var record = await recommendationRepository.GetAsync(id, cancellationToken);
        if (record is null)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<RouteRecommendationResponse>(record.ResponseJson);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

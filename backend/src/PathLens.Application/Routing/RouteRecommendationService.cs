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

public sealed class RouteEvaluationOptions
{
    public const string SectionName = "Routing";
    public int StaleEvidenceDays { get; set; } = 30;
}

public sealed class RouteRecommendationService(
    IRoutingService routingService,
    IReportRepository reportRepository,
    IRouteRecommendationRepository recommendationRepository,
    Microsoft.Extensions.Options.IOptions<RouteEvaluationOptions>? routingOptions = null)
{
    /// <summary>Reports closer than this to the route count as known obstacles on it.</summary>
    public const double ObstacleRadiusMeters = 50;

    /// <summary>Reports within this distance count as evidence coverage (not obstacles).</summary>
    public const double CoverageRadiusMeters = 100;

    /// <summary>Evidence older than this is called stale in explanations — never silently fresh.</summary>
    public const int StaleEvidenceDays = 30;

    private int EffectiveStaleDays => routingOptions?.Value?.StaleEvidenceDays is int v && v > 0 ? v : StaleEvidenceDays;

    public async Task<RouteRecommendationResponse> RecommendAsync(
        GeoPoint origin, GeoPoint destination, CancellationToken cancellationToken,
        RoutePreferences? preferences = null)
    {
        preferences ??= RoutePreferences.Default;
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
                    candidateReport.Report.CameraSourceId is not null,
                    candidateReport.Report.HasStairs,
                    candidateReport.Report.HasRamp,
                    candidateReport.Report.Surface?.ToString()))
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
                    if (ageDays > EffectiveStaleDays)
                    {
                        ageText += $" — stale (over {EffectiveStaleDays} days; conditions may have changed)";
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
                var staleCount = nearby.Count(info => (now - info.EvidenceAt).TotalDays > EffectiveStaleDays);
                explanation.Add(
                    $"Certainty split for this route: {certainCount} certain (human-verified) obstacle(s), " +
                    $"{nearby.Count - certainCount} uncertain (unverified, AI-only, disputed, or camera-detected). " +
                    (staleCount > 0
                        ? $"{staleCount} obstacle(s) rely on stale evidence (over {EffectiveStaleDays} days old)."
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

            var hasConfirmedStairs = nearby.Any(info =>
                info.VerificationStatus == "HumanVerified" &&
                (info.HasStairs == true || info.ObstacleType == "StairsOrStep"));
            var routeStaleCount = nearby.Count(info => (now - info.EvidenceAt).TotalDays > EffectiveStaleDays);

            if (hasConfirmedStairs)
            {
                explanation.Add(
                    "This route has a human-verified stair report. With Avoid stairs on (the default), " +
                    "it cannot outrank a route without one, however short it is.");
            }

            foreach (var obstacle in nearby.Where(info => info.HasStairs is not null || info.HasRamp is not null || info.Surface is not null).Take(3))
            {
                var parts = new List<string>();
                if (obstacle.HasStairs is not null) parts.Add($"stairs: {(obstacle.HasStairs.Value ? "yes" : "no")}");
                if (obstacle.HasRamp is not null) parts.Add($"ramp: {(obstacle.HasRamp.Value ? "yes" : "no")}");
                if (obstacle.Surface is not null) parts.Add($"surface: {obstacle.Surface}");
                explanation.Add($"- Observed attributes for {obstacle.Title}: {string.Join(", ", parts)}. Attributes that nobody measured stay unknown and are never estimated.");
            }

            return new EvaluatedRoute(
                0,
                candidate.Geometry,
                candidate.DistanceMeters,
                candidate.DurationSeconds,
                nearby,
                unknownPercent,
                newestEvidence,
                explanation,
                hasConfirmedStairs,
                routeStaleCount);
        }).ToList();

        // Explainable ranking v2. Key order: confirmed stairs (hard, when
        // Avoid stairs is on), fewest known obstacles, [shortest if
        // preferred], fewest human-verified, [fewest uncertain if
        // preferred], [fewest stale if preferred], shortest. Unknown
        // segments never win by default, and no route is ever "safest".
        var ranked = evaluated
            .OrderBy(route => route, new RouteRankingComparer(preferences))
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
                var (aboveLine, belowLine) = RankingReason(above, below, preferences);

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
            now,
            preferences.Describe());

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

    private sealed class RouteRankingComparer(RoutePreferences preferences) : IComparer<EvaluatedRoute>
    {
        public int Compare(EvaluatedRoute? x, EvaluatedRoute? y)
        {
            if (x is null || y is null) return 0;
            if (preferences.AvoidStairs)
            {
                var c = x.HasConfirmedStairs.CompareTo(y.HasConfirmedStairs);
                if (c != 0) return c;
            }
            var count = x.KnownObstacles.Count.CompareTo(y.KnownObstacles.Count);
            if (count != 0) return count;
            if (preferences.PreferShortest)
            {
                var d = x.DistanceMeters.CompareTo(y.DistanceMeters);
                if (d != 0) return d;
            }
            var verified = Verified(x).CompareTo(Verified(y));
            if (verified != 0) return verified;
            if (preferences.PreferVerifiedEvidence)
            {
                var u = Uncertain(x).CompareTo(Uncertain(y));
                if (u != 0) return u;
            }
            if (preferences.AvoidStaleEvidence)
            {
                var s = x.StaleObstacleCount.CompareTo(y.StaleObstacleCount);
                if (s != 0) return s;
            }
            return x.DistanceMeters.CompareTo(y.DistanceMeters);
        }
        private static int Verified(EvaluatedRoute r) => r.KnownObstacles.Count(i => i.VerificationStatus == "HumanVerified");
        private static int Uncertain(EvaluatedRoute r) => r.KnownObstacles.Count - Verified(r);
    }

    private static (string AboveLine, string BelowLine) RankingReason(
        EvaluatedRoute above, EvaluatedRoute below, RoutePreferences preferences)
    {
        var aboveName = $"Route {above.Index + 1}";
        var belowName = $"Route {below.Index + 1}";

        if (preferences.AvoidStairs && above.HasConfirmedStairs != below.HasConfirmedStairs)
        {
            return (
                $"{aboveName} ranks above {belowName} because {belowName} has a human-verified stair report and Avoid stairs is on: a confirmed-stair route cannot outrank one without, however short it is.",
                $"{belowName} ranks below {aboveName} because it has a human-verified stair report and Avoid stairs is on. Turn Avoid stairs off to treat stairs as an ordinary obstacle.");
        }

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
        if (preferences.PreferShortest && Math.Abs(below.DistanceMeters - above.DistanceMeters) > 0.5)
        {
            return (
                $"{aboveName} ranks above {belowName} because obstacle counts tie and Prefer shortest is on: {aboveName} is shorter ({above.DistanceMeters / 1000:0.00} km vs {below.DistanceMeters / 1000:0.00} km).",
                $"{belowName} ranks below {aboveName} because obstacle counts tie and Prefer shortest is on: {belowName} is longer ({below.DistanceMeters / 1000:0.00} km vs {above.DistanceMeters / 1000:0.00} km).");
        }

        if (belowVerified != aboveVerified)
        {
            return (
                $"{aboveName} ranks above {belowName} because both have {above.KnownObstacles.Count} known obstacle(s) " +
                $"but {aboveName} has fewer human-verified ones ({aboveVerified} vs {belowVerified}).",
                $"{belowName} ranks below {aboveName} because it has more human-verified obstacles " +
                $"({belowVerified} vs {aboveVerified}) at the same total count.");
        }

        if (preferences.PreferVerifiedEvidence &&
            below.KnownObstacles.Count - belowVerified != above.KnownObstacles.Count - aboveVerified)
        {
            return (
                $"{aboveName} ranks above {belowName} because Prefer verified evidence is on and {aboveName} has fewer uncertain obstacles ({above.KnownObstacles.Count - aboveVerified} vs {below.KnownObstacles.Count - belowVerified}).",
                $"{belowName} ranks below {aboveName} because Prefer verified evidence is on and {belowName} has more uncertain obstacles ({below.KnownObstacles.Count - belowVerified} vs {above.KnownObstacles.Count - aboveVerified}). Uncertain does not mean absent.");
        }

        if (preferences.AvoidStaleEvidence && below.StaleObstacleCount != above.StaleObstacleCount)
        {
            return (
                $"{aboveName} ranks above {belowName} because Avoid stale evidence is on and {aboveName} has fewer stale obstacles ({above.StaleObstacleCount} vs {below.StaleObstacleCount}).",
                $"{belowName} ranks below {aboveName} because Avoid stale evidence is on and {belowName} has more stale obstacles ({below.StaleObstacleCount} vs {above.StaleObstacleCount}).");
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

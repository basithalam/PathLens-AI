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
                    candidateReport.Report.Longitude))
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
                    explanation.Add($"- {obstacle.Title} ({obstacle.ObstacleType}, {obstacle.VerificationStatus}) about {obstacle.DistanceMeters:0} m from the route.");
                }
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

namespace PathLens.Application.Routing;

public sealed record NearbyReportInfo(
    Guid ReportId,
    string Title,
    string ObstacleType,
    string VerificationStatus,
    double DistanceMeters,
    DateTimeOffset EvidenceAt,
    double Latitude,
    double Longitude);

public sealed record EvaluatedRoute(
    int Index,
    IReadOnlyList<GeoPoint> Geometry,
    double DistanceMeters,
    double DurationSeconds,
    IReadOnlyList<NearbyReportInfo> KnownObstacles,
    double UnknownLengthPercent,
    DateTimeOffset? NewestEvidenceAt,
    IReadOnlyList<string> Explanation);

public sealed record RouteRecommendationResponse(
    Guid Id,
    string Provider,
    string Profile,
    GeoPoint Origin,
    GeoPoint Destination,
    IReadOnlyList<EvaluatedRoute> Routes,
    IReadOnlyList<string> Limitations,
    DateTimeOffset CreatedAt);

public static class RouteGeometry
{
    /// <summary>Distance from a point to a route polyline, in metres.</summary>
    public static double DistanceToRouteMeters(GeoPoint point, IReadOnlyList<GeoPoint> geometry)
    {
        if (geometry.Count == 0)
        {
            return double.MaxValue;
        }

        if (geometry.Count == 1)
        {
            return DistanceMeters(point, geometry[0]);
        }

        var best = double.MaxValue;
        for (var i = 0; i < geometry.Count - 1; i++)
        {
            best = Math.Min(best, DistanceToSegmentMeters(point, geometry[i], geometry[i + 1]));
        }

        return best;
    }

    public static double RouteLengthMeters(IReadOnlyList<GeoPoint> geometry)
    {
        var total = 0d;
        for (var i = 0; i < geometry.Count - 1; i++)
        {
            total += DistanceMeters(geometry[i], geometry[i + 1]);
        }

        return total;
    }

    /// <summary>
    /// Share of the route (by sampled points) with no report within
    /// <paramref name="radiusMeters"/>. This measures ignorance, not safety:
    /// 100% unknown means nobody has reported there, not that it is clear.
    /// </summary>
    public static double UnknownLengthPercent(
        IReadOnlyList<GeoPoint> geometry,
        IReadOnlyList<GeoPoint> reportPoints,
        double radiusMeters)
    {
        var samples = SamplePoints(geometry, stepMeters: 25);
        if (samples.Count == 0)
        {
            return 100;
        }

        if (reportPoints.Count == 0)
        {
            return 100;
        }

        var unknown = samples.Count(sample =>
            reportPoints.All(report => DistanceMeters(sample, report) > radiusMeters));
        return Math.Round(unknown * 100d / samples.Count, 1);
    }

    public static IReadOnlyList<GeoPoint> SamplePoints(IReadOnlyList<GeoPoint> geometry, double stepMeters)
    {
        var samples = new List<GeoPoint>();
        if (geometry.Count == 0)
        {
            return samples;
        }

        samples.Add(geometry[0]);
        var remaining = stepMeters;

        for (var i = 0; i < geometry.Count - 1; i++)
        {
            var start = geometry[i];
            var end = geometry[i + 1];
            var segmentLength = DistanceMeters(start, end);
            var travelled = 0d;

            while (travelled + remaining <= segmentLength && segmentLength > 0)
            {
                travelled += remaining;
                var fraction = travelled / segmentLength;
                samples.Add(new GeoPoint(
                    start.Latitude + (end.Latitude - start.Latitude) * fraction,
                    start.Longitude + (end.Longitude - start.Longitude) * fraction));
                remaining = stepMeters;
            }

            remaining -= segmentLength - travelled;
        }

        samples.Add(geometry[^1]);
        return samples;
    }

    public static double DistanceMeters(GeoPoint a, GeoPoint b)
    {
        const double earthRadius = 6_371_000;
        var dLat = ToRadians(b.Latitude - a.Latitude);
        var dLon = ToRadians(b.Longitude - a.Longitude);
        var h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(a.Latitude)) * Math.Cos(ToRadians(b.Latitude)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return earthRadius * 2 * Math.Atan2(Math.Sqrt(h), Math.Sqrt(1 - h));
    }

    private static double DistanceToSegmentMeters(GeoPoint point, GeoPoint start, GeoPoint end)
    {
        // Local equirectangular projection around the segment start.
        var kx = 111_320 * Math.Cos(ToRadians(start.Latitude));
        var ky = 110_540;

        var px = (point.Longitude - start.Longitude) * kx;
        var py = (point.Latitude - start.Latitude) * ky;
        var ex = (end.Longitude - start.Longitude) * kx;
        var ey = (end.Latitude - start.Latitude) * ky;

        var lengthSquared = ex * ex + ey * ey;
        var t = lengthSquared == 0 ? 0 : Math.Clamp((px * ex + py * ey) / lengthSquared, 0, 1);
        var closestX = t * ex;
        var closestY = t * ey;
        return Math.Sqrt((px - closestX) * (px - closestX) + (py - closestY) * (py - closestY));
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}

using PathLens.Application.Routing;
using PathLens.Infrastructure.Routing;

namespace PathLens.UnitTests;

public class RouteGeometryTests
{
    private static readonly GeoPoint A = new(23.8103, 90.4125);
    private static readonly GeoPoint B = new(23.8203, 90.4125);

    [Fact]
    public void Point_on_route_has_near_zero_distance_and_far_point_is_far()
    {
        var route = new[] { A, B };
        var middle = new GeoPoint(23.8153, 90.4125);
        var far = new GeoPoint(23.8153, 90.5125);

        Assert.True(RouteGeometry.DistanceToRouteMeters(middle, route) < 5);
        Assert.True(RouteGeometry.DistanceToRouteMeters(far, route) > 5000);
    }

    [Fact]
    public void Route_without_any_reports_is_fully_unknown_not_clear()
    {
        var route = new[] { A, B };
        var percent = RouteGeometry.UnknownLengthPercent(route, [], 100);
        Assert.Equal(100, percent);
    }

    [Fact]
    public void Osrm_parser_reads_geojson_geometry_distance_and_duration()
    {
        const string json = """
            {"code":"Ok","routes":[{"distance":1200.5,"duration":900.2,
            "geometry":{"type":"LineString","coordinates":[[90.4125,23.8103],[90.4135,23.8113]]}}]}
            """;

        var result = OsrmRoutingService.ParseResponse(json, "foot");

        Assert.Equal("OSRM", result.Provider);
        var route = Assert.Single(result.Routes);
        Assert.Equal(1200.5, route.DistanceMeters);
        Assert.Equal(23.8103, route.Geometry[0].Latitude);
        Assert.Equal(90.4125, route.Geometry[0].Longitude);
    }

    [Fact]
    public void Osrm_parser_rejects_empty_routes_instead_of_inventing_one()
    {
        Assert.Throws<RouteProviderUnavailableException>(() =>
            OsrmRoutingService.ParseResponse("""{"code":"NoRoute","routes":[]}""", "foot"));
    }
}

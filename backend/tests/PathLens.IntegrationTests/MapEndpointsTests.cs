using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PathLens.IntegrationTests;

public class MapEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string TestConnectionString =
        "Host=localhost;Port=5433;Database=pathlens_test;Username=postgres;Timeout=5;Command Timeout=15";

    private readonly HttpClient _client;

    public MapEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:DefaultConnection", TestConnectionString))
            .CreateClient();
    }

    private async Task CreateReportAsync(double latitude, double longitude, string obstacleType = "BlockedSidewalk")
    {
        var response = await _client.PostAsJsonAsync("/api/reports", new
        {
            title = $"Map test {Guid.NewGuid():N}",
            description = (string?)null,
            obstacleType,
            severity = "High",
            latitude,
            longitude,
        });
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Viewport_returns_only_reports_inside_bounds()
    {
        await CreateReportAsync(23.8103, 90.4125);
        await CreateReportAsync(10.0, 10.0);

        var inside = await _client.GetFromJsonAsync<JsonElement>(
            "/api/map/reports?minLatitude=23&maxLatitude=24&minLongitude=90&maxLongitude=91");

        Assert.True(inside.GetArrayLength() >= 1);
        Assert.All(inside.EnumerateArray(), item =>
        {
            var latitude = item.GetProperty("latitude").GetDouble();
            var longitude = item.GetProperty("longitude").GetDouble();
            Assert.InRange(latitude, 23, 24);
            Assert.InRange(longitude, 90, 91);
        });
    }

    [Fact]
    public async Task Viewport_filter_by_obstacle_type()
    {
        await CreateReportAsync(23.8103, 90.4125, "BrokenSurface");

        var filtered = await _client.GetFromJsonAsync<JsonElement>(
            "/api/map/reports?minLatitude=23&maxLatitude=24&minLongitude=90&maxLongitude=91&obstacleType=BrokenSurface");

        Assert.True(filtered.GetArrayLength() >= 1);
        Assert.All(filtered.EnumerateArray(), item =>
            Assert.Equal("BrokenSurface", item.GetProperty("obstacleType").GetString()));
    }

    [Fact]
    public async Task Invalid_viewport_returns_400()
    {
        var response = await _client.GetAsync(
            "/api/map/reports?minLatitude=24&maxLatitude=23&minLongitude=90&maxLongitude=91");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Summary_reports_counts()
    {
        await CreateReportAsync(23.8103, 90.4125);

        var summary = await _client.GetFromJsonAsync<JsonElement>("/api/map/summary");

        Assert.True(summary.GetProperty("totalReports").GetInt32() >= 1);
        Assert.True(summary.GetProperty("byObstacleType").TryGetProperty("BlockedSidewalk", out _));
    }

    [Fact]
    public async Task Coverage_returns_grid_with_summary_and_status_strings()
    {
        await CreateReportAsync(23.8103, 90.4125);

        var coverage = await _client.GetFromJsonAsync<JsonElement>(
            "/api/map/coverage?minLatitude=23.8&maxLatitude=23.82&minLongitude=90.4&maxLongitude=90.43");

        Assert.True(coverage.GetProperty("cells").GetArrayLength() > 0);
        Assert.True(coverage.GetProperty("summary").GetProperty("totalCells").GetInt32() > 0);
        Assert.True(coverage.GetProperty("summary").GetProperty("insufficientCells").GetInt32() >= 0);
        // New: status is a string, not an integer
        var first = coverage.GetProperty("cells").EnumerateArray().First();
        Assert.True(first.GetProperty("status").ValueKind == JsonValueKind.String);
        var status = first.GetProperty("status").GetString();
        Assert.Contains(status, new[] { "InsufficientInfo", "ObstacleEvidence", "EvidencePresent", "Mixed" });
    }

    [Fact]
    public async Task Coverage_invalid_viewport_returns_400()
    {
        var response = await _client.GetAsync(
            "/api/map/coverage?minLatitude=24&maxLatitude=23&minLongitude=90&maxLongitude=91");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Coverage_invalid_cellSize_returns_400()
    {
        var response = await _client.GetAsync(
            "/api/map/coverage?minLatitude=23&maxLatitude=24&minLongitude=90&maxLongitude=91&cellSizeMeters=5000");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

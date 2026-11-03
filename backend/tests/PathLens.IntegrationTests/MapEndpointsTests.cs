using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PathLens.IntegrationTests;

public class MapEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly string TestConnectionString = TestDatabase.Connection("pathlens_test");

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

    [Fact]
    public async Task Coverage_empty_viewport_all_insufficient()
    {
        var coverage = await _client.GetFromJsonAsync<JsonElement>(
            "/api/map/coverage?minLatitude=0&maxLatitude=0.01&minLongitude=0&maxLongitude=0.01");

        Assert.True(coverage.GetProperty("cells").GetArrayLength() > 0);
        Assert.All(coverage.GetProperty("cells").EnumerateArray(),
            cell => Assert.Equal("InsufficientInfo", cell.GetProperty("status").GetString()));
        Assert.Equal(0, coverage.GetProperty("summary").GetProperty("coveredCells").GetInt32());
    }

    [Fact]
    public async Task Coverage_respects_limit_and_auto_scales_large_viewport()
    {
        var coverage = await _client.GetFromJsonAsync<JsonElement>(
            "/api/map/coverage?minLatitude=20&maxLatitude=25&minLongitude=88&maxLongitude=93&cellSizeMeters=20");

        // Large viewport with small cellSize would exceed 1000, so it auto-scales.
        Assert.True(coverage.GetProperty("cells").GetArrayLength() <= 1000);
        Assert.True(coverage.GetProperty("cellSizeMeters").GetDouble() >= 20);
    }

    [Fact]
    public async Task Coverage_status_has_cellId_and_bounds_and_reportCounts()
    {
        await CreateReportAsync(23.8110, 90.4130);
        var coverage = await _client.GetFromJsonAsync<JsonElement>(
            "/api/map/coverage?minLatitude=23.81&maxLatitude=23.812&minLongitude=90.412&maxLongitude=90.414");

        var cellWithReport = coverage.GetProperty("cells").EnumerateArray().FirstOrDefault(c => c.GetProperty("reportCount").GetInt32() > 0);
        Assert.True(cellWithReport.ValueKind != JsonValueKind.Undefined);
        Assert.True(cellWithReport.GetProperty("reportCount").GetInt32() >= 1);
        Assert.False(string.IsNullOrWhiteSpace(cellWithReport.GetProperty("cellId").GetString()));
        Assert.True(cellWithReport.GetProperty("minLatitude").GetDouble() <= cellWithReport.GetProperty("maxLatitude").GetDouble());
        Assert.True(cellWithReport.GetProperty("newestEvidenceAt").ValueKind != JsonValueKind.Null);
    }
}

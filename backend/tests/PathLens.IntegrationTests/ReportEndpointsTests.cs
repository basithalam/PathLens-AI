using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;

namespace PathLens.IntegrationTests;

public class ReportEndpointsTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private const string TestConnectionString =
        "Host=localhost;Port=5433;Database=pathlens_test;Username=postgres;Timeout=5;Command Timeout=15";

    private readonly HttpClient _client;

    public ReportEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:DefaultConnection", TestConnectionString))
            .CreateClient();
    }

    public async Task InitializeAsync()
    {
        await using var connection = new NpgsqlConnection(TestConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "TRUNCATE TABLE accessibility_reports CASCADE", connection);
        try
        {
            await command.ExecuteNonQueryAsync();
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            // Table does not exist until the API creates it via migrations in this fixture run.
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static object ValidReport(double latitude = 23.8103, double longitude = 90.4125) => new
    {
        title = "Blocked sidewalk near crossing",
        description = "A temporary structure obstructs the path.",
        obstacleType = "BlockedSidewalk",
        severity = "High",
        latitude,
        longitude,
    };

    [Fact]
    public async Task Create_then_get_report_persists()
    {
        var create = await _client.PostAsJsonAsync("/api/reports", ValidReport());
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetGuid();

        var get = await _client.GetAsync($"/api/reports/{id}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var fetched = await get.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Blocked sidewalk near crossing", fetched.GetProperty("title").GetString());
        Assert.Equal("BlockedSidewalk", fetched.GetProperty("obstacleType").GetString());
    }

    [Fact]
    public async Task Create_with_invalid_coordinates_returns_400_problem_details()
    {
        var response = await _client.PostAsJsonAsync("/api/reports", ValidReport(latitude: 123));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("latitude", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task List_supports_pagination_and_obstacle_filter()
    {
        await _client.PostAsJsonAsync("/api/reports", ValidReport());
        await _client.PostAsJsonAsync("/api/reports", new
        {
            title = "Broken surface",
            description = (string?)null,
            obstacleType = "BrokenSurface",
            severity = "Low",
            latitude = 23.9,
            longitude = 90.5,
        });

        var filtered = await _client.GetFromJsonAsync<JsonElement>(
            "/api/reports?obstacleType=BrokenSurface&page=1&pageSize=10");
        Assert.Equal(1, filtered.GetProperty("totalCount").GetInt32());
        Assert.Equal("BrokenSurface",
            filtered.GetProperty("items")[0].GetProperty("obstacleType").GetString());

        var page = await _client.GetFromJsonAsync<JsonElement>("/api/reports?page=1&pageSize=1");
        Assert.Equal(2, page.GetProperty("totalCount").GetInt32());
        Assert.Equal(1, page.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task Get_missing_report_returns_404()
    {
        var response = await _client.GetAsync($"/api/reports/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

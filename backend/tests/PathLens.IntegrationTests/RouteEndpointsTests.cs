using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PathLens.IntegrationTests;

public class RouteEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string TestConnectionString =
        "Host=localhost;Port=5433;Database=pathlens_test;Username=postgres;Timeout=5;Command Timeout=15";

    private readonly HttpClient _client;

    public RouteEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", TestConnectionString);
            builder.UseSetting("Routing:Provider", "Mock");
        }).CreateClient();
    }

    private async Task CreateReportAsync(double latitude, double longitude, string title)
    {
        var response = await _client.PostAsJsonAsync("/api/reports", new
        {
            title,
            description = (string?)null,
            obstacleType = "BlockedSidewalk",
            severity = "High",
            latitude,
            longitude,
        });
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Recommend_returns_ranked_routes_with_explanations_and_persists()
    {
        // Isolated coordinates so reports from other tests cannot interfere.
        // The direct mock route passes (24.505, 92.0); the detour bends away.
        await CreateReportAsync(24.505, 92.0, $"Route obstacle {Guid.NewGuid():N}");

        var response = await _client.PostAsJsonAsync("/api/routes/recommend", new
        {
            originLatitude = 24.5,
            originLongitude = 92.0,
            destinationLatitude = 24.51,
            destinationLongitude = 92.0,
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var recommendation = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("MockRouting", recommendation.GetProperty("provider").GetString());

        var routes = recommendation.GetProperty("routes");
        Assert.Equal(2, routes.GetArrayLength());

        // The route with the known obstacle must not be ranked first.
        var first = routes[0];
        Assert.Equal(0, first.GetProperty("knownObstacles").GetArrayLength());

        var withObstacle = routes.EnumerateArray()
            .First(route => route.GetProperty("knownObstacles").GetArrayLength() > 0);
        var explanationText = string.Join(" ", withObstacle.GetProperty("explanation")
            .EnumerateArray().Select(element => element.GetString()));
        Assert.Contains("known obstacle", explanationText);

        var limitationsText = string.Join(" ", recommendation.GetProperty("limitations")
            .EnumerateArray().Select(element => element.GetString()));
        Assert.Contains("unknown, not clear", limitationsText);
        Assert.Contains("not been validated", limitationsText);

        // Persisted: the same recommendation can be reopened.
        var id = recommendation.GetProperty("id").GetGuid();
        var reopened = await _client.GetAsync($"/api/routes/{id}");
        Assert.Equal(HttpStatusCode.OK, reopened.StatusCode);
    }

    [Fact]
    public async Task Recommend_rejects_same_origin_and_destination()
    {
        var response = await _client.PostAsJsonAsync("/api/routes/recommend", new
        {
            originLatitude = 23.8103,
            originLongitude = 90.4125,
            destinationLatitude = 23.8103,
            destinationLongitude = 90.4125,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Recommend_rejects_invalid_coordinates()
    {
        var response = await _client.PostAsJsonAsync("/api/routes/recommend", new
        {
            originLatitude = 123,
            originLongitude = 90.4125,
            destinationLatitude = 23.8203,
            destinationLongitude = 90.4125,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

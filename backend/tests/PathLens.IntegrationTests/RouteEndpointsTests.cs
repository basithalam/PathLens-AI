using System.Net;
using System.Net.Http.Headers;
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
        // F1: per-obstacle age/certainty, route certainty split, and the
        // cross-route reason must all be present and human-readable.
        Assert.Contains("evidence less than a day old", explanationText);
        Assert.Contains("uncertain (not human-verified)", explanationText);
        Assert.Contains("Certainty split for this route:", explanationText);
        Assert.Contains("ranks below Route 1", explanationText);

        var firstText = string.Join(" ", first.GetProperty("explanation")
            .EnumerateArray().Select(element => element.GetString()));
        Assert.Contains("ranks above Route 2", firstText);
        Assert.Contains("not proof of safety", firstText);

        var limitationsText = string.Join(" ", recommendation.GetProperty("limitations")
            .EnumerateArray().Select(element => element.GetString()));
        Assert.Contains("unknown, not clear", limitationsText);
        Assert.Contains("not been validated", limitationsText);

        // Persisted: the same recommendation can be reopened.
        var id = recommendation.GetProperty("id").GetGuid();
        var reopened = await _client.GetAsync($"/api/routes/{id}");
        Assert.Equal(HttpStatusCode.OK, reopened.StatusCode);
    }

    private async Task<Guid> CreateReportFullAsync(
        double latitude, double longitude, string title,
        string obstacleType = "BlockedSidewalk", bool? hasStairs = null, string? surface = null)
    {
        var response = await _client.PostAsJsonAsync("/api/reports", new
        {
            title,
            description = (string?)null,
            obstacleType,
            severity = "High",
            latitude,
            longitude,
            hasStairs,
            hasRamp = (bool?)false,
            surface,
        });
        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<JsonElement>();
        return report.GetProperty("id").GetGuid();
    }

    private async Task VerifyAsync(Guid reportId)
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "reviewer@pathlens.local", password = "Reviewer@123" });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("accessToken").GetString();

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/reports/{reportId}/review")
        {
            Content = JsonContent.Create(new { verificationStatus = "HumanVerified", note = "F2 test verification." }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private async Task<JsonElement> RecommendAsync(double lat, double lon, object? preferences)
    {
        var response = await _client.PostAsJsonAsync("/api/routes/recommend", new
        {
            originLatitude = lat,
            originLongitude = lon,
            destinationLatitude = lat + 0.01,
            destinationLongitude = lon,
            preferences,
        });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task Report_attributes_are_stored_with_their_source()
    {
        var response = await _client.PostAsJsonAsync("/api/reports", new
        {
            title = $"Attribute test {Guid.NewGuid():N}",
            description = (string?)null,
            obstacleType = "StairsOrStep",
            severity = "High",
            latitude = 28.5,
            longitude = 96.0,
            hasStairs = true,
            hasRamp = false,
            surface = "Broken",
        });
        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(report.GetProperty("hasStairs").GetBoolean());
        Assert.False(report.GetProperty("hasRamp").GetBoolean());
        Assert.Equal("Broken", report.GetProperty("surface").GetString());
        Assert.Equal("Reporter", report.GetProperty("attributeSource").GetString());

        var badSurface = await _client.PostAsJsonAsync("/api/reports", new
        {
            title = "Bad surface",
            description = (string?)null,
            obstacleType = "Other",
            severity = "Low",
            latitude = 28.5,
            longitude = 96.0,
            surface = "Sparkly",
        });
        Assert.Equal(HttpStatusCode.BadRequest, badSurface.StatusCode);
    }

    [Fact]
    public async Task Verified_stairs_are_a_hard_constraint_unless_the_preference_is_off()
    {
        // Direct route (straight line) gets ONE human-verified stair report.
        var stairId = await CreateReportFullAsync(
            27.505, 95.0, $"Verified stairs {Guid.NewGuid():N}", "StairsOrStep", hasStairs: true, surface: "Broken");
        await VerifyAsync(stairId);

        // Detour (bends +0.004 longitude at the middle) gets TWO unverified reports.
        await CreateReportFullAsync(27.505, 95.004, $"Detour obstacle A {Guid.NewGuid():N}");
        await CreateReportFullAsync(27.5025, 95.0028, $"Detour obstacle B {Guid.NewGuid():N}");

        // Default preferences: Avoid stairs is hard, so the 2-obstacle detour
        // must outrank the 1-obstacle stair route.
        var withDefault = await RecommendAsync(27.5, 95.0, null);
        var routes = withDefault.GetProperty("routes");
        // Relative assertions only: the shared test DB keeps reports from
        // earlier runs, so exact obstacle counts grow between runs.
        Assert.True(
            routes[0].GetProperty("knownObstacles").GetArrayLength() >
            routes[1].GetProperty("knownObstacles").GetArrayLength(),
            "With Avoid stairs on, the obstacle-heavier detour must outrank the stair route.");
        Assert.True(routes[1].GetProperty("hasConfirmedStairs").GetBoolean());
        var firstText = string.Join(" ", routes[0].GetProperty("explanation")
            .EnumerateArray().Select(e => e.GetString()));
        Assert.Contains("human-verified stair report", firstText);
        Assert.Contains("Avoid stairs: hard constraint", string.Join(" ",
            withDefault.GetProperty("appliedPreferences").EnumerateArray().Select(e => e.GetString())));

        // Avoid stairs off: the stair route (fewest obstacles) ranks first again.
        var stairsOff = await RecommendAsync(27.5, 95.0, new
        {
            avoidStairs = false,
            preferVerifiedEvidence = false,
            avoidStaleEvidence = false,
            preferShortest = false,
        });
        var offRoutes = stairsOff.GetProperty("routes");
        Assert.True(offRoutes[0].GetProperty("hasConfirmedStairs").GetBoolean());
        Assert.True(
            offRoutes[0].GetProperty("knownObstacles").GetArrayLength() <
            offRoutes[1].GetProperty("knownObstacles").GetArrayLength(),
            "With Avoid stairs off, the stair route wins on fewest obstacles again.");
    }

    [Fact]
    public async Task Old_evidence_is_called_stale_in_route_explanations()
    {
        var title = $"Stale route obstacle {Guid.NewGuid():N}";
        await CreateReportAsync(26.505, 94.0, title);

        // Age the report 45 days directly; the API never lets users fake age.
        await using var connection = new Npgsql.NpgsqlConnection(TestConnectionString);
        await connection.OpenAsync();
        await using var command = new Npgsql.NpgsqlCommand(
            "UPDATE accessibility_reports SET \"CreatedAt\" = now() - interval '45 days' WHERE \"Title\" = @title",
            connection);
        command.Parameters.AddWithValue("title", title);
        await command.ExecuteNonQueryAsync();

        var response = await _client.PostAsJsonAsync("/api/routes/recommend", new
        {
            originLatitude = 26.5,
            originLongitude = 94.0,
            destinationLatitude = 26.51,
            destinationLongitude = 94.0,
        });
        response.EnsureSuccessStatusCode();

        var recommendation = await response.Content.ReadFromJsonAsync<JsonElement>();
        var withObstacle = recommendation.GetProperty("routes").EnumerateArray()
            .First(route => route.GetProperty("knownObstacles").GetArrayLength() > 0);
        var text = string.Join(" ", withObstacle.GetProperty("explanation")
            .EnumerateArray().Select(element => element.GetString()));

        Assert.Contains("stale (over 30 days", text);
        Assert.Contains("rely on stale evidence", text);
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


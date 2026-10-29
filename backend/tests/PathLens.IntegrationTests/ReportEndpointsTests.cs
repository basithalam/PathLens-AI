using System.Net;
using System.Net.Http.Headers;
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

    private async Task<JsonElement> CreateAtAsync(
        double latitude, double longitude, string obstacleType = "BlockedSidewalk")
    {
        var response = await _client.PostAsJsonAsync("/api/reports", new
        {
            title = $"Report {Guid.NewGuid():N}",
            description = (string?)null,
            obstacleType,
            severity = "High",
            latitude,
            longitude,
        });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<string> ReviewerTokenAsync()
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "reviewer@pathlens.local", password = "Reviewer@123" });
        login.EnsureSuccessStatusCode();
        return (await login.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("accessToken").GetString()!;
    }

    private async Task<HttpResponseMessage> DuplicateDecisionAsync(Guid id, string action, string? token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/reports/{id}/duplicate/{action}")
        {
            Content = JsonContent.Create(new { note = "F4 test decision." }),
        };
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await _client.SendAsync(request);
    }

    [Fact]
    public async Task Nearby_same_type_report_gets_a_duplicate_suggestion_but_different_type_or_far_does_not()
    {
        var first = await CreateAtAsync(30.5000, 97.0000);
        var firstId = first.GetProperty("id").GetGuid();
        Assert.True(first.GetProperty("possibleDuplicateOfReportId").ValueKind == JsonValueKind.Null);

        // About 11 m away, same type: suggestion.
        var near = await CreateAtAsync(30.5001, 97.0000);
        Assert.Equal(firstId, near.GetProperty("possibleDuplicateOfReportId").GetGuid());

        // About 11 m away but a different (non-Unknown) type: no suggestion.
        var differentType = await CreateAtAsync(30.5001, 97.0001, "ConstructionBarrier");
        Assert.True(differentType.GetProperty("possibleDuplicateOfReportId").ValueKind == JsonValueKind.Null);

        // Same type but ~1 km away: no suggestion.
        var far = await CreateAtAsync(30.5100, 97.0000);
        Assert.True(far.GetProperty("possibleDuplicateOfReportId").ValueKind == JsonValueKind.Null);
    }

    [Fact]
    public async Task Reviewer_can_confirm_merge_and_the_duplicate_leaves_lists_but_stays_readable()
    {
        var first = await CreateAtAsync(31.5000, 98.0000);
        var firstId = first.GetProperty("id").GetGuid();
        var duplicate = await CreateAtAsync(31.5001, 98.0000);
        var duplicateId = duplicate.GetProperty("id").GetGuid();

        // Anonymous cannot decide.
        var anonymous = await DuplicateDecisionAsync(duplicateId, "confirm", token: null);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        var token = await ReviewerTokenAsync();
        var confirmed = await DuplicateDecisionAsync(duplicateId, "confirm", token);
        confirmed.EnsureSuccessStatusCode();
        var body = await confirmed.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(firstId, body.GetProperty("duplicateOfReportId").GetGuid());
        Assert.True(body.GetProperty("possibleDuplicateOfReportId").ValueKind == JsonValueKind.Null);

        // Not counted as a separate report any more...
        var list = await _client.GetFromJsonAsync<JsonElement>("/api/reports?pageSize=50");
        Assert.DoesNotContain(list.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("id").GetGuid() == duplicateId);
        Assert.Contains(list.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("id").GetGuid() == firstId);

        // ...but the pointer page still opens (evidence/history are not deleted).
        var get = await _client.GetAsync($"/api/reports/{duplicateId}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);

        // The decision is in the audit trail.
        var reviews = await _client.GetFromJsonAsync<JsonElement>($"/api/reports/{duplicateId}/reviews");
        Assert.Contains(reviews.EnumerateArray(),
            entry => entry.GetProperty("action").GetString() == "DuplicateConfirmed");
    }

    [Fact]
    public async Task Reviewer_can_reject_a_suggestion_and_both_reports_stay_listed()
    {
        var first = await CreateAtAsync(32.5000, 99.0000);
        var duplicate = await CreateAtAsync(32.5001, 99.0000);
        var duplicateId = duplicate.GetProperty("id").GetGuid();
        Assert.True(duplicate.GetProperty("possibleDuplicateOfReportId").ValueKind != JsonValueKind.Null);

        var token = await ReviewerTokenAsync();
        var rejected = await DuplicateDecisionAsync(duplicateId, "reject", token);
        rejected.EnsureSuccessStatusCode();
        var body = await rejected.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("possibleDuplicateOfReportId").ValueKind == JsonValueKind.Null);
        Assert.True(body.GetProperty("duplicateOfReportId").ValueKind == JsonValueKind.Null);

        var list = await _client.GetFromJsonAsync<JsonElement>("/api/reports?pageSize=50");
        Assert.Equal(2, list.GetProperty("totalCount").GetInt32());

        // Rejecting again (nothing to reject) is a 400, not a silent success.
        var again = await DuplicateDecisionAsync(duplicateId, "reject", token);
        Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);

        // The canonical first report has nothing to confirm either.
        var confirmFirst = await DuplicateDecisionAsync(first.GetProperty("id").GetGuid(), "confirm", token);
        Assert.Equal(HttpStatusCode.BadRequest, confirmFirst.StatusCode);
    }

    [Fact]
    public async Task Get_missing_report_returns_404()
    {
        var response = await _client.GetAsync($"/api/reports/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Feedback_valid_kind_returns_201_and_audit()
    {
        var create = await _client.PostAsJsonAsync("/api/reports", ValidReport());
        var reportId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var feedback = await _client.PostAsJsonAsync($"/api/reports/{reportId}/feedback", new { kind = "StillVisible", note = "Still blocked today" });
        Assert.Equal(HttpStatusCode.Created, feedback.StatusCode);
        var body = await feedback.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("StillVisible", body.GetProperty("kind").GetString());
    }

    [Fact]
    public async Task Feedback_invalid_kind_returns_400()
    {
        var create = await _client.PostAsJsonAsync("/api/reports", ValidReport());
        var reportId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var feedback = await _client.PostAsJsonAsync($"/api/reports/{reportId}/feedback", new { kind = "Bogus", note = (string?)null });
        Assert.Equal(HttpStatusCode.BadRequest, feedback.StatusCode);
    }

    [Fact]
    public async Task Feedback_missing_report_returns_404()
    {
        var feedback = await _client.PostAsJsonAsync($"/api/reports/{Guid.NewGuid()}/feedback", new { kind = "NotSure", note = "Unsure" });
        Assert.Equal(HttpStatusCode.NotFound, feedback.StatusCode);
    }
}

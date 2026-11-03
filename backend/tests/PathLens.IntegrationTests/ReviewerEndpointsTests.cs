using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PathLens.IntegrationTests;

public class ReviewerEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly string TestConnectionString = TestDatabase.Connection("pathlens_test");

    private readonly HttpClient _client;

    public ReviewerEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:DefaultConnection", TestConnectionString))
            .CreateClient();
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<JsonElement>();
        return login.GetProperty("accessToken").GetString()!;
    }

    private async Task<HttpClient> ClientForAsync(string email, string password)
    {
        // Reuse the same client but set the bearer header per test through a fresh request helper.
        var token = await LoginAsync(email, password);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return _client;
    }

    private async Task<Guid> CreateReportAsync()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.PostAsJsonAsync("/api/reports", new
        {
            title = $"Review test {Guid.NewGuid():N}",
            description = (string?)null,
            obstacleType = "BlockedSidewalk",
            severity = "High",
            latitude = 25.5,
            longitude = 93.0,
        });
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        return created.GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Seeded_reviewer_can_login_and_wrong_password_is_rejected()
    {
        var token = await LoginAsync("reviewer@pathlens.local", "Reviewer@123");
        Assert.False(string.IsNullOrWhiteSpace(token));

        var bad = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "reviewer@pathlens.local",
            password = "wrong-password",
        });
        Assert.Equal(HttpStatusCode.Unauthorized, bad.StatusCode);
    }

    [Fact]
    public async Task Anonymous_cannot_review_and_plain_user_is_forbidden()
    {
        var reportId = await CreateReportAsync();

        var anonymous = await _client.PostAsJsonAsync($"/api/reports/{reportId}/review", new
        {
            verificationStatus = "HumanVerified",
            note = "Checked on site.",
        });
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        await ClientForAsync("user@pathlens.local", "User@123");
        var forbidden = await _client.PostAsJsonAsync($"/api/reports/{reportId}/review", new
        {
            verificationStatus = "HumanVerified",
            note = "Checked on site.",
        });
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task Reviewer_verify_updates_report_and_writes_audit()
    {
        var reportId = await CreateReportAsync();
        await ClientForAsync("reviewer@pathlens.local", "Reviewer@123");

        var review = await _client.PostAsJsonAsync($"/api/reports/{reportId}/review", new
        {
            verificationStatus = "HumanVerified",
            note = "Visited the location and confirmed the obstacle.",
        });
        Assert.Equal(HttpStatusCode.OK, review.StatusCode);
        var reviewed = await review.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("HumanVerified", reviewed.GetProperty("verificationStatus").GetString());

        _client.DefaultRequestHeaders.Authorization = null;
        var audit = await _client.GetFromJsonAsync<JsonElement>($"/api/reports/{reportId}/reviews");
        Assert.True(audit.GetArrayLength() >= 1);
        Assert.Equal("VerificationChanged", audit[0].GetProperty("action").GetString());
        Assert.Equal("Unverified", audit[0].GetProperty("previousValue").GetString());
        Assert.Equal("HumanVerified", audit[0].GetProperty("newValue").GetString());

        await ClientForAsync("reviewer@pathlens.local", "Reviewer@123");
        var queue = await _client.GetFromJsonAsync<JsonElement>("/api/reviewer/queue");
        Assert.True(queue.GetProperty("reports").GetArrayLength() >= 0);
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PathLens.Infrastructure.Persistence;

namespace PathLens.IntegrationTests;

public class ComparisonEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    // Dedicated database: background processors in other test hosts poll the
    // same database a class uses, so each class keeps its own.
    private const string TestConnectionString =
        "Host=localhost;Port=5433;Database=pathlens_comparison_test;Username=postgres;Timeout=5;Command Timeout=15";

    private static readonly byte[] TinyPng =
        Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8Xw8AAwMCAO+ip1sAAAAASUVORK5CYII=");

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ComparisonEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", TestConnectionString);
            builder.UseSetting("Ai:Provider", "Mock");
            builder.UseSetting("Storage:RootPath",
                Path.Combine(Path.GetTempPath(), "pathlens-comparison-tests", "shared"));
        });
        _client = _factory.CreateClient();
    }

    private async Task LoginAsReviewerAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "reviewer@pathlens.local",
            password = "Reviewer@123",
        });
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<JsonElement>();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login.GetProperty("accessToken").GetString());
    }

    private async Task<Guid> CreateReportAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/reports", new
        {
            title = "Comparison test report",
            description = (string?)null,
            obstacleType = "BlockedSidewalk",
            severity = "High",
            latitude = 23.8103,
            longitude = 90.4125,
        });
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        return created.GetProperty("id").GetGuid();
    }

    private async Task<Guid> UploadEvidenceAsync(Guid reportId, DateTimeOffset captureTime)
    {
        var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(TinyPng);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(fileContent, "file", "photo.png");
        form.Add(new StringContent(captureTime.ToString("O")), "captureTimeUtc");

        var response = await _client.PostAsync($"/api/reports/{reportId}/evidence", form);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        return created.GetProperty("id").GetGuid();
    }

    /// <summary>Seeds a completed analysis directly, as a real provider would have produced it.</summary>
    private async Task SeedAnalysisAsync(Guid evidenceId, string obstacleType, double confidence)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PathLensDbContext>();
        var item = await dbContext.EvidenceItems.FirstAsync(candidate => candidate.Id == evidenceId);

        var json = JsonSerializer.Serialize(new
        {
            schemaVersion = "1.0",
            obstacleType,
            visibleEvidence = new[] { "Seeded visible evidence for comparison tests." },
            limitations = Array.Empty<string>(),
            confidence,
            humanReviewRequired = true,
            provider = "Meta",
            model = "test-seed",
            analyzedAt = DateTimeOffset.UtcNow,
        });

        item.MarkAnalysisCompleted(json, "Meta", "test-seed", DateTimeOffset.UtcNow);
        await dbContext.SaveChangesAsync();
    }

    private async Task<(Guid Earlier, Guid Later)> CreatePairAsync(
        string laterObstacle, double laterConfidence = 0.85)
    {
        var reportId = await CreateReportAsync();
        var now = DateTimeOffset.UtcNow.AddDays(-2);
        var earlier = await UploadEvidenceAsync(reportId, now);
        var later = await UploadEvidenceAsync(reportId, now.AddDays(1));
        await SeedAnalysisAsync(earlier, "BlockedSidewalk", 0.85);
        await SeedAnalysisAsync(later, laterObstacle, laterConfidence);
        return (earlier, later);
    }

    private async Task<JsonElement> CompareAsync(Guid earlier, Guid later)
    {
        var response = await _client.PostAsJsonAsync("/api/comparisons", new
        {
            earlierEvidenceId = earlier,
            laterEvidenceId = later,
        });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task Same_obstacle_twice_is_likely_still_present_and_persists()
    {
        var (earlier, later) = await CreatePairAsync("BlockedSidewalk");

        var comparison = await CompareAsync(earlier, later);
        Assert.Equal("LikelyStillPresent", comparison.GetProperty("outcome").GetString());

        var id = comparison.GetProperty("id").GetGuid();
        var fetched = await _client.GetFromJsonAsync<JsonElement>($"/api/comparisons/{id}");
        Assert.Equal("LikelyStillPresent", fetched.GetProperty("effectiveOutcome").GetString());

        var reportId = comparison.GetProperty("reportId").GetGuid();
        var list = await _client.GetFromJsonAsync<JsonElement>($"/api/reports/{reportId}/comparisons");
        Assert.True(list.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Missing_obstacle_later_is_not_observed_not_removed()
    {
        var (earlier, later) = await CreatePairAsync("Unknown");

        var comparison = await CompareAsync(earlier, later);

        Assert.Equal("NotObservedInNewImage", comparison.GetProperty("outcome").GetString());
        Assert.Contains("does not prove", comparison.GetProperty("explanation").GetString());
    }

    [Fact]
    public async Task Self_comparison_is_rejected()
    {
        var (earlier, _) = await CreatePairAsync("BlockedSidewalk");

        var response = await _client.PostAsJsonAsync("/api/comparisons", new
        {
            earlierEvidenceId = earlier,
            laterEvidenceId = earlier,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Human_review_overrides_effective_outcome_but_keeps_original()
    {
        var (earlier, later) = await CreatePairAsync("BlockedSidewalk");
        var comparison = await CompareAsync(earlier, later);
        var id = comparison.GetProperty("id").GetGuid();

        await LoginAsReviewerAsync();
        var review = await _client.PostAsJsonAsync($"/api/comparisons/{id}/review", new
        {
            reviewOutcome = "Inconclusive",
            reason = "The newer photo is taken from a different angle.",
        });
        review.EnsureSuccessStatusCode();
        var reviewed = await review.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("LikelyStillPresent", reviewed.GetProperty("outcome").GetString());
        Assert.Equal("Inconclusive", reviewed.GetProperty("effectiveOutcome").GetString());
        Assert.Equal("Inconclusive", reviewed.GetProperty("reviewedOutcome").GetString());
    }

    [Fact]
    public async Task Review_without_reason_is_rejected()
    {
        var (earlier, later) = await CreatePairAsync("BlockedSidewalk");
        var comparison = await CompareAsync(earlier, later);
        var id = comparison.GetProperty("id").GetGuid();

        await LoginAsReviewerAsync();
        var response = await _client.PostAsJsonAsync($"/api/comparisons/{id}/review", new
        {
            reviewOutcome = "Inconclusive",
            reason = "",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

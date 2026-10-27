using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PathLens.IntegrationTests;

public class EvidenceEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    // A dedicated database: the background job processor in every test host
    // polls for pending jobs, so evidence tests must not share a database
    // (and storage root) with other test classes.
    private const string TestConnectionString =
        "Host=localhost;Port=5433;Database=pathlens_evidence_test;Username=postgres;Timeout=5;Command Timeout=15";

    private static readonly byte[] TinyPng =
        Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8Xw8AAwMCAO+ip1sAAAAASUVORK5CYII=");

    // One storage root for the whole class: every test instance builds its own
    // host, and all of them poll the same database for jobs. A per-instance
    // root would let another host's processor steal a job and miss the file.
    private static readonly string StorageRoot =
        Path.Combine(Path.GetTempPath(), "pathlens-tests", Guid.NewGuid().ToString("N"));

    private readonly HttpClient _client;
    private readonly string _storageRoot = StorageRoot;

    public EvidenceEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", TestConnectionString);
            builder.UseSetting("Ai:Provider", "Mock");
            builder.UseSetting("Storage:RootPath", _storageRoot);
        }).CreateClient();
    }

    private async Task<Guid> CreateReportAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/reports", new
        {
            title = "Evidence test report",
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

    private MultipartFormDataContent ImageForm(byte[] content, string fileName, string contentType)
    {
        var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(content);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(fileContent, "file", fileName);
        return form;
    }

    [Fact]
    public async Task Upload_then_mock_analysis_completes_and_is_labelled_mock()
    {
        var reportId = await CreateReportAsync();

        var upload = await _client.PostAsync(
            $"/api/reports/{reportId}/evidence", ImageForm(TinyPng, "blocked.png", "image/png"));
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);

        var evidence = await upload.Content.ReadFromJsonAsync<JsonElement>();
        var evidenceId = evidence.GetProperty("id").GetGuid();
        Assert.Equal("Pending", evidence.GetProperty("analysisStatus").GetString());

        // The stored file must exist under a random key, not the user file name.
        Assert.Single(Directory.GetFiles(_storageRoot));

        var analyze = await _client.PostAsync($"/api/evidence/{evidenceId}/analyze", null);
        Assert.Equal(HttpStatusCode.Accepted, analyze.StatusCode);
        var job = await analyze.Content.ReadFromJsonAsync<JsonElement>();
        var jobId = job.GetProperty("id").GetGuid();

        JsonElement jobStatus = default;
        for (var attempt = 0; attempt < 40; attempt++)
        {
            jobStatus = await _client.GetFromJsonAsync<JsonElement>($"/api/analysis-jobs/{jobId}");
            if (jobStatus.GetProperty("status").GetString() is "Completed" or "Failed")
            {
                break;
            }

            await Task.Delay(500);
        }

        Assert.Equal("Completed", jobStatus.GetProperty("status").GetString());

        var completed = await _client.GetFromJsonAsync<JsonElement>($"/api/evidence/{evidenceId}");
        Assert.Equal("Completed", completed.GetProperty("analysisStatus").GetString());
        Assert.Equal("Mock", completed.GetProperty("analysisProvider").GetString());
        Assert.Contains("not real AI inference", completed.GetProperty("analysisResultJson").GetString());
    }

    [Fact]
    public async Task Upload_rejects_non_image_content()
    {
        var reportId = await CreateReportAsync();
        var form = ImageForm("not an image"u8.ToArray(), "fake.png", "image/png");

        var response = await _client.PostAsync($"/api/reports/{reportId}/evidence", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_to_missing_report_returns_404()
    {
        var response = await _client.PostAsync(
            $"/api/reports/{Guid.NewGuid()}/evidence", ImageForm(TinyPng, "x.png", "image/png"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

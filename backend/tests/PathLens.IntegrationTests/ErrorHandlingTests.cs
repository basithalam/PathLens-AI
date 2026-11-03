using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PathLens.IntegrationTests;

public class ErrorHandlingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly string TestConnectionString = TestDatabase.Connection("pathlens_evidence_test");

    private static readonly byte[] TinyPng =
        Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8Xw8AAwMCAO+ip1sAAAAASUVORK5CYII=");

    private readonly HttpClient _client;

    public ErrorHandlingTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", TestConnectionString);
            builder.UseSetting("Ai:Provider", "Mock");
            builder.UseSetting("Storage:RootPath",
                Path.Combine(Path.GetTempPath(), "pathlens-error-tests", "shared"));
        }).CreateClient();
    }

    [Fact]
    public async Task Malformed_json_returns_400_not_500()
    {
        using var content = new StringContent("{not valid json", Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/reports", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_with_invalid_capture_time_returns_400()
    {
        var report = await _client.PostAsJsonAsync("/api/reports", new
        {
            title = "Capture time test",
            description = (string?)null,
            obstacleType = "Other",
            severity = "Low",
            latitude = 26.0,
            longitude = 93.5,
        });
        report.EnsureSuccessStatusCode();
        var reportId = (await report.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(TinyPng);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(fileContent, "file", "photo.png");
        form.Add(new StringContent("not-a-date"), "captureTimeUtc");

        var response = await _client.PostAsync($"/api/reports/{reportId}/evidence", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_sets_report_last_evidence_time_immediately()
    {
        var report = await _client.PostAsJsonAsync("/api/reports", new
        {
            title = "Freshness test",
            description = (string?)null,
            obstacleType = "Other",
            severity = "Low",
            latitude = 26.1,
            longitude = 93.6,
        });
        report.EnsureSuccessStatusCode();
        var reportId = (await report.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var before = await _client.GetFromJsonAsync<JsonElement>($"/api/reports/{reportId}");
        Assert.Equal(JsonValueKind.Null, before.GetProperty("lastEvidenceAt").ValueKind);

        var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(TinyPng);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(fileContent, "file", "photo.png");
        var upload = await _client.PostAsync($"/api/reports/{reportId}/evidence", form);
        upload.EnsureSuccessStatusCode();

        var after = await _client.GetFromJsonAsync<JsonElement>($"/api/reports/{reportId}");
        Assert.NotEqual(JsonValueKind.Null, after.GetProperty("lastEvidenceAt").ValueKind);
    }
}

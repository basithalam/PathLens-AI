using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PathLens.Infrastructure.Persistence;

namespace PathLens.IntegrationTests.Cameras;

public sealed class CameraApiTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private string? _adminToken;

    public CameraApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection",
                "Host=localhost;Port=5433;Database=pathlens_camera_test;Username=postgres;Timeout=5;Command Timeout=15");
            builder.UseSetting("Ai:Provider", "Mock");
            builder.UseSetting("Cameras:DailyAnalysisBudgetPerCamera", "2");
            builder.UseSetting("Storage:RootPath", Path.Combine(Path.GetTempPath(), "pathlens-camera-tests"));
            builder.UseSetting("Auth:JwtSigningKey", "camera-test-signing-key-at-least-32-chars-long!!");
        });
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PathLensDbContext>();
        await db.Database.MigrateAsync();
        await db.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE camera_sources, accessibility_reports RESTART IDENTITY CASCADE");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Create_without_login_is_401()
    {
        var response = await _client.PostAsJsonAsync("/api/cameras", ValidRequest());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Admin_creates_camera_and_it_is_listed_publicly()
    {
        var created = await CreateCameraAsync();

        Assert.True(created.GetProperty("enabled").GetBoolean());
        Assert.Equal("NycDot", created.GetProperty("provider").GetString());

        var list = await _client.GetFromJsonAsync<JsonElement>("/api/cameras");
        Assert.Contains(list.EnumerateArray(), item => item.GetProperty("id").GetGuid() == created.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Enabling_without_permission_note_is_400()
    {
        var request = ValidRequest();
        request["permissionNote"] = null;
        request["enabled"] = true;

        var response = await PostAsAdminAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Polling_a_local_feed_twice_dedupes_and_change_creates_camera_evidence()
    {
        using var feed = new LocalFeedServer();
        var reportId = await CreateNearbyReportAsync();

        var request = ValidRequest();
        request["feedImageUrl"] = feed.Url;
        request["latitude"] = 40.7000;
        request["longitude"] = -73.9000;
        var created = await PostAsAdminAsync(request);
        created.EnsureSuccessStatusCode();
        var camera = await created.Content.ReadFromJsonAsync<JsonElement>();
        var cameraId = camera.GetProperty("id").GetGuid();

        // First poll: changed image, but the screenshot's GPS metadata differs
        // from the camera position, which is expected: camera evidence uses the
        // camera location. The report is at the camera location, so this matches.
        var first = await PollAsync(cameraId);
        Assert.Equal("Changed", first.GetProperty("outcome").GetString());
        var firstEvidenceId = first.GetProperty("evidenceId").GetGuid();

        // Second poll: same image, no new evidence, no AI cost.
        var second = await PollAsync(cameraId);
        Assert.Equal("Unchanged", second.GetProperty("outcome").GetString());
        Assert.True(second.GetProperty("evidenceId").ValueKind == JsonValueKind.Null);

        // Change the feed image: a new evidence item must appear.
        feed.ChangeImage();
        var third = await PollAsync(cameraId);
        Assert.Equal("Changed", third.GetProperty("outcome").GetString());
        Assert.NotEqual(firstEvidenceId, third.GetProperty("evidenceId").GetGuid());

        var evidence = await _client.GetFromJsonAsync<JsonElement>($"/api/reports/{reportId}/evidence");
        var items = evidence.EnumerateArray().ToList();
        Assert.Equal(2, items.Count);
        Assert.All(items, item => Assert.Equal("CameraFeed", item.GetProperty("sourceType").GetString()));
    }

    [Fact]
    public async Task Poll_without_nearby_report_creates_unverified_camera_candidate_in_reviewer_queue()
    {
        using var feed = new LocalFeedServer();
        var request = ValidRequest();
        request["feedImageUrl"] = feed.Url;
        request["latitude"] = 41.5000;
        request["longitude"] = -74.5000;
        var created = await PostAsAdminAsync(request);
        created.EnsureSuccessStatusCode();
        var cameraId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var result = await PollAsync(cameraId);
        Assert.Equal("CandidateCreated", result.GetProperty("outcome").GetString());
        var evidenceId = result.GetProperty("evidenceId").GetGuid();

        // The candidate shows up as an unverified report owned by the camera.
        var reports = await _client.GetFromJsonAsync<JsonElement>("/api/reports?pageSize=50");
        var candidate = reports.GetProperty("items").EnumerateArray()
            .Single(item => item.GetProperty("cameraSourceId").ValueKind != JsonValueKind.Null &&
                            item.GetProperty("cameraSourceId").GetGuid() == cameraId);
        Assert.Equal("Unverified", candidate.GetProperty("verificationStatus").GetString());
        Assert.Contains("CameraDetected", candidate.GetProperty("title").GetString());

        var evidence = await _client.GetFromJsonAsync<JsonElement>(
            $"/api/reports/{candidate.GetProperty("id").GetGuid()}/evidence");
        Assert.Contains(evidence.EnumerateArray(), item => item.GetProperty("id").GetGuid() == evidenceId);

        // And it is in the reviewer queue, flagged as a camera candidate.
        var reviewerToken = await LoginAsync("reviewer@pathlens.local", "Reviewer@123");
        using var queueRequest = new HttpRequestMessage(HttpMethod.Get, "/api/reviewer/queue");
        queueRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", reviewerToken);
        var queueResponse = await _client.SendAsync(queueRequest);
        queueResponse.EnsureSuccessStatusCode();
        var queue = await queueResponse.Content.ReadFromJsonAsync<JsonElement>();
        var queueItem = queue.GetProperty("reports").EnumerateArray()
            .Single(item => item.GetProperty("id").GetGuid() == candidate.GetProperty("id").GetGuid());
        Assert.True(queueItem.GetProperty("isCameraCandidate").GetBoolean());
    }

    [Fact]
    public async Task Third_change_in_a_day_hits_budget_and_stores_nothing()
    {
        using var feed = new LocalFeedServer();
        var reportId = await CreateNearbyReportAsync(40.7200, -73.9200);

        var request = ValidRequest();
        request["feedImageUrl"] = feed.Url;
        request["latitude"] = 40.7200;
        request["longitude"] = -73.9200;
        var created = await PostAsAdminAsync(request);
        created.EnsureSuccessStatusCode();
        var cameraId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        // Test budget is 2 analysed snapshots per camera per day.
        Assert.Equal("Changed", (await PollAsync(cameraId)).GetProperty("outcome").GetString());
        feed.ChangeImage();
        Assert.Equal("Changed", (await PollAsync(cameraId)).GetProperty("outcome").GetString());
        feed.ChangeImage();
        var third = await PollAsync(cameraId);
        Assert.Equal("BudgetExceeded", third.GetProperty("outcome").GetString());
        Assert.True(third.GetProperty("evidenceId").ValueKind == JsonValueKind.Null);

        var evidence = await _client.GetFromJsonAsync<JsonElement>($"/api/reports/{reportId}/evidence");
        Assert.Equal(2, evidence.GetArrayLength());

        // The skip is reported on the source, not silent.
        var camera = await _client.GetFromJsonAsync<JsonElement>($"/api/cameras/{cameraId}");
        Assert.Contains("budget", camera.GetProperty("lastError").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Enabled_camera_that_never_succeeded_appears_as_stale_in_reviewer_queue()
    {
        var created = await CreateCameraAsync();
        var cameraId = created.GetProperty("id").GetGuid();

        var reviewerToken = await LoginAsync("reviewer@pathlens.local", "Reviewer@123");
        using var queueRequest = new HttpRequestMessage(HttpMethod.Get, "/api/reviewer/queue");
        queueRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", reviewerToken);
        var queueResponse = await _client.SendAsync(queueRequest);
        queueResponse.EnsureSuccessStatusCode();
        var queue = await queueResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Contains(
            queue.GetProperty("staleCameras").EnumerateArray(),
            camera => camera.GetProperty("id").GetGuid() == cameraId);
    }

    [Fact]
    public async Task Second_camera_snapshot_auto_compares_with_prior_after_analysis()
    {
        using var feed = new LocalFeedServer();
        var reportId = await CreateNearbyReportAsync(40.7100, -73.9100);

        var request = ValidRequest();
        request["feedImageUrl"] = feed.Url;
        request["latitude"] = 40.7100;
        request["longitude"] = -73.9100;
        var created = await PostAsAdminAsync(request);
        created.EnsureSuccessStatusCode();
        var cameraId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var first = await PollAsync(cameraId);
        Assert.Equal("Changed", first.GetProperty("outcome").GetString());
        var firstEvidenceId = first.GetProperty("evidenceId").GetGuid();

        feed.ChangeImage();
        var second = await PollAsync(cameraId);
        Assert.Equal("Changed", second.GetProperty("outcome").GetString());
        var secondEvidenceId = second.GetProperty("evidenceId").GetGuid();

        // Analysis runs in the background (mock provider); the auto-comparison
        // must appear without anyone calling POST /api/comparisons.
        JsonElement comparisons = default;
        for (var attempt = 0; attempt < 30; attempt++)
        {
            comparisons = await _client.GetFromJsonAsync<JsonElement>($"/api/reports/{reportId}/comparisons");
            if (comparisons.GetArrayLength() > 0)
            {
                break;
            }

            await Task.Delay(1000);
        }

        Assert.Equal(1, comparisons.GetArrayLength());
        var comparison = comparisons[0];
        Assert.Equal(firstEvidenceId, comparison.GetProperty("earlierEvidenceId").GetGuid());
        Assert.Equal(secondEvidenceId, comparison.GetProperty("laterEvidenceId").GetGuid());
        // Mock analysis reports Unknown with zero confidence, so the honest
        // engine outcome is Inconclusive — never a removal claim.
        Assert.Equal("Inconclusive", comparison.GetProperty("outcome").GetString());
    }

    private Dictionary<string, object?> ValidRequest() => new()
    {
        ["provider"] = "NycDot",
        ["externalId"] = $"cam-{Guid.NewGuid():N}",
        ["label"] = "Test camera",
        ["city"] = "New York",
        ["latitude"] = 40.7644,
        ["longitude"] = -73.9733,
        ["feedImageUrl"] = "https://example.test/cam.jpg",
        ["pollCadenceSeconds"] = 60,
        ["permissionNote"] = "Public DOT feed, terms noted for this test.",
        ["enabled"] = true,
    };

    private async Task<JsonElement> CreateCameraAsync()
    {
        var response = await PostAsAdminAsync(ValidRequest());
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<HttpResponseMessage> PostAsAdminAsync(Dictionary<string, object?> request)
    {
        _adminToken ??= await LoginAsync("admin@pathlens.local", "Admin@123");
        using var message = new HttpRequestMessage(HttpMethod.Post, "/api/cameras")
        {
            Content = JsonContent.Create(request),
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);
        return await _client.SendAsync(message);
    }

    private async Task<JsonElement> PollAsync(Guid cameraId)
    {
        _adminToken ??= await LoginAsync("admin@pathlens.local", "Admin@123");
        using var message = new HttpRequestMessage(HttpMethod.Post, $"/api/cameras/{cameraId}/poll");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);
        var response = await _client.SendAsync(message);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<Guid> CreateNearbyReportAsync(double latitude = 40.7000, double longitude = -73.9000)
    {
        var response = await _client.PostAsJsonAsync("/api/reports", new
        {
            title = "Report near test camera",
            description = "Beside the test feed.",
            category = "Other",
            latitude,
            longitude,
            city = "New York",
        });
        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<JsonElement>();
        return report.GetProperty("id").GetGuid();
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("accessToken").GetString()!;
    }

    /// <summary>A local "camera feed": serves a tiny valid JPEG; ChangeImage swaps pixels.</summary>
    private sealed class LocalFeedServer : IDisposable
    {
        private readonly HttpListener _listener = new();
        private readonly CancellationTokenSource _cts = new();
        private byte[] _image = CreateJpeg(red: 200);

        public string Url { get; }

        public LocalFeedServer()
        {
            var port = GetFreePort();
            Url = $"http://127.0.0.1:{port}/cam.jpg";
            _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            _listener.Start();
            _ = Task.Run(ServeAsync);
        }

        private int _variant;

        public void ChangeImage() => _image = CreateJpeg(red: (byte)(30 + ++_variant * 40));

        private async Task ServeAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    context.Response.ContentType = "image/jpeg";
                    await context.Response.OutputStream.WriteAsync(_image, _cts.Token);
                    context.Response.Close();
                }
                catch
                {
                    break;
                }
            }
        }

        private static byte[] CreateJpeg(byte red)
        {
            // Minimal 1x1 JPEG whose bytes vary with the red value.
            using var ms = new MemoryStream();
            var header = Convert.FromBase64String(
                "/9j/4AAQSkZJRgABAQEASABIAAD/2wBDAP//////////////////////////////////////////////////////////////////////////////////////2wBDAf//////////////////////////////////////////////////////////////////////////////////////wAARCAABAAEDASIAAhEBAxEB/8QAFAABAAAAAAAAAAAAAAAAAAAACf/EABQQAQAAAAAAAAAAAAAAAAAAAAD/xAAUAQEAAAAAAAAAAAAAAAAAAAAA/8QAFBEBAAAAAAAAAAAAAAAAAAAAAP/aAAwDAQACEQMRAD8AvwH/2Q==");
            ms.Write(header);
            ms.WriteByte(red);
            ms.WriteByte(0xFF);
            ms.WriteByte(0xD9);
            return ms.ToArray();
        }

        private static int GetFreePort()
        {
            var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        public void Dispose()
        {
            _cts.Cancel();
            _listener.Stop();
            _listener.Close();
        }
    }
}

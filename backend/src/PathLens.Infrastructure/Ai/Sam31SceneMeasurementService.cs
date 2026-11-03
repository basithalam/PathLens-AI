using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PathLens.Application.Ai;

namespace PathLens.Infrastructure.Ai;

/// <summary>
/// Measures how much of the walkable region in a photo is occupied, using SAM 3.1 concept segmentation
/// on the Meta Model API (POST /v1/responses - the chat completions endpoint answers 404 for this model).
///
/// Two prompts are issued per image: one for the walkable surface and one for the obstacle the analysis
/// reported. The share is obstacle-area-over-walkway, so a photo that is mostly sky cannot make a
/// blockage look small.
///
/// The contract was measured, not assumed (2026-11-03): SAM answers with
/// <c>&lt;|box;x1=..;y1=..;x2=..;y2=..;w=..;h=..|&gt;</c> tokens inside <c>output[0].content[0].text</c>,
/// with <b>no score and no confidence</b>. Nothing here invents either.
///
/// Returns null - never zero - when the walkway box or the obstacle box is missing. That case is real:
/// on a photo of three fallen scooters the bare concept "scooter" returned no box at all while
/// "scooters blocking the sidewalk" returned two. Reporting that as "0% occupied" would tell a
/// wheelchair user the path is clear, which is the one thing this product promises not to do.
/// </summary>
public sealed class Sam31SceneMeasurementService(
    IHttpClientFactory httpClientFactory,
    IOptions<Sam31Options> options) : ISceneMeasurementService
{
    private readonly Sam31Options _options = options.Value;

    public string ProviderName => "SAM 3.1";

    public async Task<ObstructionMeasurement?> MeasureAsync(
        ImageAnalysisRequest request,
        string obstacleConcept,
        CancellationToken cancellationToken)
    {
        var apiKey = FirstNonEmpty(_options.ApiKey, _options.FallbackApiKey);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "SAM 3.1 is enabled but no key is configured - set Sam31:ApiKey or MetaAi:ApiKey " +
                "(server-side env Sam31__ApiKey / MetaAi__ApiKey, or GitHub Secret META_AI_API_KEY). " +
                "Measurement returns unknown rather than guessing.");
        }

        var dataUri = $"data:{request.ContentType};base64,{Convert.ToBase64String(request.ImageBytes)}";

        var walkwayBoxes = await QueryBoxesAsync(apiKey, _options.WalkwayConcept, dataUri, cancellationToken);
        var obstacleBoxes = await QueryBoxesAsync(apiKey, obstacleConcept, dataUri, cancellationToken);

        var percent = ObstructionGeometry.WalkwayOccupancyPercent(walkwayBoxes, obstacleBoxes);
        if (percent is null)
        {
            return null;
        }

        var frame = walkwayBoxes.Count > 0 ? walkwayBoxes[0] : obstacleBoxes[0];

        return new ObstructionMeasurement(
            percent.Value,
            obstacleBoxes.Count,
            _options.WalkwayConcept,
            obstacleConcept,
            frame.FrameWidth,
            frame.FrameHeight,
            obstacleBoxes,
            walkwayBoxes,
            ProviderName,
            _options.Model,
            DateTimeOffset.UtcNow);
    }

    private async Task<IReadOnlyList<PixelBox>> QueryBoxesAsync(
        string apiKey,
        string concept,
        string dataUri,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            model = _options.Model,
            input = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "input_text", text = concept },
                        new { type = "input_image", image_url = dataUri },
                    },
                },
            },
        };

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(5, _options.TimeoutSeconds)));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        var client = httpClientFactory.CreateClient(nameof(Sam31SceneMeasurementService));
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(httpRequest, linked.Token);
        var body = await response.Content.ReadAsStringAsync(linked.Token);

        if (!response.IsSuccessStatusCode)
        {
            var snippet = body.Length > 600 ? body[..600] + "..." : body;
            throw new HttpRequestException(
                $"SAM 3.1 request failed with status {(int)response.StatusCode} at {_options.Endpoint} " +
                $"(model {_options.Model}, concept \"{concept}\"). Body: {snippet}");
        }

        return ObstructionGeometry.ParseBoxes(ExtractText(body));
    }

    /// <summary>
    /// Reads output[0].content[*].text. The mask tokens are deliberately ignored: the box coordinates are
    /// enough for the share, and a mask would invite a precision the response does not actually support.
    /// </summary>
    private static string ExtractText(string responseBody)
    {
        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var text = new System.Text.StringBuilder();

            if (document.RootElement.TryGetProperty("output", out var output) &&
                output.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in output.EnumerateArray())
                {
                    if (!item.TryGetProperty("content", out var content) ||
                        content.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    foreach (var part in content.EnumerateArray())
                    {
                        if (part.TryGetProperty("text", out var t) && t.ValueKind == JsonValueKind.String)
                        {
                            text.Append(t.GetString());
                        }
                    }
                }
            }

            return text.ToString();
        }
        catch (JsonException)
        {
            return string.Empty;
        }
    }

    private static string? FirstNonEmpty(string? preferred, string? fallback) =>
        !string.IsNullOrWhiteSpace(preferred) ? preferred : fallback;
}

/// <summary>
/// Measurement stand-in for tests and for <c>Ai:Provider=Mock</c>. Always reports unknown, so a demo
/// running on the mock pipeline can never show a fabricated occupancy figure.
/// </summary>
public sealed class MockSceneMeasurementService : ISceneMeasurementService
{
    public string ProviderName => "Mock";

    public Task<ObstructionMeasurement?> MeasureAsync(
        ImageAnalysisRequest request,
        string obstacleConcept,
        CancellationToken cancellationToken) => Task.FromResult<ObstructionMeasurement?>(null);
}

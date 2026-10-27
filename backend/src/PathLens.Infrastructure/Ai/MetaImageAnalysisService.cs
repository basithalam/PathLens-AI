using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PathLens.Application.Ai;

namespace PathLens.Infrastructure.Ai;

/// <summary>
/// Real Meta AI adapter over an OpenAI-compatible chat completions endpoint
/// (for example the Llama API compatibility endpoint). Sends the image as a
/// base64 data URI and requires the model to answer with the schema 1.0 JSON
/// contract only. Without configuration it fails loudly; it never invents a
/// result. Gate A (a live image test with a real key) must pass before this
/// is used in a demo.
/// </summary>
public sealed class MetaImageAnalysisService(
    IHttpClientFactory httpClientFactory,
    IOptions<MetaAiOptions> options) : IImageAnalysisService
{
    private readonly MetaAiOptions _options = options.Value;

    public string ProviderName => "Meta";

    public async Task<ImageAnalysisResult> AnalyzeAsync(
        ImageAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Endpoint) ||
            string.IsNullOrWhiteSpace(_options.ApiKey) ||
            string.IsNullOrWhiteSpace(_options.Model))
        {
            throw new InvalidOperationException(
                "Meta AI is not configured. Set MetaAi:Endpoint, MetaAi:Model, and the server-side MetaAi:ApiKey before running real analysis.");
        }

        var dataUri = $"data:{request.ContentType};base64,{Convert.ToBase64String(request.ImageBytes)}";

        var payload = new
        {
            model = _options.Model,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "text",
                            text = """
                                You analyze a photo of a pedestrian path for possible accessibility obstacles.
                                Answer with JSON only, matching this schema exactly:
                                {"schemaVersion":"1.0","obstacleType":"BlockedSidewalk|BrokenSurface|MissingOrBlockedRamp|StairsOrStep|ConstructionBarrier|Other|Unknown","visibleEvidence":["what is visibly seen"],"limitations":["what cannot be seen or is uncertain"],"confidence":0.0,"humanReviewRequired":true}
                                Describe only what is visible. Do not claim GPS location or capture time from the image.
                                If the image does not show the path clearly, use obstacleType "Unknown" and say why in limitations.
                                """,
                        },
                        new { type = "image_url", image_url = new { url = dataUri } },
                    },
                },
            },
        };

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(5, _options.TimeoutSeconds)));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        var client = httpClientFactory.CreateClient(nameof(MetaImageAnalysisService));
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        httpRequest.Content = JsonContent.Create(payload);

        using var response = await client.SendAsync(httpRequest, linked.Token);
        var body = await response.Content.ReadAsStringAsync(linked.Token);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Meta AI request failed with status {(int)response.StatusCode}.");
        }

        var content = ExtractMessageContent(body);
        string error = "empty response content.";
        if (content is null ||
            !ImageAnalysisResultParser.TryParse(content, ProviderName, _options.Model, DateTimeOffset.UtcNow, out var result, out error))
        {
            throw new InvalidOperationException($"Meta AI returned an unusable analysis: {error}");
        }

        return result!;
    }

    private static string? ExtractMessageContent(string responseBody)
    {
        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var choices = document.RootElement.GetProperty("choices");
            var content = choices[0].GetProperty("message").GetProperty("content");

            if (content.ValueKind == JsonValueKind.String)
            {
                return content.GetString();
            }

            // Some providers return content as an array of parts.
            if (content.ValueKind == JsonValueKind.Array)
            {
                foreach (var part in content.EnumerateArray())
                {
                    if (part.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                    {
                        return text.GetString();
                    }
                }
            }

            return null;
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or IndexOutOfRangeException)
        {
            return null;
        }
    }
}

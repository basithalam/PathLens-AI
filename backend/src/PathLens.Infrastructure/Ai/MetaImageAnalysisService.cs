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
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(_options.Endpoint)) missing.Add("MetaAi:Endpoint (e.g. https://api.llama.com/v1/chat/completions or https://api.groq.com/openai/v1/chat/completions)");
        if (string.IsNullOrWhiteSpace(_options.Model)) missing.Add("MetaAi:Model (e.g. Llama-4-Maverick-17B-128E-Instruct-FP8 or meta-llama/llama-4-maverick-17b-128e-instruct for Groq)");
        if (string.IsNullOrWhiteSpace(_options.ApiKey)) missing.Add("MetaAi:ApiKey (server-side env MetaAi__ApiKey / GitHub Secret META_AI_API_KEY — never committed, see docs/SECURITY.md)");
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Meta AI is not configured — missing: {string.Join(", ", missing)}. " +
                "Set all three as server-side env (local: . .\\.local\\set-meta-env.ps1, production: GitHub Secrets META_AI_ENDPOINT/MODEL/API_KEY → .env via deploy-hetzner.yml). " +
                "Without this, analysis jobs fail honestly (502) and UI shows 'Analysis failed'. For tests use Ai:Provider=Mock (labelled Demo/mock — not real AI). Phase G1 fix 2026-10-31: old api.meta.ai/compat/v1 returned 404.");
        }

        // Downscale and strip EXIF (including embedded GPS) before sending.
        var (preparedBytes, preparedContentType) =
            ImageDownscaler.DownscaleForAnalysis(request.ImageBytes, request.ContentType);
        var dataUri = $"data:{preparedContentType};base64,{Convert.ToBase64String(preparedBytes)}";

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
            // G1 improvement: include truncated body for debugging 401/404 (key/host issues) without leaking key
            var snippet = body.Length > 600 ? body.Substring(0, 600) + "..." : body;
            throw new HttpRequestException(
                $"Meta AI request failed with status {(int)response.StatusCode} at {_options.Endpoint} (model {_options.Model}). Body: {snippet}");
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

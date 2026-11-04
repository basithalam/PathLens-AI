using System.Text.Json;
using PathLens.Domain.Reports;

namespace PathLens.Application.Ai;

/// <summary>Validates raw provider JSON against the schema 1.0 contract.</summary>
public static class ImageAnalysisResultParser
{
    public const string CurrentSchemaVersion = "1.0";

    /// <summary>
    /// Safety guardrail (measured 2026-11-04): a confident-sounding type with weak confidence is an
    /// abstention, not a finding. Below 0.35 a named obstacle becomes Unknown so the UI never shows
    /// a red "incorrect" that was really the model saying "I'm not sure". Threshold is code, not config.
    /// </summary>
    public const double AbstainBelowConfidence = 0.35;

    public static bool TryParse(
        string json,
        string provider,
        string? model,
        DateTimeOffset analyzedAt,
        out ImageAnalysisResult? result,
        out string error)
    {
        result = null;
        error = string.Empty;

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            var schemaVersion = GetString(root, "schemaVersion");
            if (schemaVersion != CurrentSchemaVersion)
            {
                error = $"Unsupported analysis schema version '{schemaVersion ?? "<missing>"}'.";
                return false;
            }

            var obstacleType = GetString(root, "obstacleType");
            if (obstacleType is null || !Enum.TryParse<ObstacleType>(obstacleType, ignoreCase: true, out _))
            {
                error = $"Unknown obstacle type '{obstacleType ?? "<missing>"}'.";
                return false;
            }

            if (!root.TryGetProperty("confidence", out var confidenceElement) ||
                !confidenceElement.TryGetDouble(out var confidence) ||
                confidence is < 0 or > 1)
            {
                error = "Confidence is missing or outside the 0..1 range.";
                return false;
            }

            // Low-confidence guardrail: a named type below threshold is an abstention.
            if (confidence < AbstainBelowConfidence &&
                !string.Equals(obstacleType, nameof(ObstacleType.Unknown), StringComparison.OrdinalIgnoreCase))
            {
                obstacleType = nameof(ObstacleType.Unknown);
            }

            result = new ImageAnalysisResult(
                schemaVersion,
                obstacleType,
                GetStringList(root, "visibleEvidence"),
                GetStringList(root, "limitations"),
                confidence,
                GetBool(root, "humanReviewRequired"),
                provider,
                model,
                analyzedAt);
            return true;
        }
        catch (JsonException ex)
        {
            error = $"Provider returned invalid JSON: {ex.Message}";
            return false;
        }
    }

    public static string Serialize(ImageAnalysisResult result) => JsonSerializer.Serialize(new
    {
        schemaVersion = result.SchemaVersion,
        obstacleType = result.ObstacleType,
        visibleEvidence = result.VisibleEvidence,
        limitations = result.Limitations,
        confidence = result.Confidence,
        humanReviewRequired = result.HumanReviewRequired,
        provider = result.Provider,
        model = result.Model,
        analyzedAt = result.AnalyzedAt,
    });

    private static string? GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static bool GetBool(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;

    private static IReadOnlyList<string> GetStringList(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString()!)
            .ToList();
    }
}

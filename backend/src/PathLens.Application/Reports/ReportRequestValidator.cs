using PathLens.Domain.Reports;

namespace PathLens.Application.Reports;

/// <summary>Server-side validation. Client validation is never a security boundary.</summary>
public static class ReportRequestValidator
{
    public const int MaxTitleLength = 200;
    public const int MaxDescriptionLength = 2000;

    public static Dictionary<string, string[]> ValidateCreate(CreateReportRequest request)
    {
        var errors = ValidateCommon(request.Title, request.Description, request.ObstacleType, request.Severity);
        ValidateSurface(request.Surface, errors);

        if (!AccessibilityReport.IsValidLatitude(request.Latitude))
        {
            errors["latitude"] = ["Latitude must be between -90 and 90."];
        }

        if (!AccessibilityReport.IsValidLongitude(request.Longitude))
        {
            errors["longitude"] = ["Longitude must be between -180 and 180."];
        }

        return errors;
    }

    public static Dictionary<string, string[]> ValidateUpdate(UpdateReportRequest request)
    {
        var errors = ValidateCommon(request.Title, request.Description, request.ObstacleType, request.Severity);
        ValidateSurface(request.Surface, errors);
        return errors;
    }

    private static void ValidateSurface(string? surface, Dictionary<string, string[]> errors)
    {
        if (!string.IsNullOrWhiteSpace(surface) &&
            !Enum.TryParse<SurfaceCondition>(surface, ignoreCase: true, out _))
        {
            errors["surface"] =
                [$"Surface must be one of: {string.Join(", ", Enum.GetNames<SurfaceCondition>())}, or omitted when unknown."];
        }
    }

    private static Dictionary<string, string[]> ValidateCommon(
        string? title, string? description, string? obstacleType, string? severity)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(title))
        {
            errors["title"] = ["Title is required."];
        }
        else if (title.Trim().Length > MaxTitleLength)
        {
            errors["title"] = [$"Title must be at most {MaxTitleLength} characters."];
        }

        if (description is not null && description.Length > MaxDescriptionLength)
        {
            errors["description"] = [$"Description must be at most {MaxDescriptionLength} characters."];
        }

        if (!string.IsNullOrWhiteSpace(obstacleType) &&
            !Enum.TryParse<ObstacleType>(obstacleType, ignoreCase: true, out _))
        {
            errors["obstacleType"] =
                [$"Obstacle type must be one of: {string.Join(", ", Enum.GetNames<ObstacleType>())}."];
        }

        if (!string.IsNullOrWhiteSpace(severity) &&
            !Enum.TryParse<SeverityLevel>(severity, ignoreCase: true, out _))
        {
            errors["severity"] =
                [$"Severity must be one of: {string.Join(", ", Enum.GetNames<SeverityLevel>())}."];
        }

        return errors;
    }

    public static ObstacleType ParseObstacleType(string? value) =>
        Enum.TryParse<ObstacleType>(value, ignoreCase: true, out var parsed) ? parsed : ObstacleType.Unknown;

    public static SeverityLevel ParseSeverity(string? value) =>
        Enum.TryParse<SeverityLevel>(value, ignoreCase: true, out var parsed) ? parsed : SeverityLevel.Unknown;
}

namespace PathLens.Application.Evidence;

/// <summary>
/// Upload validation based on file content (magic bytes), never on the
/// file extension alone.
/// </summary>
public static class ImageFileValidator
{
    public const long MaxFileSizeBytes = 10 * 1024 * 1024;

    public static readonly IReadOnlySet<string> AllowedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
    };

    public static Dictionary<string, string[]> Validate(byte[] content, string? contentType, long declaredSize)
    {
        var errors = new Dictionary<string, string[]>();

        if (content.Length == 0 || declaredSize == 0)
        {
            errors["file"] = ["The uploaded file is empty."];
            return errors;
        }

        if (declaredSize > MaxFileSizeBytes || content.Length > MaxFileSizeBytes)
        {
            errors["file"] = [$"File is too large. The maximum size is {MaxFileSizeBytes / (1024 * 1024)} MB."];
            return errors;
        }

        if (contentType is null || !AllowedContentTypes.Contains(contentType))
        {
            errors["file"] = ["Only JPEG, PNG, or WebP images are allowed."];
            return errors;
        }

        var detected = DetectContentType(content);
        if (detected is null || !string.Equals(detected, contentType, StringComparison.OrdinalIgnoreCase))
        {
            errors["file"] = ["The file content does not match an allowed image type."];
        }

        return errors;
    }

    public static string? DetectContentType(ReadOnlySpan<byte> content)
    {
        if (content.Length >= 3 && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF)
        {
            return "image/jpeg";
        }

        if (content.Length >= 8 &&
            content[0] == 0x89 && content[1] == 0x50 && content[2] == 0x4E && content[3] == 0x47 &&
            content[4] == 0x0D && content[5] == 0x0A && content[6] == 0x1A && content[7] == 0x0A)
        {
            return "image/png";
        }

        if (content.Length >= 12 &&
            content[0] == (byte)'R' && content[1] == (byte)'I' && content[2] == (byte)'F' && content[3] == (byte)'F' &&
            content[8] == (byte)'W' && content[9] == (byte)'E' && content[10] == (byte)'B' && content[11] == (byte)'P')
        {
            return "image/webp";
        }

        return null;
    }
}

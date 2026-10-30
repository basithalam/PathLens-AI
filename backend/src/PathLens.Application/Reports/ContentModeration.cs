namespace PathLens.Application.Reports;

/// <summary>H5.1 Basic moderation — profanity blocklist + NSFW placeholder flag.</summary>
public static class ContentModeration
{
    // Minimal blocklist (English + roman Bangla slurs). Extend per locale; not exhaustive.
    private static readonly string[] Blocklist = ["fuck", "shit", "bastard", "chutiya", "madarchod"];

    public static bool ContainsProfanity(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var lower = text.ToLowerInvariant();
        return Blocklist.Any(w => lower.Contains(w));
    }

    // Placeholder NSFW flag — real detector (e.g., NSFWJS/YOLO) would run on image bytes.
    public static bool FlagNsfwPlaceholder(byte[]? imageBytes) => false;

    public static string? ModerateTitle(string title)
    {
        if (ContainsProfanity(title)) return "Title contains blocked language and will be reviewed.";
        return null;
    }

    public static string? ModerateDescription(string? description)
    {
        if (ContainsProfanity(description)) return "Description contains blocked language and will be queued for review.";
        return null;
    }
}

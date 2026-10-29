namespace PathLens.Domain.Reports;

public enum FeedbackKind
{
    StillVisible,
    NotInNewPhoto,
    WrongOrStale,
    NotSure
}

public static class FeedbackKindExtensions
{
    public static string ToAuditAction(this FeedbackKind kind) => kind switch
    {
        FeedbackKind.StillVisible => "FeedbackStillVisible",
        FeedbackKind.NotInNewPhoto => "FeedbackNotInNewPhoto",
        FeedbackKind.WrongOrStale => "FeedbackWrongOrStale",
        FeedbackKind.NotSure => "FeedbackNotSure",
        _ => "FeedbackUnknown"
    };

    public static string ToDisplayName(this FeedbackKind kind) => kind switch
    {
        FeedbackKind.StillVisible => "Still visible",
        FeedbackKind.NotInNewPhoto => "Not in new photo",
        FeedbackKind.WrongOrStale => "Information wrong or old",
        FeedbackKind.NotSure => "Not sure",
        _ => kind.ToString()
    };
}

namespace PathLens.Application.Reports;

public sealed record CreateFeedbackRequest(
    string Kind,
    string? Note,
    string? EvidenceId);

public sealed record FeedbackResponse(
    Guid Id,
    Guid ReportId,
    string Kind,
    string? Note,
    string Action,
    DateTimeOffset CreatedAt);

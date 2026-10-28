namespace PathLens.Application.Comparisons;

public sealed record CreateComparisonRequest(Guid EarlierEvidenceId, Guid LaterEvidenceId);

public sealed record ReviewComparisonRequest(string? ReviewOutcome, string? Reason);

public sealed record ComparisonResponse(
    Guid Id,
    Guid ReportId,
    Guid EarlierEvidenceId,
    Guid LaterEvidenceId,
    string Outcome,
    string EffectiveOutcome,
    string Explanation,
    string SupportingEvidenceJson,
    string? ModelVersion,
    DateTimeOffset CreatedAt,
    string? ReviewedOutcome,
    string? ReviewReason,
    DateTimeOffset? ReviewedAt,
    Guid? ReviewedByUserId);

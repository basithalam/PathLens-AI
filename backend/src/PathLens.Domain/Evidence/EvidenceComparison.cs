using PathLens.Domain.Reports;

namespace PathLens.Domain.Evidence;

/// <summary>
/// Comparison of two evidence items from the same report. The original
/// engine outcome is never overwritten: a human review is stored separately.
/// </summary>
public sealed class EvidenceComparison
{
    private EvidenceComparison()
    {
    }

    public Guid Id { get; private set; }
    public Guid ReportId { get; private set; }
    public Guid EarlierEvidenceId { get; private set; }
    public Guid LaterEvidenceId { get; private set; }
    public EvidenceComparisonOutcome Outcome { get; private set; }
    public string Explanation { get; private set; } = string.Empty;
    public string SupportingEvidenceJson { get; private set; } = string.Empty;
    public string? ModelVersion { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public EvidenceComparisonOutcome? ReviewedOutcome { get; private set; }
    public string? ReviewReason { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }

    public EvidenceComparisonOutcome EffectiveOutcome => ReviewedOutcome ?? Outcome;

    public static EvidenceComparison Create(
        Guid reportId,
        Guid earlierEvidenceId,
        Guid laterEvidenceId,
        EvidenceComparisonOutcome outcome,
        string explanation,
        string supportingEvidenceJson,
        string? modelVersion,
        DateTimeOffset now) => new()
        {
            Id = Guid.NewGuid(),
            ReportId = reportId,
            EarlierEvidenceId = earlierEvidenceId,
            LaterEvidenceId = laterEvidenceId,
            Outcome = outcome,
            Explanation = explanation,
            SupportingEvidenceJson = supportingEvidenceJson,
            ModelVersion = modelVersion,
            CreatedAt = now,
        };

    public void Review(EvidenceComparisonOutcome outcome, string reason, DateTimeOffset now, Guid? reviewedByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A review reason is required.", nameof(reason));
        }

        ReviewedOutcome = outcome;
        ReviewReason = reason.Trim();
        ReviewedAt = now;
        ReviewedByUserId = reviewedByUserId;
    }
}

using PathLens.Domain.Reports;

namespace PathLens.Domain.Evidence;

/// <summary>Database-backed analysis job. Status is polled by the frontend.</summary>
public sealed class AnalysisJob
{
    private AnalysisJob()
    {
    }

    public Guid Id { get; private set; }
    public Guid EvidenceItemId { get; private set; }
    public AnalysisStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public static AnalysisJob Create(Guid evidenceItemId, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(),
        EvidenceItemId = evidenceItemId,
        Status = AnalysisStatus.Pending,
        Attempts = 0,
        CreatedAt = now,
    };

    public void MarkProcessing(DateTimeOffset now)
    {
        Status = AnalysisStatus.Processing;
        Attempts++;
        StartedAt = now;
        ErrorMessage = null;
    }

    public void MarkCompleted(DateTimeOffset now)
    {
        Status = AnalysisStatus.Completed;
        CompletedAt = now;
        ErrorMessage = null;
    }

    public void MarkFailed(string error, DateTimeOffset now)
    {
        Status = AnalysisStatus.Failed;
        ErrorMessage = error;
        CompletedAt = now;
    }

    public void Requeue(string error)
    {
        Status = AnalysisStatus.Pending;
        ErrorMessage = error;
        CompletedAt = null;
    }
}

namespace PathLens.Domain.Cameras;

public enum CameraProvider
{
    NycDot,
    FiveOneOneNy,
    Mapillary,
    ManualAuthorised,
}

/// <summary>
/// One allow-listed public or authorised camera. A source can be enabled
/// only when its permission/terms note is recorded — that rule is enforced
/// here and at the API, not left to convention.
/// </summary>
public sealed class CameraSource
{
    public const int MinimumCadenceSeconds = 30;

    private CameraSource()
    {
    }

    public Guid Id { get; private set; }
    public CameraProvider Provider { get; private set; }
    public string ExternalId { get; private set; } = string.Empty;
    public string Label { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public string FeedImageUrl { get; private set; } = string.Empty;
    public string? DirectionLabel { get; private set; }
    public int CadenceSeconds { get; private set; }
    public bool Enabled { get; private set; }
    public string? PermissionNote { get; private set; }
    public DateTimeOffset? LastAttemptAt { get; private set; }
    public DateTimeOffset? LastSuccessAt { get; private set; }
    public DateTimeOffset? LastChangeAt { get; private set; }
    public string? LastError { get; private set; }
    public int ConsecutiveFailures { get; private set; }
    public string? LastImageHash { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public bool HasPermissionNote => !string.IsNullOrWhiteSpace(PermissionNote);

    public bool CanEnable => HasPermissionNote;

    public static CameraSource Create(
        CameraProvider provider,
        string externalId,
        string label,
        string city,
        double latitude,
        double longitude,
        string feedImageUrl,
        string? directionLabel,
        int cadenceSeconds,
        string? permissionNote,
        DateTimeOffset now)
    {
        if (latitude is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");
        }

        if (longitude is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");
        }

        return new CameraSource
        {
            Id = Guid.NewGuid(),
            Provider = provider,
            ExternalId = externalId.Trim(),
            Label = label.Trim(),
            City = city.Trim(),
            Latitude = latitude,
            Longitude = longitude,
            FeedImageUrl = feedImageUrl.Trim(),
            DirectionLabel = string.IsNullOrWhiteSpace(directionLabel) ? null : directionLabel.Trim(),
            CadenceSeconds = Math.Max(MinimumCadenceSeconds, cadenceSeconds),
            Enabled = false,
            PermissionNote = string.IsNullOrWhiteSpace(permissionNote) ? null : permissionNote.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void SetEnabled(bool enabled, DateTimeOffset now)
    {
        if (enabled && !CanEnable)
        {
            throw new InvalidOperationException(
                "A camera source cannot be enabled until its permission/terms note is recorded.");
        }

        Enabled = enabled;
        UpdatedAt = now;
    }

    public void UpdateCadence(int cadenceSeconds, DateTimeOffset now)
    {
        CadenceSeconds = Math.Max(MinimumCadenceSeconds, cadenceSeconds);
        UpdatedAt = now;
    }

    public void UpdatePermissionNote(string? note, DateTimeOffset now)
    {
        PermissionNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (!HasPermissionNote)
        {
            Enabled = false;
        }

        UpdatedAt = now;
    }

    /// <summary>Effective wait between polls, with exponential backoff on failures (capped).</summary>
    public TimeSpan EffectivePollInterval()
    {
        var multiplier = 1 << Math.Min(ConsecutiveFailures, 5);
        return TimeSpan.FromSeconds(CadenceSeconds * multiplier);
    }

    public bool IsDue(DateTimeOffset now) =>
        Enabled && (LastAttemptAt is null || now - LastAttemptAt.Value >= EffectivePollInterval());

    public bool IsStale(DateTimeOffset now, TimeSpan staleAfter) =>
        LastSuccessAt is null || now - LastSuccessAt.Value > staleAfter;

    public void RecordAttempt(DateTimeOffset now)
    {
        LastAttemptAt = now;
        UpdatedAt = now;
    }

    public void RecordSuccess(DateTimeOffset now, string imageHash, bool changed)
    {
        LastSuccessAt = now;
        LastError = null;
        ConsecutiveFailures = 0;
        if (changed)
        {
            LastImageHash = imageHash;
            LastChangeAt = now;
        }

        UpdatedAt = now;
    }

    public void RecordFailure(DateTimeOffset now, string error)
    {
        LastError = error;
        ConsecutiveFailures++;
        UpdatedAt = now;
    }
}

namespace PathLens.Domain.Reports;

/// <summary>
/// One reported potential accessibility obstacle at a place.
/// Location is stored as latitude/longitude until PostGIS is available in
/// every environment (Phase 4 spatial work); never treat user-supplied
/// coordinates as a verified fact about the real world.
/// </summary>
public sealed class AccessibilityReport
{
    private AccessibilityReport()
    {
    }

    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public ObstacleType ObstacleType { get; private set; }
    public SeverityLevel Severity { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public ReportStatus Status { get; private set; }
    public VerificationStatus VerificationStatus { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? LastEvidenceAt { get; private set; }

    public static bool IsValidLatitude(double value) => value is >= -90 and <= 90;
    public static bool IsValidLongitude(double value) => value is >= -180 and <= 180;

    public static AccessibilityReport Create(
        string title,
        string? description,
        ObstacleType obstacleType,
        SeverityLevel severity,
        double latitude,
        double longitude,
        Guid? createdByUserId,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        if (!IsValidLatitude(latitude))
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");
        }

        if (!IsValidLongitude(longitude))
        {
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");
        }

        return new AccessibilityReport
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            ObstacleType = obstacleType,
            Severity = severity,
            Latitude = latitude,
            Longitude = longitude,
            Status = ReportStatus.Active,
            VerificationStatus = VerificationStatus.Unverified,
            CreatedByUserId = createdByUserId,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void RecordEvidence(DateTimeOffset evidenceUploadedAt)
    {
        LastEvidenceAt = evidenceUploadedAt;
        UpdatedAt = evidenceUploadedAt;
    }

    public void Update(
        string title,
        string? description,
        ObstacleType obstacleType,
        SeverityLevel severity,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        ObstacleType = obstacleType;
        Severity = severity;
        UpdatedAt = now;
    }

    /// <summary>Great-circle distance in metres (Haversine).</summary>
    public double DistanceMetersTo(double latitude, double longitude)
    {
        const double earthRadiusMeters = 6_371_000;
        var dLat = DegreesToRadians(latitude - Latitude);
        var dLon = DegreesToRadians(longitude - Longitude);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(Latitude)) * Math.Cos(DegreesToRadians(latitude)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return earthRadiusMeters * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;
}

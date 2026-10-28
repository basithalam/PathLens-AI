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

    /// <summary>Set only for camera-detected candidates: the source that produced them. Never human-verified automatically.</summary>
    public Guid? CameraSourceId { get; private set; }

    // Optional observed accessibility attributes. Null means unknown: nobody
    // has measured it, and route scoring must never estimate it.
    public bool? HasStairs { get; private set; }
    public bool? HasRamp { get; private set; }
    public SurfaceCondition? Surface { get; private set; }

    /// <summary>Who supplied the attributes above: Reporter, CameraFeed, or Reviewer.</summary>
    public string? AttributeSource { get; private set; }
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
        DateTimeOffset now,
        Guid? cameraSourceId = null,
        bool? hasStairs = null,
        bool? hasRamp = null,
        SurfaceCondition? surface = null,
        string? attributeSource = null)
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
            CameraSourceId = cameraSourceId,
            HasStairs = hasStairs,
            HasRamp = hasRamp,
            Surface = surface,
            AttributeSource = string.IsNullOrWhiteSpace(attributeSource) ? null : attributeSource.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void SetVerificationStatus(VerificationStatus status, DateTimeOffset now)
    {
        VerificationStatus = status;
        UpdatedAt = now;
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

    public void SetAttributes(
        bool? hasStairs, bool? hasRamp, SurfaceCondition? surface, string? attributeSource, DateTimeOffset now)
    {
        HasStairs = hasStairs;
        HasRamp = hasRamp;
        Surface = surface;
        AttributeSource = string.IsNullOrWhiteSpace(attributeSource) ? null : attributeSource.Trim();
        UpdatedAt = now;
    }

    /// <summary>Great-circle distance in metres (Haversine).</summary>
    public double DistanceMetersTo(double latitude, double longitude) =>
        DistanceMetersBetween(Latitude, Longitude, latitude, longitude);

    public static double DistanceMetersBetween(
        double latitude1, double longitude1, double latitude2, double longitude2)
    {
        const double earthRadiusMeters = 6_371_000;
        var dLat = DegreesToRadians(latitude2 - latitude1);
        var dLon = DegreesToRadians(longitude2 - longitude1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(latitude1)) * Math.Cos(DegreesToRadians(latitude2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return earthRadiusMeters * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;
}

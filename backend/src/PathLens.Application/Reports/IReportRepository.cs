using PathLens.Domain.Reports;

namespace PathLens.Application.Reports;

public interface IReportRepository
{
    Task AddAsync(AccessibilityReport report, CancellationToken cancellationToken);
    Task<AccessibilityReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResult<AccessibilityReport>> ListAsync(ReportQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<AccessibilityReport>> NearbyAsync(
        double latitude, double longitude, double radiusMeters, int limit, CancellationToken cancellationToken);
    Task<IReadOnlyList<AccessibilityReport>> InViewportAsync(
        double minLatitude, double maxLatitude, double minLongitude, double maxLongitude,
        ObstacleType? obstacleType, VerificationStatus? verificationStatus,
        int limit, CancellationToken cancellationToken);
    /// <summary>Nearest non-duplicate report within range whose type matches (or either side is Unknown).</summary>
    Task<AccessibilityReport?> FindDuplicateCandidateAsync(
        Guid excludeReportId, double latitude, double longitude,
        ObstacleType obstacleType, double radiusMeters, CancellationToken cancellationToken);
    Task<MapSummaryResponse> SummaryAsync(CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

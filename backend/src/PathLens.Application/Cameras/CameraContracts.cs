namespace PathLens.Application.Cameras;

public sealed record CreateCameraRequest(
    string? Provider,
    string? ExternalId,
    string? Label,
    string? City,
    double Latitude,
    double Longitude,
    string? FeedImageUrl,
    string? DirectionLabel,
    int? CadenceSeconds,
    string? PermissionNote,
    bool Enabled);

public sealed record UpdateCameraRequest(
    int? CadenceSeconds,
    string? PermissionNote,
    bool? Enabled);

public sealed record CameraResponse(
    Guid Id,
    string Provider,
    string ExternalId,
    string Label,
    string City,
    double Latitude,
    double Longitude,
    string FeedImageUrl,
    string? DirectionLabel,
    int CadenceSeconds,
    bool Enabled,
    bool HasPermissionNote,
    DateTimeOffset? LastAttemptAt,
    DateTimeOffset? LastSuccessAt,
    DateTimeOffset? LastChangeAt,
    string? LastError,
    int ConsecutiveFailures,
    bool IsStale,
    DateTimeOffset CreatedAt);

public interface ICameraRepository
{
    Task AddAsync(Domain.Cameras.CameraSource source, CancellationToken cancellationToken);
    Task<Domain.Cameras.CameraSource?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Domain.Cameras.CameraSource>> ListAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Domain.Cameras.CameraSource>> ListDueAsync(DateTimeOffset now, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

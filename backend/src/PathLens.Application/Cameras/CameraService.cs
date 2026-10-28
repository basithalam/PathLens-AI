using PathLens.Domain.Cameras;

namespace PathLens.Application.Cameras;

public sealed class CameraService(ICameraRepository repository)
{
    public static readonly TimeSpan DefaultStaleAfter = TimeSpan.FromMinutes(10);

    public async Task<CameraResponse> CreateAsync(CreateCameraRequest request, CancellationToken cancellationToken)
    {
        var provider = Enum.TryParse<CameraProvider>(request.Provider, ignoreCase: true, out var parsed)
            ? parsed
            : CameraProvider.ManualAuthorised;

        var source = CameraSource.Create(
            provider,
            request.ExternalId ?? string.Empty,
            request.Label ?? string.Empty,
            request.City ?? string.Empty,
            request.Latitude,
            request.Longitude,
            request.FeedImageUrl ?? string.Empty,
            request.DirectionLabel,
            request.CadenceSeconds ?? 60,
            request.PermissionNote,
            DateTimeOffset.UtcNow);

        if (request.Enabled)
        {
            // Throws when no permission note — the API turns this into a 400.
            source.SetEnabled(true, DateTimeOffset.UtcNow);
        }

        await repository.AddAsync(source, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(source);
    }

    public async Task<IReadOnlyList<CameraResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var sources = await repository.ListAsync(cancellationToken);
        return sources.Select(ToResponse).ToList();
    }

    public async Task<CameraResponse?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var source = await repository.GetAsync(id, cancellationToken);
        return source is null ? null : ToResponse(source);
    }

    public async Task<CameraResponse?> UpdateAsync(
        Guid id, UpdateCameraRequest request, CancellationToken cancellationToken)
    {
        var source = await repository.GetAsync(id, cancellationToken);
        if (source is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        if (request.CadenceSeconds is not null)
        {
            source.UpdateCadence(request.CadenceSeconds.Value, now);
        }

        if (request.PermissionNote is not null)
        {
            source.UpdatePermissionNote(request.PermissionNote, now);
        }

        if (request.Enabled is not null)
        {
            source.SetEnabled(request.Enabled.Value, now);
        }

        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(source);
    }

    public static CameraResponse ToResponse(CameraSource source)
    {
        var now = DateTimeOffset.UtcNow;
        return new CameraResponse(
            source.Id,
            source.Provider.ToString(),
            source.ExternalId,
            source.Label,
            source.City,
            source.Latitude,
            source.Longitude,
            source.FeedImageUrl,
            source.DirectionLabel,
            source.CadenceSeconds,
            source.Enabled,
            source.HasPermissionNote,
            source.LastAttemptAt,
            source.LastSuccessAt,
            source.LastChangeAt,
            source.LastError,
            source.ConsecutiveFailures,
            source.IsStale(now, DefaultStaleAfter),
            source.CreatedAt);
    }

    public static Dictionary<string, string[]> ValidateCreate(CreateCameraRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.ExternalId))
        {
            errors["externalId"] = ["External ID is required."];
        }

        if (string.IsNullOrWhiteSpace(request.Label))
        {
            errors["label"] = ["Label is required."];
        }

        if (string.IsNullOrWhiteSpace(request.FeedImageUrl) ||
            !Uri.TryCreate(request.FeedImageUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            errors["feedImageUrl"] = ["Feed image URL must be an absolute http(s) URL."];
        }

        if (!string.IsNullOrWhiteSpace(request.Provider) &&
            !Enum.TryParse<CameraProvider>(request.Provider, ignoreCase: true, out _))
        {
            errors["provider"] = [$"Provider must be one of: {string.Join(", ", Enum.GetNames<CameraProvider>())}."];
        }

        if (request.Latitude is < -90 or > 90)
        {
            errors["latitude"] = ["Latitude must be between -90 and 90."];
        }

        if (request.Longitude is < -180 or > 180)
        {
            errors["longitude"] = ["Longitude must be between -180 and 180."];
        }

        if (request.CadenceSeconds is < CameraSource.MinimumCadenceSeconds)
        {
            errors["cadenceSeconds"] = [$"Cadence must be at least {CameraSource.MinimumCadenceSeconds} seconds."];
        }

        if (request.Enabled && string.IsNullOrWhiteSpace(request.PermissionNote))
        {
            errors["permissionNote"] = ["A camera source cannot be enabled until its permission/terms note is recorded."];
        }

        return errors;
    }
}

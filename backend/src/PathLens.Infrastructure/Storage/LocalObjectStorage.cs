using Microsoft.Extensions.Configuration;
using PathLens.Application.Storage;

namespace PathLens.Infrastructure.Storage;

/// <summary>
/// Private local-disk storage for development. Files live outside the public
/// web root and are served only through the API. Swap with S3-compatible
/// storage for deployment.
/// </summary>
public sealed class LocalObjectStorage : IObjectStorage
{
    private readonly string _rootPath;

    public LocalObjectStorage(IConfiguration configuration)
    {
        var configured = configuration["Storage:RootPath"];
        _rootPath = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Directory.GetCurrentDirectory(), ".local", "storage")
            : Path.GetFullPath(configured, Directory.GetCurrentDirectory());
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(Stream content, string contentType, CancellationToken cancellationToken)
    {
        var extension = contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ".bin",
        };

        var storageKey = $"{Guid.NewGuid():N}{extension}";
        var path = GetPath(storageKey);
        await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(file, cancellationToken);
        return storageKey;
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        var path = GetPath(storageKey);
        Stream? stream = File.Exists(path)
            ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)
            : null;
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        var path = GetPath(storageKey);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private string GetPath(string storageKey)
    {
        // Storage keys are server-generated; still refuse anything path-like.
        var safeKey = Path.GetFileName(storageKey);
        return Path.Combine(_rootPath, safeKey);
    }
}

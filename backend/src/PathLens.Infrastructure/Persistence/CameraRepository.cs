using Microsoft.EntityFrameworkCore;
using PathLens.Application.Cameras;
using PathLens.Domain.Cameras;

namespace PathLens.Infrastructure.Persistence;

public sealed class CameraRepository(PathLensDbContext dbContext) : ICameraRepository
{
    public async Task AddAsync(CameraSource source, CancellationToken cancellationToken) =>
        await dbContext.CameraSources.AddAsync(source, cancellationToken);

    public Task<CameraSource?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.CameraSources.FirstOrDefaultAsync(source => source.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CameraSource>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.CameraSources.AsNoTracking()
            .OrderBy(source => source.City)
            .ThenBy(source => source.Label)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CameraSource>> ListDueAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var enabled = await dbContext.CameraSources
            .Where(source => source.Enabled)
            .ToListAsync(cancellationToken);
        return enabled.Where(source => source.IsDue(now)).ToList();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}

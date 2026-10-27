using Microsoft.EntityFrameworkCore;
using PathLens.Application.Routing;
using PathLens.Domain.Routing;

namespace PathLens.Infrastructure.Persistence;

public sealed class RouteRecommendationRepository(PathLensDbContext dbContext) : IRouteRecommendationRepository
{
    public async Task AddAsync(RouteRecommendationRecord record, CancellationToken cancellationToken) =>
        await dbContext.RouteRecommendations.AddAsync(record, cancellationToken);

    public Task<RouteRecommendationRecord?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.RouteRecommendations.FirstOrDefaultAsync(record => record.Id == id, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}

using Microsoft.EntityFrameworkCore;
using PathLens.Application.Users;
using PathLens.Domain.Users;

namespace PathLens.Infrastructure.Persistence;

public sealed class UserRepository(PathLensDbContext dbContext) : IUserRepository
{
    public Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        dbContext.Users.FirstOrDefaultAsync(
            user => user.Email == email.Trim().ToLowerInvariant(), cancellationToken);

    public Task<AppUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);

    public async Task AddAsync(AppUser user, CancellationToken cancellationToken) =>
        await dbContext.Users.AddAsync(user, cancellationToken);

    public Task<bool> AnyUsersAsync(CancellationToken cancellationToken) =>
        dbContext.Users.AnyAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}

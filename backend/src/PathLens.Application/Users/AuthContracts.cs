namespace PathLens.Application.Users;

public sealed record LoginRequest(string? Email, string? Password);

public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    Guid UserId,
    string Email,
    string DisplayName,
    string Role);

public sealed record UserResponse(Guid Id, string Email, string DisplayName, string Role);

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(string email, string password, CancellationToken cancellationToken);
}

public interface IUserRepository
{
    Task<Domain.Users.AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task<Domain.Users.AppUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Domain.Users.AppUser user, CancellationToken cancellationToken);
    Task<bool> AnyUsersAsync(CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

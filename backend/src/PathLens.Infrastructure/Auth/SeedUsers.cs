using Microsoft.AspNetCore.Identity;
using PathLens.Application.Users;
using PathLens.Domain.Users;

namespace PathLens.Infrastructure.Auth;

/// <summary>
/// Development seed accounts. Passwords are documented local-only values and
/// must be changed outside local development; hashes are what get stored.
/// </summary>
public static class SeedUsers
{
    public const string ReviewerEmail = "reviewer@pathlens.local";
    public const string ReviewerPassword = "Reviewer@123";
    public const string AdminEmail = "admin@pathlens.local";
    public const string AdminPassword = "Admin@123";
    public const string DemoUserEmail = "user@pathlens.local";
    public const string DemoUserPassword = "User@123";

    public static async Task SeedAsync(IUserRepository repository, CancellationToken cancellationToken)
    {
        if (await repository.AnyUsersAsync(cancellationToken))
        {
            return;
        }

        var hasher = new PasswordHasher<AppUser>();
        var now = DateTimeOffset.UtcNow;

        foreach (var (email, name, password, role) in new[]
        {
            (ReviewerEmail, "Demo Reviewer", ReviewerPassword, UserRole.Reviewer),
            (AdminEmail, "Demo Admin", AdminPassword, UserRole.Admin),
            (DemoUserEmail, "Demo User", DemoUserPassword, UserRole.User),
        })
        {
            var user = AppUser.Create(email, name, string.Empty, role, now);
            var hash = hasher.HashPassword(user, password);
            var seeded = AppUser.Create(email, name, hash, role, now);
            await repository.AddAsync(seeded, cancellationToken);
        }

        await repository.SaveChangesAsync(cancellationToken);
    }
}

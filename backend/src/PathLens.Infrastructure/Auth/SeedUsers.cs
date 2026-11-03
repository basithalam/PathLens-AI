using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using PathLens.Application.Users;
using PathLens.Domain.Users;

namespace PathLens.Infrastructure.Auth;

/// <summary>
/// Seeded accounts. The defaults below are local-development values and are documented as such;
/// a deployment overrides them with the Auth:Seed* configuration keys so the real credentials never
/// appear in this repository.
/// </summary>
public static class SeedUsers
{
    public const string ReviewerEmail = "reviewer@pathlens.local";
    public const string ReviewerPassword = "Reviewer@123";
    public const string AdminEmail = "admin@pathlens.local";
    public const string AdminPassword = "Admin@123";
    public const string DemoUserEmail = "user@pathlens.local";
    public const string DemoUserPassword = "User@123";

    private static readonly (string Email, string DisplayName, string Default, string ConfigKey, UserRole Role)[] Accounts =
    [
        (ReviewerEmail, "Demo Reviewer", ReviewerPassword, "Auth:SeedReviewerPassword", UserRole.Reviewer),
        (AdminEmail, "Demo Admin", AdminPassword, "Auth:SeedAdminPassword", UserRole.Admin),
        (DemoUserEmail, "Demo User", DemoUserPassword, "Auth:SeedUserPassword", UserRole.User),
    ];

    public static Task SeedAsync(IUserRepository repository, CancellationToken cancellationToken) =>
        SeedAsync(repository, configuration: null, cancellationToken);

    /// <summary>
    /// Creates the seeded accounts on an empty database, and on an existing one rotates the password
    /// of any account whose credential is supplied through configuration.
    ///
    /// The rotation branch is what makes this a real control: the accounts are created once, so
    /// changing the constants above would leave an already-deployed database accepting the old,
    /// publicly documented password forever.
    /// </summary>
    /// <returns>The emails whose password was rotated, for the caller to log.</returns>
    public static async Task<IReadOnlyList<string>> SeedAsync(
        IUserRepository repository, IConfiguration? configuration, CancellationToken cancellationToken)
    {
        var hasher = new PasswordHasher<AppUser>();
        var now = DateTimeOffset.UtcNow;
        var rotated = new List<string>();

        if (!await repository.AnyUsersAsync(cancellationToken))
        {
            foreach (var account in Accounts)
            {
                var password = Resolve(account, configuration);
                var placeholder = AppUser.Create(account.Email, account.DisplayName, string.Empty, account.Role, now);
                var user = AppUser.Create(
                    account.Email, account.DisplayName, hasher.HashPassword(placeholder, password), account.Role, now);
                await repository.AddAsync(user, cancellationToken);
            }

            await repository.SaveChangesAsync(cancellationToken);
            return rotated;
        }

        if (configuration is null)
        {
            return rotated;
        }

        foreach (var account in Accounts)
        {
            var configured = configuration[account.ConfigKey];
            if (string.IsNullOrWhiteSpace(configured))
            {
                // Nothing configured for this account: leave the stored credential alone.
                continue;
            }

            var user = await repository.GetByEmailAsync(account.Email, cancellationToken);
            if (user is null)
            {
                continue;
            }

            if (hasher.VerifyHashedPassword(user, user.PasswordHash, configured) != PasswordVerificationResult.Failed)
            {
                continue;
            }

            user.SetPasswordHash(hasher.HashPassword(user, configured));
            rotated.Add(account.Email);
        }

        if (rotated.Count > 0)
        {
            await repository.SaveChangesAsync(cancellationToken);
        }

        return rotated;
    }

    private static string Resolve(
        (string Email, string DisplayName, string Default, string ConfigKey, UserRole Role) account,
        IConfiguration? configuration)
    {
        var configured = configuration?[account.ConfigKey];
        return string.IsNullOrWhiteSpace(configured) ? account.Default : configured;
    }
}

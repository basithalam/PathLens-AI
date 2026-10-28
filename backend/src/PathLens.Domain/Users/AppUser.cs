namespace PathLens.Domain.Users;

public enum UserRole
{
    User,
    Reviewer,
    Admin,
}

/// <summary>
/// Application user. Accounts are seeded for the hackathon build (no public
/// registration); passwords are stored only as hashes.
/// </summary>
public sealed class AppUser
{
    private AppUser()
    {
    }

    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static AppUser Create(
        string email, string displayName, string passwordHash, UserRole role, DateTimeOffset now) => new()
        {
            Id = Guid.NewGuid(),
            Email = email.Trim().ToLowerInvariant(),
            DisplayName = displayName.Trim(),
            PasswordHash = passwordHash,
            Role = role,
            IsActive = true,
            CreatedAt = now,
        };
}

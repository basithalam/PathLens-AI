namespace PathLens.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Server-side signing key (environment variable Jwt__SigningKey). Never committed with a real value.</summary>
    public string SigningKey { get; set; } = "LOCAL_DEV_ONLY_CHANGE_ME_PATHLENS_SIGNING_KEY_32CHARS";

    public string Issuer { get; set; } = "PathLens";

    public string Audience { get; set; } = "PathLensWeb";

    public int ExpiryMinutes { get; set; } = 120;
}

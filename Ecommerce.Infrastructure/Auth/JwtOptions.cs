namespace Ecommerce.Infrastructure.Auth;
public sealed class JwtOptions
{
    // HS256 needs a key at least as long as the 256-bit hash, so shorter keys are rejected at startup.
    public const int MinSecretKeyBytes = 32;

    public required string Issuer { get; set; }
    public required string Audience { get; init; }
    public required string SecretKey { get; set; }
    public int AccessTokenLifetimeMinutes { get; init; } = 15;
    public int RefreshTokenLifetimeDays { get; init; } = 7;
}

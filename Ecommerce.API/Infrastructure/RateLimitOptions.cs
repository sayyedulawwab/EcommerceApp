using System.ComponentModel.DataAnnotations;

namespace Ecommerce.API.Infrastructure;

// Sliding-window limit for one group of /auth endpoints, counted per client IP.
public abstract class RateLimitOptions
{
    [Range(1, 1_000_000)]
    public int PermitLimit { get; init; }

    [Range(1, 3_600)]
    public int WindowSeconds { get; init; } = 60;
}

// login and register: where password guessing and account spam happen, so the limit is tight.
public sealed class CredentialsRateLimitOptions : RateLimitOptions
{
    public const string PolicyName = "credentials";
    public const string SectionName = "RateLimiting:Credentials";

    public CredentialsRateLimitOptions()
    {
        PermitLimit = 5;
    }
}

// refresh and logout: every signed-in client calls these in the background, so the limit is looser.
public sealed class TokensRateLimitOptions : RateLimitOptions
{
    public const string PolicyName = "tokens";
    public const string SectionName = "RateLimiting:Tokens";

    public TokensRateLimitOptions()
    {
        PermitLimit = 30;
    }
}

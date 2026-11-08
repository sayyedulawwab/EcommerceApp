using System.ComponentModel.DataAnnotations;

namespace Ecommerce.API.Infrastructure;

// Limits for the /auth endpoints, counted per client IP across all of them.
public sealed class AuthRateLimitOptions
{
    public const string PolicyName = "auth";

    [Range(1, 1_000_000)]
    public int PermitLimit { get; init; } = 10;

    [Range(1, 3_600)]
    public int WindowSeconds { get; init; } = 60;
}

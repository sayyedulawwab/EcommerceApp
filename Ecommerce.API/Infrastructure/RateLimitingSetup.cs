using System.Globalization;
using System.Net;
using System.Threading.RateLimiting;
using Ecommerce.API.Extensions;
using Ecommerce.Domain.Abstractions;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Ecommerce.API.Infrastructure;

internal static class RateLimitingSetup
{
    private static readonly Error TooManyRequests = new(
        "RateLimit.Exceeded",
        "Too many requests. Try again later",
        HttpStatusCode.TooManyRequests);

    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services)
    {
        services.AddOptions<CredentialsRateLimitOptions>()
            .BindConfiguration(CredentialsRateLimitOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<TokensRateLimitOptions>()
            .BindConfiguration(TokensRateLimitOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                // The sliding window limiter gives no retry time, so fall back to the whole window: by then the count has surely dropped.
                if (!context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
                {
                    retryAfter = TimeSpan.FromSeconds(GetWindowSeconds(context.HttpContext));
                }

                context.HttpContext.Response.Headers.RetryAfter =
                    ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);

                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                await context.HttpContext.Response.WriteAsJsonAsync(
                    TooManyRequests.ToProblemDetails(),
                    options: null,
                    contentType: "application/problem+json",
                    cancellationToken);
            };

            // Two buckets per client IP, not one. Refresh runs for every signed-in client every few minutes, so
            // refresh traffic from many users sharing an IP (an office) must not use up the login limit and lock them out.
            options.AddPolicy(CredentialsRateLimitOptions.PolicyName, httpContext =>
                GetPartition<CredentialsRateLimitOptions>(httpContext));

            options.AddPolicy(TokensRateLimitOptions.PolicyName, httpContext =>
                GetPartition<TokensRateLimitOptions>(httpContext));
        });

        return services;
    }

    private static int GetWindowSeconds(HttpContext httpContext)
    {
        string? policyName = httpContext.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName;

        return policyName == TokensRateLimitOptions.PolicyName
            ? httpContext.RequestServices.GetRequiredService<IOptions<TokensRateLimitOptions>>().Value.WindowSeconds
            : httpContext.RequestServices.GetRequiredService<IOptions<CredentialsRateLimitOptions>>().Value.WindowSeconds;
    }

    private static RateLimitPartition<string> GetPartition<TOptions>(HttpContext httpContext)
        where TOptions : RateLimitOptions
    {
        RateLimitOptions limits = httpContext.RequestServices.GetRequiredService<IOptions<TOptions>>().Value;

        // RemoteIpAddress is the real client only after the forwarded-headers middleware accepted a trusted proxy.
        string clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // A sliding window counts the last WindowSeconds at any moment, so a client can't send a double burst across a window edge.
        return RateLimitPartition.GetSlidingWindowLimiter(clientIp, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = limits.PermitLimit,
            Window = TimeSpan.FromSeconds(limits.WindowSeconds),
            SegmentsPerWindow = 6,
            QueueLimit = 0
        });
    }
}

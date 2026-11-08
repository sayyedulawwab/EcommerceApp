using System.Globalization;
using System.Net;
using System.Threading.RateLimiting;
using Ecommerce.API.Extensions;
using Ecommerce.Domain.Abstractions;
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
        services.AddOptions<AuthRateLimitOptions>()
            .BindConfiguration("RateLimiting:Auth")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                }

                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                await context.HttpContext.Response.WriteAsJsonAsync(
                    TooManyRequests.ToProblemDetails(),
                    options: null,
                    contentType: "application/problem+json",
                    cancellationToken);
            };

            // One bucket per client IP, shared by login, register, refresh and logout.
            options.AddPolicy(AuthRateLimitOptions.PolicyName, httpContext =>
            {
                AuthRateLimitOptions limits = httpContext.RequestServices.GetRequiredService<IOptions<AuthRateLimitOptions>>().Value;

                // RemoteIpAddress is the real client only after the forwarded-headers middleware accepted a trusted proxy.
                string clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limits.PermitLimit,
                    Window = TimeSpan.FromSeconds(limits.WindowSeconds),
                    QueueLimit = 0
                });
            });
        });

        return services;
    }
}

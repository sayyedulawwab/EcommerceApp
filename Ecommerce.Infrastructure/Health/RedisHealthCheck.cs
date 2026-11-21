using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ecommerce.Infrastructure.Health;

internal sealed class RedisHealthCheck(IDistributedCache cache) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // A read of a key that doesn't exist still needs a working connection, and throws without one.
        // WaitAsync stops waiting when the registration's timeout cancels the token, even if Redis never answers.
        await cache.GetAsync("health-check", cancellationToken).WaitAsync(cancellationToken);

        return HealthCheckResult.Healthy();
    }
}

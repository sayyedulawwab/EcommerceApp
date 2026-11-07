using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ecommerce.Infrastructure.Health;

internal sealed class RedisHealthCheck(IDistributedCache cache) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // A read of a key that doesn't exist still needs a working connection, and throws without one.
        await cache.GetAsync("health-check", cancellationToken);

        return HealthCheckResult.Healthy();
    }
}

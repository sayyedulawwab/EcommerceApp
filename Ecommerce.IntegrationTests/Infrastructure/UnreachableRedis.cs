using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace Ecommerce.IntegrationTests.Infrastructure;

internal static class UnreachableRedis
{
    // The real Redis client, pointed at a port where nothing listens: Redis is down. Only the endpoint
    // changes, so the app's own client settings (fail fast) are what the tests exercise. RedisCache
    // prefers ConfigurationOptions over the Configuration string, so changing the string alone would do nothing.
    public static void UseRedisThatIsDown(this IServiceCollection services)
    {
        services.PostConfigure<RedisCacheOptions>(options =>
        {
            ConfigurationOptions redis = options.ConfigurationOptions ?? ConfigurationOptions.Parse(options.Configuration!);
            redis.EndPoints.Clear();
            redis.EndPoints.Add("127.0.0.1", 1);
            options.ConfigurationOptions = redis;
        });
    }

    // A Redis that accepts the request and never answers, which a closed port can't simulate.
    public static void UseRedisThatNeverAnswers(this IServiceCollection services)
    {
        services.RemoveAll<IDistributedCache>();
        services.AddSingleton<IDistributedCache, SilentDistributedCache>();
    }

    private sealed class SilentDistributedCache : IDistributedCache
    {
        public byte[]? Get(string key) => throw new NotSupportedException("The app only uses the async methods.");

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => new TaskCompletionSource<byte[]?>().Task;

        public void Refresh(string key) => throw new NotSupportedException("The app only uses the async methods.");

        public Task RefreshAsync(string key, CancellationToken token = default) => new TaskCompletionSource().Task;

        public void Remove(string key) => throw new NotSupportedException("The app only uses the async methods.");

        public Task RemoveAsync(string key, CancellationToken token = default) => new TaskCompletionSource().Task;

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => throw new NotSupportedException("The app only uses the async methods.");

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) => new TaskCompletionSource().Task;
    }
}

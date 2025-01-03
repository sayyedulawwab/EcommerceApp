﻿using System.Collections.Concurrent;
using Ecommerce.Application.Abstractions.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace Ecommerce.Infrastructure.Caching;

public class CacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private static readonly ConcurrentDictionary<string, bool> CachedKeys = new();

    public CacheService(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        string? cachedValue = await _distributedCache.GetStringAsync(key, cancellationToken);

        if (cachedValue is null)
        {
            return default;
        }

        T? value = JsonConvert.DeserializeObject<T>(cachedValue, new JsonSerializerSettings
        {
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            ContractResolver = new PrivateResolver()
        });

        return value;
    }

    public async Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        string cachedValue = JsonConvert.SerializeObject(value);

        await _distributedCache.SetStringAsync(key, cachedValue, cancellationToken);

        CachedKeys.TryAdd(key, false);

    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _distributedCache.RemoveAsync(key, cancellationToken);

        CachedKeys.TryRemove(key, out bool _);
    }

    public Task RemoveByPrefixAsync(string prefixKey, CancellationToken cancellationToken = default)
    {
        IEnumerable<Task> tasks = CachedKeys
            .Keys
            .Where(k => k.StartsWith(prefixKey, StringComparison.Ordinal))
            .Select(k => RemoveAsync(k, cancellationToken));

        return Task.WhenAll(tasks);
    }
}

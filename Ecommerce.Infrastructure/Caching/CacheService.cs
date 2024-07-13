﻿using Ecommerce.Application.Abstractions.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace Ecommerce.Infrastructure.Caching;

public class CacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private static readonly ConcurrentDictionary<string, bool> CachedKeys = new();

    public CacheService(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        string? cachedValue = await _distributedCache.GetStringAsync(key, cancellationToken);

        if (cachedValue is null)
        {
            return null;
        }

        T? value = JsonConvert.DeserializeObject<T>(cachedValue, new JsonSerializerSettings
        {
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            ContractResolver = new PrivateResolver()
        }); 

        return value;
    }

    public async Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default) where T : class
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

    public async Task RemoveByPrefixAsync(string prefixKey, CancellationToken cancellationToken = default)
    {
        //foreach (var key in CachedKeys.Keys)
        //{
        //    if (key.StartsWith(prefixKey))
        //    {
        //        await _distributedCache.RemoveAsync(key, cancellationToken);
        //    }
        //}

        IEnumerable<Task> tasks = CachedKeys
            .Keys
            .Where(k => k.StartsWith(prefixKey))
            .Select(k => RemoveAsync(k, cancellationToken));

        Task.WhenAll(tasks);
    }
}

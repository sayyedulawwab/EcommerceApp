using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Application.Products;

// Every cached product search carries this tag, so one call clears them all.
internal static class ProductCache
{
    public const string Tag = "products";

    // Call after SaveChangesAsync. The change is already saved, so a cache that cannot be cleared is logged
    // as a warning and the request still succeeds. Stale entries then expire on their own.
    public static async Task ClearAsync(HybridCache cache, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            await cache.RemoveByTagAsync(Tag, cancellationToken);
        }
#pragma warning disable CA1031 // Any cache failure, whatever its type, must not fail a request that already saved.
        catch (Exception exception) when (exception is not OperationCanceledException)
#pragma warning restore CA1031
        {
            logger.LogWarning(exception, "Saved the change but could not clear the {CacheTag} cache; cached results stay until they expire", Tag);
        }
    }
}

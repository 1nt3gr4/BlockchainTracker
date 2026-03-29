using BlockchainTracker.Application.Interfaces;
using BlockchainTracker.Infrastructure.Telemetry;
using Microsoft.Extensions.Caching.Memory;

namespace BlockchainTracker.Infrastructure.Caching;

public sealed class MemoryCacheService(IMemoryCache cache, BlockchainTrackerMetrics metrics) : ICacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken ct)
    {
        if (cache.TryGetValue(key, out T? value))
        {
            metrics.RecordCacheHit(key);
            return Task.FromResult(value);
        }

        metrics.RecordCacheMiss(key);
        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct)
    {
        cache.Set(key, value, ttl);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct)
    {
        cache.Remove(key);
        return Task.CompletedTask;
    }
}

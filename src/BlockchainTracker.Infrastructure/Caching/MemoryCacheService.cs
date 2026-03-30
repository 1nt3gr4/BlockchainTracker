using BlockchainTracker.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace BlockchainTracker.Infrastructure.Caching;

public sealed class MemoryCacheService(IMemoryCache cache) : ICacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken ct)
    {
        cache.TryGetValue(key, out T? value);
        return Task.FromResult(value);
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

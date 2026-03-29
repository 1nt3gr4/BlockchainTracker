using System.Text.Json;
using BlockchainTracker.Application.Interfaces;
using BlockchainTracker.Infrastructure.Telemetry;
using StackExchange.Redis;

namespace BlockchainTracker.Infrastructure.Caching;

public sealed class RedisCacheService(IConnectionMultiplexer redis, BlockchainTrackerMetrics metrics) : ICacheService
{
    private readonly IDatabase _db = redis.GetDatabase();

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct)
    {
        var value = await _db.StringGetAsync(key);

        if (value.HasValue)
        {
            metrics.RecordCacheHit(key);
            return JsonSerializer.Deserialize<T>((string)value!);
        }

        metrics.RecordCacheMiss(key);
        return default;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(value);
        await _db.StringSetAsync(key, json, ttl);
    }

    public async Task RemoveAsync(string key, CancellationToken ct)
    {
        await _db.KeyDeleteAsync(key);
    }
}

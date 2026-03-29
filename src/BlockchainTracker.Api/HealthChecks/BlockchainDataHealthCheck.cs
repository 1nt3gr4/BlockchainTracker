using BlockchainTracker.Domain.Configuration;
using BlockchainTracker.Domain.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace BlockchainTracker.Api.HealthChecks;

/// <summary>
/// Monitors that blockchain polling is functioning correctly by checking data freshness.
/// Results are cached for 1 minute to minimize database load.
/// </summary>
public class BlockchainDataHealthCheck(
    IServiceScopeFactory scopeFactory,
    IOptions<HealthCheckSettings> settings) : IHealthCheck
{
    private HealthCheckResult? _cachedResult;
    private DateTimeOffset _lastCheck = DateTimeOffset.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(1);

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        if (_cachedResult.HasValue && DateTimeOffset.UtcNow - _lastCheck < CacheDuration)
            return _cachedResult.Value;

        var result = await EvaluateHealthAsync(ct);
        _cachedResult = result;
        _lastCheck = DateTimeOffset.UtcNow;
        return result;
    }

    private async Task<HealthCheckResult> EvaluateHealthAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IBlockchainSnapshotRepository>();
        var snapshots = await repository.GetLatestPerChainAsync(ct);

        if (snapshots.Count == 0)
            return HealthCheckResult.Healthy("No blockchain data yet \u2014 polling may not have completed its first cycle");

        var staleThreshold = DateTimeOffset.UtcNow - settings.Value.MaxStaleAge;
        var staleChains = snapshots
            .Where(s => s.FetchedAt < staleThreshold)
            .Select(s => s.ChainName)
            .ToList();

        if (staleChains.Count > 0)
            return HealthCheckResult.Degraded($"Stale data for chains: {string.Join(", ", staleChains)}");

        return HealthCheckResult.Healthy($"All {snapshots.Count} chains have fresh data");
    }
}

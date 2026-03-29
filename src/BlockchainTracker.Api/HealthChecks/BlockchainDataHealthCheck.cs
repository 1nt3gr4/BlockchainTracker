using BlockchainTracker.Domain.Configuration;
using BlockchainTracker.Domain.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace BlockchainTracker.Api.HealthChecks;

public class BlockchainDataHealthCheck : IHealthCheck
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<HealthCheckSettings> _settings;

    public BlockchainDataHealthCheck(IServiceScopeFactory scopeFactory, IOptions<HealthCheckSettings> settings)
    {
        _scopeFactory = scopeFactory;
        _settings = settings;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IBlockchainSnapshotRepository>();
        var snapshots = await repository.GetLatestPerChainAsync(ct);

        if (snapshots.Count == 0)
            return HealthCheckResult.Degraded("No blockchain data available");

        var staleThreshold = DateTime.UtcNow - _settings.Value.MaxStaleAge;
        var staleChains = snapshots
            .Where(s => s.FetchedAt < staleThreshold)
            .Select(s => s.ChainName)
            .ToList();

        if (staleChains.Count > 0)
            return HealthCheckResult.Degraded($"Stale data for chains: {string.Join(", ", staleChains)}");

        return HealthCheckResult.Healthy($"All {snapshots.Count} chains have fresh data");
    }
}

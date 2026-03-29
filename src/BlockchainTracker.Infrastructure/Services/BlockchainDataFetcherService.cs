using System.Diagnostics;
using System.Text.Json;
using BlockchainTracker.Application.Interfaces;
using BlockchainTracker.Domain.Entities;
using BlockchainTracker.Domain.Interfaces;
using BlockchainTracker.Infrastructure.Persistence;
using BlockchainTracker.Infrastructure.Telemetry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlockchainTracker.Infrastructure.Services;

public class BlockchainDataFetcherService(
    IBlockchainApiClient apiClient,
    IDbContextFactory<BlockchainDbContext> contextFactory,
    ICacheService cache,
    BlockchainTrackerMetrics metrics,
    ILogger<BlockchainDataFetcherService> logger) : IBlockchainDataFetcherService
{
    private static readonly JsonSerializerOptions SnakeCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public async Task<bool> FetchAndSaveAsync(string chainName, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await apiClient.GetChainDataAsync(chainName, ct);
            metrics.RecordSnapshotFetched(chainName);

            await using var context = await contextFactory.CreateDbContextAsync(ct);

            var exists = await context.Snapshots
                .AnyAsync(s => s.ChainName == chainName && s.Height == response.Height && s.Hash == response.Hash, ct);

            if (exists)
            {
                metrics.RecordDuplicateSkipped(chainName);
                return false;
            }

            var snapshot = new BlockchainSnapshot
            {
                ChainName = chainName,
                Height = response.Height,
                Hash = response.Hash,
                Time = response.Time,
                RawJson = JsonSerializer.Serialize(response, SnakeCaseOptions),
                FetchedAt = DateTimeOffset.UtcNow
            };

            await context.Snapshots.AddAsync(snapshot, ct);
            await context.SaveChangesAsync(ct);
            metrics.RecordSnapshotSaved(chainName);

            await cache.RemoveAsync($"chain:latest:{chainName}", ct);
            await cache.RemoveAsync("chains:latest:all", ct);

            logger.LogDebug("Saved new snapshot for {ChainName} at height {Height}", chainName, response.Height);
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            metrics.RecordFetchError(chainName);
            throw;
        }
        finally
        {
            sw.Stop();
            metrics.RecordFetchDuration(chainName, sw.Elapsed.TotalMilliseconds);
        }
    }
}

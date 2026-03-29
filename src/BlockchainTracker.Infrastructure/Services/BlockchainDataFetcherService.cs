using System.Diagnostics;
using System.Text.Json;
using BlockchainTracker.Application.Interfaces;
using BlockchainTracker.Domain.Entities;
using BlockchainTracker.Domain.Interfaces;
using BlockchainTracker.Infrastructure.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlockchainTracker.Infrastructure.Services;

public class BlockchainDataFetcherService(
    IBlockchainApiClient apiClient,
    IServiceScopeFactory scopeFactory,
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

            await using var scope = scopeFactory.CreateAsyncScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var exists = await unitOfWork.Repository.ExistsAsync(chainName, response.Height, response.Hash, ct);
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

            await unitOfWork.Repository.AddAsync(snapshot, ct);
            await unitOfWork.SaveChangesAsync(ct);
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

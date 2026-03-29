using BlockchainTracker.Application.Commands;
using BlockchainTracker.Domain.Configuration;
using Mediator;
using Microsoft.Extensions.Options;
using RedLockNet;

namespace BlockchainTracker.Api.Workers;

public class BlockchainPollingWorker(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<PollingSettings> pollingSettings,
    IOptionsMonitor<DistributedLockSettings> lockSettings,
    IDistributedLockFactory lockFactory,
    ILogger<BlockchainPollingWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Blockchain polling worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            var currentLockSettings = lockSettings.CurrentValue;

            await using var redLock = await lockFactory.CreateLockAsync(
                currentLockSettings.PollingLockKey,
                currentLockSettings.LockTtl);

            if (redLock.IsAcquired)
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                    await mediator.Send(new FetchAllChainsCommand(), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error during blockchain polling cycle");
                }
            }
            else
            {
                logger.LogDebug("Skipping polling cycle — another instance holds the lock");
            }

            try
            {
                await Task.Delay(pollingSettings.CurrentValue.Interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        logger.LogInformation("Blockchain polling worker stopped");
    }
}

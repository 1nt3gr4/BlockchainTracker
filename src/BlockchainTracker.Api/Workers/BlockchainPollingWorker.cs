using BlockchainTracker.Application.Commands;
using BlockchainTracker.Domain.Configuration;
using BlockchainTracker.Domain.Helpers;
using Mediator;
using Microsoft.Extensions.Options;

namespace BlockchainTracker.Api.Workers;

public class BlockchainPollingWorker(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<PollingSettings> pollingSettings,
    ILogger<BlockchainPollingWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Blockchain polling worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await FetchAllChainsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during blockchain polling cycle");
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

    private async Task FetchAllChainsAsync(CancellationToken ct)
    {
        var chains = BlockchainChainHelper.GetSupportedChains();
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = pollingSettings.CurrentValue.MaxDegreeOfParallelism,
            CancellationToken = ct
        };

        await Parallel.ForEachAsync(chains, options, async (chainName, token) =>
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var saved = await mediator.Send(new FetchChainDataCommand(chainName), token);

                if (saved)
                {
                    logger.LogInformation("Saved new snapshot for chain {ChainName}", chainName);
                }
                else
                {
                    logger.LogDebug("No new data for chain {ChainName}", chainName);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to fetch data for chain {ChainName}", chainName);
            }
        });
    }
}

using BlockchainTracker.Application.Commands;
using BlockchainTracker.Domain.Configuration;
using Mediator;
using Microsoft.Extensions.Options;

namespace BlockchainTracker.Api.Workers;

public class BlockchainPollingWorker(
    IMediator mediator,
    IOptionsMonitor<PollingSettings> pollingSettings,
    ILogger<BlockchainPollingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Blockchain polling worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
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

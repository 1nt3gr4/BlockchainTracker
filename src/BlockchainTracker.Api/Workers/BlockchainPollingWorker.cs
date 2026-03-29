using BlockchainTracker.Application.Commands;
using BlockchainTracker.Domain.Configuration;
using Mediator;
using Microsoft.Extensions.Options;

namespace BlockchainTracker.Api.Workers;

public class BlockchainPollingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<PollingSettings> _pollingSettings;
    private readonly ILogger<BlockchainPollingWorker> _logger;

    public BlockchainPollingWorker(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<PollingSettings> pollingSettings,
        ILogger<BlockchainPollingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _pollingSettings = pollingSettings;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Blockchain polling worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(new FetchAllChainsCommand(), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during blockchain polling cycle");
            }

            try
            {
                await Task.Delay(_pollingSettings.CurrentValue.Interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("Blockchain polling worker stopped");
    }
}

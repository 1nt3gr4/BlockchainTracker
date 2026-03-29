using BlockchainTracker.Domain.Configuration;
using BlockchainTracker.Domain.Interfaces;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BlockchainTracker.Application.Commands;

public sealed class FetchAllChainsCommandHandler : ICommandHandler<FetchAllChainsCommand>
{
    private readonly IBlockchainApiClient _apiClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<PollingSettings> _pollingSettings;
    private readonly ILogger<FetchAllChainsCommandHandler> _logger;

    public FetchAllChainsCommandHandler(
        IBlockchainApiClient apiClient,
        IServiceScopeFactory scopeFactory,
        IOptions<PollingSettings> pollingSettings,
        ILogger<FetchAllChainsCommandHandler> logger)
    {
        _apiClient = apiClient;
        _scopeFactory = scopeFactory;
        _pollingSettings = pollingSettings;
        _logger = logger;
    }

    public async ValueTask<Unit> Handle(FetchAllChainsCommand command, CancellationToken ct)
    {
        var chains = _apiClient.GetSupportedChains();
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = _pollingSettings.Value.MaxDegreeOfParallelism,
            CancellationToken = ct
        };

        await Parallel.ForEachAsync(chains, options, async (chainName, token) =>
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(new FetchChainDataCommand(chainName), token);
                _logger.LogInformation("Fetched data for chain {ChainName}", chainName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch data for chain {ChainName}", chainName);
            }
        });

        return Unit.Value;
    }
}

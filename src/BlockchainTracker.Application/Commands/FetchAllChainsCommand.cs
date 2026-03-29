using BlockchainTracker.Application.Interfaces;
using BlockchainTracker.Domain.Configuration;
using BlockchainTracker.Domain.Interfaces;
using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BlockchainTracker.Application.Commands;

public record FetchAllChainsCommand : ICommand;

public sealed class FetchAllChainsCommandHandler(
    IBlockchainApiClient apiClient,
    IBlockchainDataFetcherService fetcherService,
    IOptions<PollingSettings> pollingSettings,
    ILogger<FetchAllChainsCommandHandler> logger) : ICommandHandler<FetchAllChainsCommand>
{
    public async ValueTask<Unit> Handle(FetchAllChainsCommand command, CancellationToken ct)
    {
        var chains = apiClient.GetSupportedChains();
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = pollingSettings.Value.MaxDegreeOfParallelism,
            CancellationToken = ct
        };

        await Parallel.ForEachAsync(chains, options, async (chainName, token) =>
        {
            try
            {
                var saved = await fetcherService.FetchAndSaveAsync(chainName, token);
                if (saved)
                    logger.LogInformation("Saved new snapshot for chain {ChainName}", chainName);
                else
                    logger.LogDebug("No new data for chain {ChainName}", chainName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to fetch data for chain {ChainName}", chainName);
            }
        });

        return Unit.Value;
    }
}

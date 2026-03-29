using BlockchainTracker.Application.Interfaces;
using Mediator;

namespace BlockchainTracker.Application.Commands;

public record FetchChainDataCommand(string ChainName) : ICommand<bool>;

public sealed class FetchChainDataCommandHandler(
    IBlockchainDataFetcherService fetcherService) : ICommandHandler<FetchChainDataCommand, bool>
{
    public async ValueTask<bool> Handle(FetchChainDataCommand command, CancellationToken ct)
    {
        return await fetcherService.FetchAndSaveAsync(command.ChainName, ct);
    }
}

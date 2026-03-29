using Mediator;

namespace BlockchainTracker.Application.Commands;

public record FetchChainDataCommand(string ChainName) : ICommand<bool>;

using BlockchainTracker.Application.Commands;
using BlockchainTracker.Domain.Configuration;
using BlockchainTracker.Domain.Interfaces;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace BlockchainTracker.UnitTests.Commands;

public class FetchAllChainsCommandHandlerTests
{
    private readonly IBlockchainApiClient _apiClient = Substitute.For<IBlockchainApiClient>();
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly ILogger<FetchAllChainsCommandHandler> _logger = Substitute.For<ILogger<FetchAllChainsCommandHandler>>();
    private readonly FetchAllChainsCommandHandler _handler;

    public FetchAllChainsCommandHandlerTests()
    {
        var settings = Options.Create(new PollingSettings { MaxDegreeOfParallelism = 1 });
        _handler = new FetchAllChainsCommandHandler(_apiClient, _scopeFactory, settings, _logger);
    }

    [Fact]
    public async Task Handle_FetchesAllSupportedChains()
    {
        var chains = new List<string> { "btc-main", "eth-main" };
        _apiClient.GetSupportedChains().Returns(chains);

        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<FetchChainDataCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<bool>(true));

        var scope = Substitute.For<IServiceScope>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IMediator)).Returns(mediator);
        scope.ServiceProvider.Returns(serviceProvider);
        _scopeFactory.CreateScope().Returns(scope);

        await _handler.Handle(new FetchAllChainsCommand(), CancellationToken.None);

        _apiClient.Received(1).GetSupportedChains();
    }
}

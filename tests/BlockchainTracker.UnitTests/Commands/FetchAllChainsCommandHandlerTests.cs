using BlockchainTracker.Application.Commands;
using BlockchainTracker.Application.Interfaces;
using BlockchainTracker.Domain.Configuration;
using BlockchainTracker.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace BlockchainTracker.UnitTests.Commands;

public class FetchAllChainsCommandHandlerTests
{
    private readonly IBlockchainApiClient _apiClient = Substitute.For<IBlockchainApiClient>();
    private readonly IBlockchainDataFetcherService _fetcherService = Substitute.For<IBlockchainDataFetcherService>();
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly ILogger<FetchAllChainsCommandHandler> _logger = Substitute.For<ILogger<FetchAllChainsCommandHandler>>();
    private readonly FetchAllChainsCommandHandler _handler;

    public FetchAllChainsCommandHandlerTests()
    {
        var scope = Substitute.For<IServiceScope>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IBlockchainDataFetcherService)).Returns(_fetcherService);
        scope.ServiceProvider.Returns(serviceProvider);
        _scopeFactory.CreateScope().Returns(scope);

        var settings = Options.Create(new PollingSettings { MaxDegreeOfParallelism = 1 });
        _handler = new FetchAllChainsCommandHandler(_apiClient, _scopeFactory, settings, _logger);
    }

    [Fact]
    public async Task Handle_FetchesAllSupportedChains()
    {
        var chains = new List<string> { "btc-main", "eth-main" };
        _apiClient.GetSupportedChains().Returns(chains);
        _fetcherService.FetchAndSaveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await _handler.Handle(new FetchAllChainsCommand(), CancellationToken.None);

        _apiClient.Received(1).GetSupportedChains();
        await _fetcherService.Received(2).FetchAndSaveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LogsErrorAndContinuesOnFailure()
    {
        var chains = new List<string> { "btc-main", "eth-main" };
        _apiClient.GetSupportedChains().Returns(chains);
        _fetcherService.FetchAndSaveAsync("btc-main", Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("API error"));
        _fetcherService.FetchAndSaveAsync("eth-main", Arg.Any<CancellationToken>())
            .Returns(true);

        await _handler.Handle(new FetchAllChainsCommand(), CancellationToken.None);

        await _fetcherService.Received(1).FetchAndSaveAsync("eth-main", Arg.Any<CancellationToken>());
    }
}

using BlockchainTracker.Application.Commands;
using BlockchainTracker.Application.Interfaces;
using NSubstitute;

namespace BlockchainTracker.UnitTests.Commands;

public class FetchChainDataCommandHandlerTests
{
    private readonly IBlockchainDataFetcherService _service = Substitute.For<IBlockchainDataFetcherService>();
    private readonly FetchChainDataCommandHandler _handler;

    public FetchChainDataCommandHandlerTests()
    {
        _handler = new FetchChainDataCommandHandler(_service);
    }

    [Fact]
    public async Task Handle_DelegatesToService_ReturnsTrue()
    {
        _service.FetchAndSaveAsync("btc-main", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(new FetchChainDataCommand("btc-main"), CancellationToken.None);

        Assert.True(result);
        await _service.Received(1).FetchAndSaveAsync("btc-main", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateSnapshot_ReturnsFalse()
    {
        _service.FetchAndSaveAsync("btc-main", Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(new FetchChainDataCommand("btc-main"), CancellationToken.None);

        Assert.False(result);
    }
}

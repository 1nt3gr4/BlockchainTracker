using BlockchainTracker.Application.Commands;
using BlockchainTracker.Domain.Entities;
using BlockchainTracker.Domain.Interfaces;
using BlockchainTracker.Domain.Models;
using NSubstitute;

namespace BlockchainTracker.UnitTests.Commands;

public class FetchChainDataCommandHandlerTests
{
    private readonly IBlockchainApiClient _apiClient = Substitute.For<IBlockchainApiClient>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IBlockchainSnapshotRepository _repository = Substitute.For<IBlockchainSnapshotRepository>();
    private readonly FetchChainDataCommandHandler _handler;

    public FetchChainDataCommandHandlerTests()
    {
        _unitOfWork.SnapshotRepository.Returns(_repository);
        _handler = new FetchChainDataCommandHandler(_apiClient, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NewSnapshot_SavesAndReturnsTrue()
    {
        var response = CreateTestResponse();
        _apiClient.GetChainDataAsync("btc-main", Arg.Any<CancellationToken>()).Returns(response);
        _repository.ExistsAsync("btc-main", response.Height, response.Hash, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(new FetchChainDataCommand("btc-main"), CancellationToken.None);

        Assert.True(result);
        await _repository.Received(1).AddAsync(Arg.Any<BlockchainSnapshot>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateSnapshot_ReturnsFalseWithoutSaving()
    {
        var response = CreateTestResponse();
        _apiClient.GetChainDataAsync("btc-main", Arg.Any<CancellationToken>()).Returns(response);
        _repository.ExistsAsync("btc-main", response.Height, response.Hash, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(new FetchChainDataCommand("btc-main"), CancellationToken.None);

        Assert.False(result);
        await _repository.DidNotReceive().AddAsync(Arg.Any<BlockchainSnapshot>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SavesCorrectSnapshotData()
    {
        var response = CreateTestResponse();
        _apiClient.GetChainDataAsync("eth-main", Arg.Any<CancellationToken>()).Returns(response);
        _repository.ExistsAsync("eth-main", response.Height, response.Hash, Arg.Any<CancellationToken>()).Returns(false);

        BlockchainSnapshot? capturedSnapshot = null;
        await _repository.AddAsync(Arg.Do<BlockchainSnapshot>(s => capturedSnapshot = s), Arg.Any<CancellationToken>());

        await _handler.Handle(new FetchChainDataCommand("eth-main"), CancellationToken.None);

        Assert.NotNull(capturedSnapshot);
        Assert.Equal("eth-main", capturedSnapshot.ChainName);
        Assert.Equal(response.Height, capturedSnapshot.Height);
        Assert.Equal(response.Hash, capturedSnapshot.Hash);
    }

    private static BlockchainApiResponse CreateTestResponse() => new()
    {
        Name = "BTC.main",
        Height = 800000,
        Hash = "0000000000000000000abc123def456",
        Time = DateTime.UtcNow,
        PeerCount = 250,
        UnconfirmedCount = 1500,
        HighFeePerKb = 50000,
        MediumFeePerKb = 25000,
        LowFeePerKb = 10000,
        LastForkHeight = 799990
    };
}

using BlockchainTracker.Application;
using BlockchainTracker.Application.Commands;
using BlockchainTracker.Application.Interfaces;
using BlockchainTracker.Domain.Entities;
using BlockchainTracker.Domain.Interfaces;
using BlockchainTracker.Domain.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace BlockchainTracker.UnitTests.Commands;

public class FetchChainDataCommandHandlerTests
{
    private readonly IBlockchainApiClient _apiClient = Substitute.For<IBlockchainApiClient>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICacheService _cache = Substitute.For<ICacheService>();
    private readonly IBlockchainTrackerMetrics _metrics = Substitute.For<IBlockchainTrackerMetrics>();
    private readonly ILogger<FetchChainDataCommandHandler> _logger = Substitute.For<ILogger<FetchChainDataCommandHandler>>();
    private readonly FetchChainDataCommandHandler _handler;

    public FetchChainDataCommandHandlerTests()
    {
        _handler = new FetchChainDataCommandHandler(_apiClient, _unitOfWork, _cache, _metrics, _logger);
    }

    [Fact]
    public async Task Handle_NewSnapshot_SavesAndInvalidatesCache()
    {
        var response = CreateApiResponse("btc-main", 800000, "hash-abc");
        _apiClient.GetChainDataAsync("btc-main", Arg.Any<CancellationToken>()).Returns(response);
        _unitOfWork.Repository.ExistsAsync("btc-main", 800000, "hash-abc", Arg.Any<CancellationToken>()).Returns(false);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await _handler.Handle(new FetchChainDataCommand("btc-main"), CancellationToken.None);

        Assert.True(result);
        _unitOfWork.Repository.Received(1).Add(Arg.Is<BlockchainSnapshot>(s =>
            s.ChainName == "btc-main" && s.Height == 800000 && s.Hash == "hash-abc"));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveAsync(CacheKeys.ChainLatest("btc-main"), Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveAsync(CacheKeys.AllChainsLatest, Arg.Any<CancellationToken>());
        _metrics.Received(1).RecordSnapshotFetched("btc-main");
        _metrics.Received(1).RecordSnapshotSaved("btc-main");
    }

    [Fact]
    public async Task Handle_DuplicateSnapshot_ReturnsFalseAndSkipsSave()
    {
        var response = CreateApiResponse("btc-main", 800000, "hash-abc");
        _apiClient.GetChainDataAsync("btc-main", Arg.Any<CancellationToken>()).Returns(response);
        _unitOfWork.Repository.ExistsAsync("btc-main", 800000, "hash-abc", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(new FetchChainDataCommand("btc-main"), CancellationToken.None);

        Assert.False(result);
        _unitOfWork.Repository.DidNotReceive().Add(Arg.Any<BlockchainSnapshot>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        _metrics.Received(1).RecordDuplicateSkipped("btc-main");
    }

    [Fact]
    public async Task Handle_ApiThrows_RecordsErrorMetricAndPropagates()
    {
        _apiClient.GetChainDataAsync("btc-main", Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("API down"));

        await Assert.ThrowsAsync<HttpRequestException>(
            () => _handler.Handle(new FetchChainDataCommand("btc-main"), CancellationToken.None).AsTask());

        _unitOfWork.Repository.DidNotReceive().Add(Arg.Any<BlockchainSnapshot>());
        _metrics.Received(1).RecordFetchError("btc-main");
        _metrics.Received(1).RecordFetchDuration("btc-main", Arg.Any<double>());
    }

    [Fact]
    public async Task Handle_Cancelled_PropagatesWithoutRecordingError()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _apiClient.GetChainDataAsync("btc-main", Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _handler.Handle(new FetchChainDataCommand("btc-main"), cts.Token).AsTask());

        _metrics.DidNotReceive().RecordFetchError(Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_NewSnapshot_SetsCorrectFieldsOnEntity()
    {
        var time = new DateTimeOffset(2024, 6, 15, 10, 0, 0, TimeSpan.Zero);
        var response = new BlockchainApiResponse
        {
            Name = "BTC.main",
            Height = 850000,
            Hash = "0000abc",
            Time = time,
            PeerCount = 100,
            UnconfirmedCount = 50
        };
        _apiClient.GetChainDataAsync("btc-main", Arg.Any<CancellationToken>()).Returns(response);
        _unitOfWork.Repository.ExistsAsync("btc-main", 850000, "0000abc", Arg.Any<CancellationToken>()).Returns(false);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        await _handler.Handle(new FetchChainDataCommand("btc-main"), CancellationToken.None);

        _unitOfWork.Repository.Received(1).Add(Arg.Is<BlockchainSnapshot>(s =>
            s.ChainName == "btc-main" &&
            s.Height == 850000 &&
            s.Hash == "0000abc" &&
            s.Time == time &&
            s.FetchedAt > DateTimeOffset.UtcNow.AddMinutes(-1)));
    }

    [Fact]
    public async Task Handle_NewSnapshot_SerializesRawJsonWithSnakeCase()
    {
        var response = new BlockchainApiResponse
        {
            Name = "ETH.main",
            Height = 18000000,
            Hash = "0xabc",
            Time = DateTimeOffset.UtcNow,
            PeerCount = 200,
            HighGasPrice = 50
        };
        _apiClient.GetChainDataAsync("eth-main", Arg.Any<CancellationToken>()).Returns(response);
        _unitOfWork.Repository.ExistsAsync("eth-main", 18000000, "0xabc", Arg.Any<CancellationToken>()).Returns(false);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        await _handler.Handle(new FetchChainDataCommand("eth-main"), CancellationToken.None);

        _unitOfWork.Repository.Received(1).Add(Arg.Is<BlockchainSnapshot>(s =>
            s.RawJson.Contains("peer_count") &&
            s.RawJson.Contains("high_gas_price")));
    }

    [Fact]
    public async Task Handle_AlwaysRecordsFetchDuration()
    {
        var response = CreateApiResponse("btc-main", 800000, "hash-abc");
        _apiClient.GetChainDataAsync("btc-main", Arg.Any<CancellationToken>()).Returns(response);
        _unitOfWork.Repository.ExistsAsync("btc-main", 800000, "hash-abc", Arg.Any<CancellationToken>()).Returns(false);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        await _handler.Handle(new FetchChainDataCommand("btc-main"), CancellationToken.None);

        _metrics.Received(1).RecordFetchDuration("btc-main", Arg.Any<double>());
    }

    private static BlockchainApiResponse CreateApiResponse(string name, long height, string hash) => new()
    {
        Name = name,
        Height = height,
        Hash = hash,
        Time = DateTimeOffset.UtcNow,
        PeerCount = 10,
        UnconfirmedCount = 5
    };
}

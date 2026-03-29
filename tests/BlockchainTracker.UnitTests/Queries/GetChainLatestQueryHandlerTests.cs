using BlockchainTracker.Application.Dtos;
using BlockchainTracker.Application.Interfaces;
using BlockchainTracker.Application.Queries;
using BlockchainTracker.Domain.Configuration;
using BlockchainTracker.Domain.Entities;
using BlockchainTracker.Domain.Interfaces;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace BlockchainTracker.UnitTests.Queries;

public class GetChainLatestQueryHandlerTests
{
    private readonly IBlockchainSnapshotRepository _repository = Substitute.For<IBlockchainSnapshotRepository>();
    private readonly ICacheService _cache = Substitute.For<ICacheService>();
    private readonly GetChainLatestQueryHandler _handler;

    public GetChainLatestQueryHandlerTests()
    {
        var settings = Options.Create(new CacheSettings());
        _handler = new GetChainLatestQueryHandler(_repository, _cache, settings);
    }

    [Fact]
    public async Task Handle_CacheHit_ReturnsCachedValue()
    {
        var cached = new BlockchainSnapshotDto { ChainName = "btc-main", Hash = "abc", Height = 100, FetchedAt = DateTime.UtcNow };
        _cache.GetAsync<BlockchainSnapshotDto>("chain:latest:btc-main", Arg.Any<CancellationToken>()).Returns(cached);

        var result = await _handler.Handle(new GetChainLatestQuery("btc-main"), CancellationToken.None);

        Assert.Equal(cached, result);
        await _repository.DidNotReceive().GetLatestByChainAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CacheMiss_QueriesRepositoryAndCaches()
    {
        _cache.GetAsync<BlockchainSnapshotDto>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((BlockchainSnapshotDto?)null);

        var snapshot = CreateTestSnapshot();
        _repository.GetLatestByChainAsync("btc-main", Arg.Any<CancellationToken>()).Returns(snapshot);

        var result = await _handler.Handle(new GetChainLatestQuery("btc-main"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("btc-main", result.ChainName);
        await _cache.Received(1).SetAsync(
            "chain:latest:btc-main",
            Arg.Any<BlockchainSnapshotDto>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoData_ReturnsNull()
    {
        _cache.GetAsync<BlockchainSnapshotDto>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((BlockchainSnapshotDto?)null);
        _repository.GetLatestByChainAsync("unknown", Arg.Any<CancellationToken>())
            .Returns((BlockchainSnapshot?)null);

        var result = await _handler.Handle(new GetChainLatestQuery("unknown"), CancellationToken.None);

        Assert.Null(result);
    }

    private static BlockchainSnapshot CreateTestSnapshot() => new()
    {
        Id = 1,
        ChainName = "btc-main",
        Height = 800000,
        Hash = "0000000000abc",
        Time = DateTime.UtcNow,
        PeerCount = 250,
        UnconfirmedCount = 1500,
        HighFeePerKb = 50000,
        MediumFeePerKb = 25000,
        LowFeePerKb = 10000,
        LastForkHeight = 799990,
        RawJson = "{}",
        FetchedAt = DateTime.UtcNow
    };
}

using BlockchainTracker.Application.Dtos;
using BlockchainTracker.Application.Interfaces;
using BlockchainTracker.Application.Queries;
using BlockchainTracker.Domain.Configuration;
using BlockchainTracker.Domain.Entities;
using BlockchainTracker.Domain.Interfaces;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace BlockchainTracker.UnitTests.Queries;

public class GetAllChainsLatestQueryHandlerTests
{
    private readonly IBlockchainSnapshotRepository _repository = Substitute.For<IBlockchainSnapshotRepository>();
    private readonly ICacheService _cache = Substitute.For<ICacheService>();
    private readonly GetAllChainsLatestQueryHandler _handler;

    public GetAllChainsLatestQueryHandlerTests()
    {
        var settings = Options.Create(new CacheSettings());
        _handler = new GetAllChainsLatestQueryHandler(_repository, _cache, settings);
    }

    [Fact]
    public async Task Handle_CacheHit_ReturnsCachedValueWithoutQueryingRepository()
    {
        var cached = new List<BlockchainSnapshotDto>
        {
            new() { ChainName = "btc-main", Hash = "abc", Height = 100, RawJson = "{}", FetchedAt = DateTimeOffset.UtcNow }
        };
        _cache.GetAsync<IReadOnlyList<BlockchainSnapshotDto>>("chains:latest:all", Arg.Any<CancellationToken>())
            .Returns(cached);

        var result = await _handler.Handle(new GetAllChainsLatestQuery(), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("btc-main", result[0].ChainName);
        await _repository.DidNotReceive().GetLatestPerChainAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CacheMiss_QueriesRepositoryAndCachesResult()
    {
        _cache.GetAsync<IReadOnlyList<BlockchainSnapshotDto>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<BlockchainSnapshotDto>?)null);

        var snapshots = new List<BlockchainSnapshot>
        {
            new() { Id = 1, ChainName = "btc-main", Height = 800000, Hash = "abc", Time = DateTimeOffset.UtcNow, RawJson = "{}", FetchedAt = DateTimeOffset.UtcNow },
            new() { Id = 2, ChainName = "eth-main", Height = 18000000, Hash = "def", Time = DateTimeOffset.UtcNow, RawJson = "{}", FetchedAt = DateTimeOffset.UtcNow }
        };
        _repository.GetLatestPerChainAsync(Arg.Any<CancellationToken>()).Returns(snapshots);

        var result = await _handler.Handle(new GetAllChainsLatestQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("btc-main", result[0].ChainName);
        Assert.Equal("eth-main", result[1].ChainName);
        await _cache.Received(1).SetAsync(
            "chains:latest:all",
            Arg.Any<IReadOnlyList<BlockchainSnapshotDto>>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CacheMiss_NoData_ReturnsEmptyAndCachesEmptyList()
    {
        _cache.GetAsync<IReadOnlyList<BlockchainSnapshotDto>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<BlockchainSnapshotDto>?)null);
        _repository.GetLatestPerChainAsync(Arg.Any<CancellationToken>()).Returns(new List<BlockchainSnapshot>());

        var result = await _handler.Handle(new GetAllChainsLatestQuery(), CancellationToken.None);

        Assert.Empty(result);
        await _cache.Received(1).SetAsync(
            "chains:latest:all",
            Arg.Any<IReadOnlyList<BlockchainSnapshotDto>>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>());
    }
}

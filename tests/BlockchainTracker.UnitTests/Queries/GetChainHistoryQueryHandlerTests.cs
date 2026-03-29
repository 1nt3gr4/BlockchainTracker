using BlockchainTracker.Application.Dtos;
using BlockchainTracker.Application.Interfaces;
using BlockchainTracker.Application.Queries;
using BlockchainTracker.Domain.Configuration;
using BlockchainTracker.Domain.Entities;
using BlockchainTracker.Domain.Interfaces;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace BlockchainTracker.UnitTests.Queries;

public class GetChainHistoryQueryHandlerTests
{
    private readonly IBlockchainSnapshotRepository _repository = Substitute.For<IBlockchainSnapshotRepository>();
    private readonly ICacheService _cache = Substitute.For<ICacheService>();
    private readonly GetChainHistoryQueryHandler _handler;

    public GetChainHistoryQueryHandlerTests()
    {
        var settings = Options.Create(new CacheSettings());
        _handler = new GetChainHistoryQueryHandler(_repository, _cache, settings);
    }

    [Fact]
    public async Task Handle_ReturnsPagedResult()
    {
        _cache.GetAsync<PagedResult<BlockchainSnapshotDto>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((PagedResult<BlockchainSnapshotDto>?)null);

        var snapshots = new List<BlockchainSnapshot>
        {
            CreateTestSnapshot(1, 800001),
            CreateTestSnapshot(2, 800000)
        };
        _repository.GetHistoryAsync("btc-main", 1, 20, Arg.Any<CancellationToken>())
            .Returns((snapshots, 50));

        var result = await _handler.Handle(new GetChainHistoryQuery("btc-main", 1, 20), CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(50, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public async Task Handle_CacheHit_ReturnsCached()
    {
        var cached = new PagedResult<BlockchainSnapshotDto>
        {
            Items = [],
            TotalCount = 0,
            Page = 1,
            PageSize = 20
        };
        _cache.GetAsync<PagedResult<BlockchainSnapshotDto>>("chain:history:btc-main:1:20", Arg.Any<CancellationToken>())
            .Returns(cached);

        var result = await _handler.Handle(new GetChainHistoryQuery("btc-main", 1, 20), CancellationToken.None);

        Assert.Same(cached, result);
        await _repository.DidNotReceive().GetHistoryAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    private static BlockchainSnapshot CreateTestSnapshot(int id, long height) => new()
    {
        Id = id,
        ChainName = "btc-main",
        Height = height,
        Hash = $"hash-{height}",
        Time = DateTimeOffset.UtcNow,
        PeerCount = 250,
        UnconfirmedCount = 1500,
        LastForkHeight = height - 10,
        RawJson = "{}",
        FetchedAt = DateTimeOffset.UtcNow
    };
}

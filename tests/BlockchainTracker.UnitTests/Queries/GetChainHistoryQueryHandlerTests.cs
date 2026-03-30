using BlockchainTracker.Application.Dtos;
using BlockchainTracker.Application.Queries;
using BlockchainTracker.Domain.Entities;
using BlockchainTracker.Domain.Interfaces;
using NSubstitute;

namespace BlockchainTracker.UnitTests.Queries;

public class GetChainHistoryQueryHandlerTests
{
    private readonly IBlockchainSnapshotRepository _repository = Substitute.For<IBlockchainSnapshotRepository>();
    private readonly GetChainHistoryQueryHandler _handler;

    public GetChainHistoryQueryHandlerTests()
    {
        _handler = new GetChainHistoryQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsPagedResult()
    {
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

    private static BlockchainSnapshot CreateTestSnapshot(int id, long height) => new()
    {
        Id = id,
        ChainName = "btc-main",
        Height = height,
        Hash = $"hash-{height}",
        Time = DateTimeOffset.UtcNow,
        RawJson = "{}",
        FetchedAt = DateTimeOffset.UtcNow
    };
}

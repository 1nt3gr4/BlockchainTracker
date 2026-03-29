using BlockchainTracker.Domain.Entities;
using BlockchainTracker.Infrastructure.Persistence;
using BlockchainTracker.IntegrationTests.Fixtures;

namespace BlockchainTracker.IntegrationTests.Persistence;

public class BlockchainSnapshotRepositoryTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture = new();
    private BlockchainDbContext _repoContext = null!;
    private BlockchainSnapshotRepository _repository = null!;

    public async ValueTask InitializeAsync()
    {
        await _fixture.InitializeAsync();
        _repoContext = _fixture.CreateContext();
        _repository = new BlockchainSnapshotRepository(_repoContext);
    }

    public async ValueTask DisposeAsync()
    {
        await _repoContext.DisposeAsync();
        await _fixture.DisposeAsync();
    }

    [Fact]
    public async Task GetLatestByChainAsync_ReturnsLatest()
    {
        await SeedSnapshot(CreateSnapshot("eth-main", 100));
        await SeedSnapshot(CreateSnapshot("eth-main", 200));

        var result = await _repository.GetLatestByChainAsync("eth-main");

        Assert.NotNull(result);
        Assert.Equal(200, result.Height);
    }

    [Fact]
    public async Task GetLatestPerChainAsync_ReturnsOnePerChain()
    {
        await SeedSnapshot(CreateSnapshot("btc-main", 100));
        await SeedSnapshot(CreateSnapshot("btc-main", 200));
        await SeedSnapshot(CreateSnapshot("eth-main", 500));

        var results = await _repository.GetLatestPerChainAsync();

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsPaged()
    {
        for (var i = 0; i < 25; i++)
            await SeedSnapshot(CreateSnapshot("ltc-main", 1000 + i));

        var (items, totalCount) = await _repository.GetHistoryAsync("ltc-main", 1, 10);

        Assert.Equal(10, items.Count);
        Assert.Equal(25, totalCount);
    }

    [Fact]
    public async Task GetLatestByChainAsync_ReturnsNullForNonExisting()
    {
        var result = await _repository.GetLatestByChainAsync("nonexistent");

        Assert.Null(result);
    }

    private async Task SeedSnapshot(BlockchainSnapshot snapshot)
    {
        await using var context = _fixture.CreateContext();
        context.Snapshots.Add(snapshot);
        await context.SaveChangesAsync();
    }

    private static int _counter;

    private static BlockchainSnapshot CreateSnapshot(string chain, long height) => new()
    {
        ChainName = chain,
        Height = height,
        Hash = $"hash-{chain}-{height}-{++_counter}",
        Time = DateTimeOffset.UtcNow,
        RawJson = "{}",
        FetchedAt = DateTimeOffset.UtcNow
    };
}

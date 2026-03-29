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

        var result = await _repository.GetLatestByChainAsync("eth-main", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(200, result.Height);
    }

    [Fact]
    public async Task GetLatestPerChainAsync_ReturnsOnePerChain()
    {
        await SeedSnapshot(CreateSnapshot("btc-main", 100));
        await SeedSnapshot(CreateSnapshot("btc-main", 200));
        await SeedSnapshot(CreateSnapshot("eth-main", 500));

        var results = await _repository.GetLatestPerChainAsync(CancellationToken.None);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsPaged()
    {
        for (var i = 0; i < 25; i++)
            await SeedSnapshot(CreateSnapshot("ltc-main", 1000 + i));

        var (items, totalCount) = await _repository.GetHistoryAsync("ltc-main", 1, 10, CancellationToken.None);

        Assert.Equal(10, items.Count);
        Assert.Equal(25, totalCount);
    }

    [Fact]
    public async Task GetLatestByChainAsync_ReturnsNullForNonExisting()
    {
        var result = await _repository.GetLatestByChainAsync("nonexistent", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrueForExistingSnapshot()
    {
        await SeedSnapshot(CreateSnapshot("btc-main", 500));

        var exists = await _repository.ExistsAsync("btc-main", 500, $"hash-btc-main-500-{_counter}", CancellationToken.None);

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalseWhenNotFound()
    {
        var exists = await _repository.ExistsAsync("btc-main", 999999, "nonexistent-hash", CancellationToken.None);

        Assert.False(exists);
    }

    [Fact]
    public async Task Add_PersistsAndRetrievesSnapshot()
    {
        var snapshot = CreateSnapshot("dash-main", 700);
        _repository.Add(snapshot);
        await _repoContext.SaveChangesAsync();

        await using var verifyContext = _fixture.CreateContext();
        var repo = new BlockchainSnapshotRepository(verifyContext);
        var result = await repo.GetLatestByChainAsync("dash-main", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(700, result.Height);
        Assert.Equal("dash-main", result.ChainName);
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsSecondPage()
    {
        for (var i = 0; i < 15; i++)
            await SeedSnapshot(CreateSnapshot("btc-main", 2000 + i));

        var (items, totalCount) = await _repository.GetHistoryAsync("btc-main", 2, 10, CancellationToken.None);

        Assert.Equal(5, items.Count);
        Assert.Equal(15, totalCount);
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsEmptyForNonExistingChain()
    {
        var (items, totalCount) = await _repository.GetHistoryAsync("nonexistent", 1, 10, CancellationToken.None);

        Assert.Empty(items);
        Assert.Equal(0, totalCount);
    }

    [Fact]
    public async Task GetHistoryAsync_FiltersOnlyRequestedChain()
    {
        for (var i = 0; i < 5; i++)
            await SeedSnapshot(CreateSnapshot("eth-main", 3000 + i));
        for (var i = 0; i < 3; i++)
            await SeedSnapshot(CreateSnapshot("btc-main", 4000 + i));

        var (items, totalCount) = await _repository.GetHistoryAsync("eth-main", 1, 20, CancellationToken.None);

        Assert.Equal(5, totalCount);
        Assert.All(items, item => Assert.Equal("eth-main", item.ChainName));
    }

    [Fact]
    public async Task GetLatestByChainAsync_FiltersCorrectlyWithMultipleChains()
    {
        await SeedSnapshot(CreateSnapshot("btc-main", 100));
        await SeedSnapshot(CreateSnapshot("eth-main", 200));
        await SeedSnapshot(CreateSnapshot("btc-main", 300));

        var result = await _repository.GetLatestByChainAsync("btc-main", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("btc-main", result.ChainName);
        Assert.Equal(300, result.Height);
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsOrderedByFetchedAtDescending()
    {
        for (var i = 0; i < 5; i++)
        {
            var snapshot = CreateSnapshot("ltc-main", 5000 + i);
            snapshot.FetchedAt = DateTimeOffset.UtcNow.AddMinutes(i);
            await SeedSnapshot(snapshot);
        }

        var (items, _) = await _repository.GetHistoryAsync("ltc-main", 1, 5, CancellationToken.None);

        for (var i = 0; i < items.Count - 1; i++)
        {
            Assert.True(items[i].FetchedAt >= items[i + 1].FetchedAt);
        }
    }

    [Fact]
    public async Task GetLatestPerChainAsync_ReturnsEmptyForEmptyDatabase()
    {
        var results = await _repository.GetLatestPerChainAsync(CancellationToken.None);

        Assert.Empty(results);
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

using BlockchainTracker.Domain.Entities;
using BlockchainTracker.Infrastructure.Persistence;
using BlockchainTracker.IntegrationTests.Fixtures;

namespace BlockchainTracker.IntegrationTests.Persistence;

public class BlockchainSnapshotRepositoryTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture = new();
    private BlockchainSnapshotRepository _repository = null!;
    private BlockchainDbContext _context = null!;

    public async ValueTask InitializeAsync()
    {
        await _fixture.InitializeAsync();
        _context = _fixture.CreateContext();
        _repository = new BlockchainSnapshotRepository(_context);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _fixture.DisposeAsync();
    }

    [Fact]
    public async Task AddAsync_PersistsSnapshot()
    {
        var snapshot = CreateSnapshot("btc-main", 800000);

        await _repository.AddAsync(snapshot);
        await _context.SaveChangesAsync();

        var result = await _repository.GetLatestByChainAsync("btc-main");
        Assert.NotNull(result);
        Assert.Equal(800000, result.Height);
    }

    [Fact]
    public async Task GetLatestByChainAsync_ReturnsLatest()
    {
        await _repository.AddAsync(CreateSnapshot("eth-main", 100));
        await _repository.AddAsync(CreateSnapshot("eth-main", 200));
        await _context.SaveChangesAsync();

        var result = await _repository.GetLatestByChainAsync("eth-main");

        Assert.NotNull(result);
        Assert.Equal(200, result.Height);
    }

    [Fact]
    public async Task GetLatestPerChainAsync_ReturnsOnePerChain()
    {
        await _repository.AddAsync(CreateSnapshot("btc-main", 100));
        await _repository.AddAsync(CreateSnapshot("btc-main", 200));
        await _repository.AddAsync(CreateSnapshot("eth-main", 500));
        await _context.SaveChangesAsync();

        var results = await _repository.GetLatestPerChainAsync();

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsPaged()
    {
        for (var i = 0; i < 25; i++)
            await _repository.AddAsync(CreateSnapshot("ltc-main", 1000 + i));
        await _context.SaveChangesAsync();

        var (items, totalCount) = await _repository.GetHistoryAsync("ltc-main", 1, 10);

        Assert.Equal(10, items.Count);
        Assert.Equal(25, totalCount);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrueForExisting()
    {
        var snapshot = CreateSnapshot("dash-main", 999);
        await _repository.AddAsync(snapshot);
        await _context.SaveChangesAsync();

        var exists = await _repository.ExistsAsync("dash-main", 999, snapshot.Hash);

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalseForNonExisting()
    {
        var exists = await _repository.ExistsAsync("nonexistent", 0, "nohash");

        Assert.False(exists);
    }

    private static int _counter;

    private static BlockchainSnapshot CreateSnapshot(string chain, long height) => new()
    {
        ChainName = chain,
        Height = height,
        Hash = $"hash-{chain}-{height}-{++_counter}",
        Time = DateTime.UtcNow,
        PeerCount = 250,
        UnconfirmedCount = 1500,
        RawJson = "{}",
        FetchedAt = DateTime.UtcNow
    };
}

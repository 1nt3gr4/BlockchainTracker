using BlockchainTracker.Domain.Entities;
using BlockchainTracker.Infrastructure.Persistence;
using BlockchainTracker.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace BlockchainTracker.IntegrationTests.Persistence;

public class UnitOfWorkTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture = new();

    public async ValueTask InitializeAsync() => await _fixture.InitializeAsync();
    public async ValueTask DisposeAsync() => await _fixture.DisposeAsync();

    [Fact]
    public async Task SaveChangesAsync_PersistsAddedSnapshot()
    {
        await using var context = _fixture.CreateContext();
        var repository = new BlockchainSnapshotRepository(context);
        var unitOfWork = new UnitOfWork(context, repository);

        var snapshot = new BlockchainSnapshot
        {
            ChainName = "btc-main",
            Height = 900000,
            Hash = "uow-test-hash",
            Time = DateTimeOffset.UtcNow,
            RawJson = "{}",
            FetchedAt = DateTimeOffset.UtcNow
        };

        unitOfWork.Repository.Add(snapshot);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        await using var verifyContext = _fixture.CreateContext();
        var persisted = await verifyContext.Snapshots
            .FirstOrDefaultAsync(s => s.Hash == "uow-test-hash");

        Assert.NotNull(persisted);
        Assert.Equal("btc-main", persisted.ChainName);
        Assert.Equal(900000, persisted.Height);
    }

    [Fact]
    public async Task Repository_ExistsAsync_ReturnsTrueForExistingSnapshot()
    {
        await using var seedContext = _fixture.CreateContext();
        seedContext.Snapshots.Add(new BlockchainSnapshot
        {
            ChainName = "eth-main",
            Height = 18000000,
            Hash = "exists-test-hash",
            Time = DateTimeOffset.UtcNow,
            RawJson = "{}",
            FetchedAt = DateTimeOffset.UtcNow
        });
        await seedContext.SaveChangesAsync();

        await using var context = _fixture.CreateContext();
        var repository = new BlockchainSnapshotRepository(context);
        var unitOfWork = new UnitOfWork(context, repository);

        var exists = await unitOfWork.Repository.ExistsAsync("eth-main", 18000000, "exists-test-hash", CancellationToken.None);

        Assert.True(exists);
    }

    [Fact]
    public async Task Repository_ExistsAsync_ReturnsFalseForNonExisting()
    {
        await using var context = _fixture.CreateContext();
        var repository = new BlockchainSnapshotRepository(context);
        var unitOfWork = new UnitOfWork(context, repository);

        var exists = await unitOfWork.Repository.ExistsAsync("nonexistent", 0, "no-hash", CancellationToken.None);

        Assert.False(exists);
    }
}

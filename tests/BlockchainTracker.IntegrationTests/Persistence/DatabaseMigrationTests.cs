using BlockchainTracker.Infrastructure.Persistence;
using BlockchainTracker.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace BlockchainTracker.IntegrationTests.Persistence;

public class DatabaseMigrationTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture = new();

    public async ValueTask InitializeAsync() => await _fixture.InitializeAsync();
    public async ValueTask DisposeAsync() => await _fixture.DisposeAsync();

    [Fact]
    public async Task Database_CreatesSchemaSuccessfully()
    {
        await using var context = _fixture.CreateContext();

        var canConnect = await context.Database.CanConnectAsync();
        Assert.True(canConnect);
    }

    [Fact]
    public async Task Database_HasSnapshotsTable()
    {
        await using var context = _fixture.CreateContext();

        var count = await context.Snapshots.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Database_UniqueIndex_PreventsDuplicateInserts()
    {
        await using var context = _fixture.CreateContext();

        var snapshot1 = new Domain.Entities.BlockchainSnapshot
        {
            ChainName = "btc-main",
            Height = 900000,
            Hash = "unique-index-test-hash",
            Time = DateTimeOffset.UtcNow,
            RawJson = "{}",
            FetchedAt = DateTimeOffset.UtcNow
        };

        var snapshot2 = new Domain.Entities.BlockchainSnapshot
        {
            ChainName = "btc-main",
            Height = 900000,
            Hash = "unique-index-test-hash",
            Time = DateTimeOffset.UtcNow,
            RawJson = "{}",
            FetchedAt = DateTimeOffset.UtcNow
        };

        context.Snapshots.Add(snapshot1);
        await context.SaveChangesAsync();

        context.Snapshots.Add(snapshot2);
        var ex = await Assert.ThrowsAsync<DbUpdateException>(
            async () => await context.SaveChangesAsync());

        Assert.NotNull(ex.InnerException);
    }

    [Fact]
    public async Task Database_Migrate_IsIdempotent()
    {
        await using var context = _fixture.CreateContext();

        // Running Migrate again after fixture already migrated should succeed without error
        await context.Database.MigrateAsync();
    }
}

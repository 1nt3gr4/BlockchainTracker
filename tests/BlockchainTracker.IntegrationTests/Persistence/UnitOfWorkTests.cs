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
    public async Task SaveChangesAsync_PersistsAddedEntities()
    {
        var factory = CreateDbContextFactory();
        await using var uow = new UnitOfWork(factory);

        var snapshot = new Domain.Entities.BlockchainSnapshot
        {
            ChainName = "btc-main",
            Height = 100,
            Hash = "uow-test-hash",
            Time = DateTime.UtcNow,
            RawJson = "{}",
            FetchedAt = DateTime.UtcNow
        };

        await uow.SnapshotRepository.AddAsync(snapshot);
        await uow.SaveChangesAsync();

        await using var verifyContext = _fixture.CreateContext();
        var saved = await verifyContext.Snapshots.FirstOrDefaultAsync(s => s.Hash == "uow-test-hash");
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task DisposeAsync_DisposesContext()
    {
        var factory = CreateDbContextFactory();
        var uow = new UnitOfWork(factory);
        await uow.DisposeAsync();

        // Verify that subsequent operations throw
        await Assert.ThrowsAsync<ObjectDisposedException>(
            async () => await uow.SaveChangesAsync());
    }

    private IDbContextFactory<BlockchainDbContext> CreateDbContextFactory()
    {
        return new SimpleDbContextFactory(_fixture.ConnectionString);
    }

    private sealed class SimpleDbContextFactory : IDbContextFactory<BlockchainDbContext>
    {
        private readonly string _connectionString;

        public SimpleDbContextFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public BlockchainDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<BlockchainDbContext>()
                .UseNpgsql(_connectionString)
                .Options;
            return new BlockchainDbContext(options);
        }
    }
}

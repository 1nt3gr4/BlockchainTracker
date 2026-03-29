using BlockchainTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace BlockchainTracker.IntegrationTests.Fixtures;

public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public BlockchainDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BlockchainDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new BlockchainDbContext(options);
    }

    public IDbContextFactory<BlockchainDbContext> CreateDbContextFactory()
    {
        return new SimpleDbContextFactory(ConnectionString);
    }

    private sealed class SimpleDbContextFactory(string connectionString) : IDbContextFactory<BlockchainDbContext>
    {
        public BlockchainDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<BlockchainDbContext>()
                .UseNpgsql(connectionString)
                .Options;
            return new BlockchainDbContext(options);
        }
    }
}

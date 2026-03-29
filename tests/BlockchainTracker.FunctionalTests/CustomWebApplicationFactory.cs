using BlockchainTracker.Domain.Interfaces;
using BlockchainTracker.Domain.Models;
using BlockchainTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace BlockchainTracker.FunctionalTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    public IBlockchainApiClient MockApiClient { get; } = Substitute.For<IBlockchainApiClient>();

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(
            _postgresContainer.StartAsync(),
            _redisContainer.StartAsync());
    }

    public new async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgresContainer.DisposeAsync();
        await _redisContainer.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<BlockchainDbContext>>();
            services.RemoveAll<IDbContextFactory<BlockchainDbContext>>();

            services.AddPooledDbContextFactory<BlockchainDbContext>(options =>
                options.UseNpgsql(_postgresContainer.GetConnectionString()));

            services.RemoveAll<IConnectionMultiplexer>();
            var redisMultiplexer = ConnectionMultiplexer.Connect(_redisContainer.GetConnectionString());
            services.AddSingleton<IConnectionMultiplexer>(redisMultiplexer);

            services.RemoveAll<IDistributedLockFactory>();
            services.AddSingleton<IDistributedLockFactory>(
                _ => RedLockFactory.Create([new RedLockMultiplexer(redisMultiplexer)]));

            services.RemoveAll<IBlockchainApiClient>();
            services.AddSingleton(MockApiClient);

            // Ensure DB schema is up to date via migrations
            using var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<BlockchainDbContext>>();
            using var context = factory.CreateDbContext();
            context.Database.Migrate();
        });

        builder.UseEnvironment("Testing");
    }
}

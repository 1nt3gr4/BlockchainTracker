using BlockchainTracker.Domain.Interfaces;
using BlockchainTracker.Domain.Models;
using BlockchainTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Testcontainers.PostgreSql;

namespace BlockchainTracker.FunctionalTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    public IBlockchainApiClient MockApiClient { get; } = Substitute.For<IBlockchainApiClient>();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
    }

    public new async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _container.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<BlockchainDbContext>>();
            services.RemoveAll<IDbContextFactory<BlockchainDbContext>>();

            services.AddPooledDbContextFactory<BlockchainDbContext>(options =>
                options.UseNpgsql(_container.GetConnectionString()));

            services.RemoveAll<IBlockchainApiClient>();
            services.AddSingleton(MockApiClient);

            // Ensure DB is created
            using var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<BlockchainDbContext>>();
            using var context = factory.CreateDbContext();
            context.Database.EnsureCreated();
        });

        builder.UseEnvironment("Testing");
    }
}

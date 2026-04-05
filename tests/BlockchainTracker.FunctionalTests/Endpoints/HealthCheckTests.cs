using System.Net;
using BlockchainTracker.Domain.Entities;
using BlockchainTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlockchainTracker.FunctionalTests.Endpoints;

public class HealthCheckTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public HealthCheckTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsSuccess()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthEndpoint_WithFreshData_ReturnsHealthy()
    {
        await SeedSnapshot("btc-main", 900000, DateTimeOffset.UtcNow);

        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Healthy", content);
    }

    [Fact]
    public async Task HealthEndpoint_WithStaleData_ReturnsDegraded()
    {
        var staleTime = DateTimeOffset.UtcNow.AddMinutes(-15);
        await SeedSnapshot("stale-health-chain", 18000000, staleTime);

        // Reset the health check cache by resolving the singleton and waiting
        // for its 1-minute TTL to expire. Instead, use a separate factory
        // instance to guarantee a fresh health check evaluation.
        await using var freshFactory = new CustomWebApplicationFactory();
        await freshFactory.InitializeAsync();
        await SeedSnapshotInFactory(freshFactory, "stale-health-chain", 18000000, staleTime);

        using var freshClient = freshFactory.CreateClient();
        var response = await freshClient.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Degraded", content);
    }

    private static async Task SeedSnapshotInFactory(
        CustomWebApplicationFactory factory, string chain, long height, DateTimeOffset fetchedAt)
    {
        using var scope = factory.Services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<BlockchainDbContext>>();
        await using var context = await dbFactory.CreateDbContextAsync();
        context.Snapshots.Add(new BlockchainSnapshot
        {
            ChainName = chain,
            Height = height,
            Hash = $"health-{chain}-{height}-{++_healthSeedCounter}",
            Time = DateTimeOffset.UtcNow,
            RawJson = "{}",
            FetchedAt = fetchedAt
        });
        await context.SaveChangesAsync();
    }

    private static int _healthSeedCounter;

    private async Task SeedSnapshot(string chain, long height, DateTimeOffset fetchedAt)
    {
        using var scope = _factory.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<BlockchainDbContext>>();
        await using var context = await factory.CreateDbContextAsync();
        context.Snapshots.Add(new BlockchainSnapshot
        {
            ChainName = chain,
            Height = height,
            Hash = $"health-{chain}-{height}-{++_healthSeedCounter}",
            Time = DateTimeOffset.UtcNow,
            RawJson = "{}",
            FetchedAt = fetchedAt
        });
        await context.SaveChangesAsync();
    }
}

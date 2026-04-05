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
        await using var freshFactory = new CustomWebApplicationFactory();
        await freshFactory.InitializeAsync();
        await SeedSnapshotInFactory(freshFactory, "btc-main", 900000, DateTimeOffset.UtcNow);

        using var freshClient = freshFactory.CreateClient();
        var response = await freshClient.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Healthy", content);
    }

    [Fact]
    public async Task HealthEndpoint_WithStaleData_ReturnsDegraded()
    {
        var staleTime = DateTimeOffset.UtcNow.AddMinutes(-15);

        // Use a separate factory instance to guarantee a fresh health check
        // evaluation with no cached results or data from other tests.
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

}

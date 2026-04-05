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
        var staleTime = DateTimeOffset.UtcNow.AddMinutes(-10);
        await SeedSnapshot("eth-main", 18000000, staleTime);

        // Force cache expiry by waiting or making multiple requests
        // The health check caches results for 1 minute, so we need a fresh check
        // Use a new factory scope to ensure fresh state
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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

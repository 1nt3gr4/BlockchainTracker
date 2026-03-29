using System.Net;
using System.Net.Http.Json;
using BlockchainTracker.Application.Dtos;
using BlockchainTracker.Domain.Entities;
using BlockchainTracker.Domain.Models;
using BlockchainTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace BlockchainTracker.FunctionalTests.Endpoints;

public class ChainEndpointsTests : IAsyncLifetime, IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ChainEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public ValueTask InitializeAsync() => default;

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return default;
    }

    [Fact]
    public async Task GetTrackedChains_ReturnsListOfChains()
    {
        _factory.MockApiClient.GetSupportedChains()
            .Returns(new List<string> { "btc-main", "eth-main", "ltc-main", "dash-main", "btc-test3" });

        var response = await _client.GetAsync("/api/chains/tracked");

        response.EnsureSuccessStatusCode();
        var chains = await response.Content.ReadFromJsonAsync<List<string>>();
        Assert.NotNull(chains);
        Assert.Equal(5, chains.Count);
    }

    [Fact]
    public async Task GetLatestByChain_WhenNoData_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/chains/nonexistent/latest");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetLatestByChain_WithData_ReturnsSnapshot()
    {
        await SeedSnapshot("btc-main", 800000);

        var response = await _client.GetAsync("/api/chains/btc-main/latest");

        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<BlockchainSnapshotDto>();
        Assert.NotNull(dto);
        Assert.Equal("btc-main", dto.ChainName);
        Assert.Equal(800000, dto.Height);
    }

    [Fact]
    public async Task GetAllChainsLatest_ReturnsAll()
    {
        await SeedSnapshot("eth-main", 18000000);

        var response = await _client.GetAsync("/api/chains");

        response.EnsureSuccessStatusCode();
        var list = await response.Content.ReadFromJsonAsync<List<BlockchainSnapshotDto>>();
        Assert.NotNull(list);
        Assert.True(list.Count > 0);
    }

    [Fact]
    public async Task GetChainHistory_ReturnsPaged()
    {
        for (var i = 0; i < 5; i++)
            await SeedSnapshot("ltc-main", 2000 + i);

        var response = await _client.GetAsync("/api/chains/ltc-main/history?page=1&pageSize=3");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<BlockchainSnapshotDto>>();
        Assert.NotNull(result);
        Assert.Equal(3, result.Items.Count);
        Assert.Equal(5, result.TotalCount);
    }

    private static int _seedCounter;

    private async Task SeedSnapshot(string chain, long height)
    {
        using var scope = _factory.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<BlockchainDbContext>>();
        await using var context = await factory.CreateDbContextAsync();
        context.Snapshots.Add(new BlockchainSnapshot
        {
            ChainName = chain,
            Height = height,
            Hash = $"seed-{chain}-{height}-{++_seedCounter}",
            Time = DateTimeOffset.UtcNow,
            RawJson = "{}",
            FetchedAt = DateTimeOffset.UtcNow
        });
        await context.SaveChangesAsync();
    }
}

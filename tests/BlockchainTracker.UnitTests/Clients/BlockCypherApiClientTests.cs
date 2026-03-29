using BlockchainTracker.Domain.Helpers;
using BlockchainTracker.Infrastructure.Clients;
using BlockchainTracker.Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace BlockchainTracker.UnitTests.Clients;

public class BlockCypherApiClientTests
{
    private readonly IBlockCypherApi _refitApi = Substitute.For<IBlockCypherApi>();
    private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
    private readonly ILogger<BlockCypherApiClient> _logger = Substitute.For<ILogger<BlockCypherApiClient>>();
    private readonly BlockCypherApiClient _client;

    public BlockCypherApiClientTests()
    {
        _configuration["BlockCypher:Token"].Returns("test-token");
        _client = new BlockCypherApiClient(_refitApi, _configuration, _logger);
    }

    [Theory]
    [InlineData("eth-main", "eth", "main")]
    [InlineData("btc-main", "btc", "main")]
    [InlineData("btc-test3", "btc", "test3")]
    [InlineData("ltc-main", "ltc", "main")]
    [InlineData("dash-main", "dash", "main")]
    public async Task GetChainDataAsync_MapsChainNameCorrectly(string chainName, string expectedCoin, string expectedChain)
    {
        var response = new BlockchainApiResponse { Name = chainName, Height = 100, Hash = "abc" };
        _refitApi.GetChainDataAsync(expectedCoin, expectedChain, "test-token", Arg.Any<CancellationToken>())
            .Returns(response);

        var result = await _client.GetChainDataAsync(chainName, CancellationToken.None);

        Assert.Equal(response, result);
        await _refitApi.Received(1).GetChainDataAsync(expectedCoin, expectedChain, "test-token", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetChainDataAsync_UnsupportedChain_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _client.GetChainDataAsync("unsupported-chain", CancellationToken.None));
    }

    [Fact]
    public void GetSupportedChains_ReturnsFiveChains()
    {
        var chains = BlockchainChainHelper.GetSupportedChains();

        Assert.Equal(5, chains.Count);
        Assert.Contains("eth-main", chains);
        Assert.Contains("btc-main", chains);
        Assert.Contains("btc-test3", chains);
        Assert.Contains("ltc-main", chains);
        Assert.Contains("dash-main", chains);
    }

    [Fact]
    public async Task GetChainDataAsync_WithoutToken_PassesNull()
    {
        var config = Substitute.For<IConfiguration>();
        config["BlockCypher:Token"].Returns((string?)null);
        var client = new BlockCypherApiClient(_refitApi, config, _logger);

        var response = new BlockchainApiResponse { Name = "btc-main", Height = 100, Hash = "abc" };
        _refitApi.GetChainDataAsync("btc", "main", null, Arg.Any<CancellationToken>()).Returns(response);

        await client.GetChainDataAsync("btc-main", CancellationToken.None);

        await _refitApi.Received(1).GetChainDataAsync("btc", "main", null, Arg.Any<CancellationToken>());
    }
}

using BlockchainTracker.Domain.Helpers;

namespace BlockchainTracker.UnitTests.Helpers;

public class BlockchainChainHelperTests
{
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
    public void GetSupportedChains_ReturnsSameInstanceEachCall()
    {
        var first = BlockchainChainHelper.GetSupportedChains();
        var second = BlockchainChainHelper.GetSupportedChains();

        Assert.Same(first, second);
    }

    [Theory]
    [InlineData("eth-main", "eth", "main")]
    [InlineData("btc-main", "btc", "main")]
    [InlineData("btc-test3", "btc", "test3")]
    [InlineData("ltc-main", "ltc", "main")]
    [InlineData("dash-main", "dash", "main")]
    public void GetChainMapping_ReturnsCorrectCoinAndChain(string chainName, string expectedCoin, string expectedChain)
    {
        var (coin, chain) = BlockchainChainHelper.GetChainMapping(chainName);

        Assert.Equal(expectedCoin, coin);
        Assert.Equal(expectedChain, chain);
    }

    [Theory]
    [InlineData("unsupported")]
    [InlineData("")]
    [InlineData("btc-main2")]
    public void GetChainMapping_UnsupportedChain_ThrowsArgumentException(string chainName)
    {
        var ex = Assert.Throws<ArgumentException>(() => BlockchainChainHelper.GetChainMapping(chainName));
        Assert.Contains("Unsupported chain", ex.Message);
    }
}

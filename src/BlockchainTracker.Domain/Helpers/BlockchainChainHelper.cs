namespace BlockchainTracker.Domain.Helpers;

public static class BlockchainChainHelper
{
    private static readonly Dictionary<string, (string Coin, string Chain)> ChainMap = new()
    {
        ["eth-main"] = ("eth", "main"),
        ["btc-main"] = ("btc", "main"),
        ["btc-test3"] = ("btc", "test3"),
        ["ltc-main"] = ("ltc", "main"),
        ["dash-main"] = ("dash", "main")
    };

    public static IReadOnlyList<string> GetSupportedChains() => ChainMap.Keys.ToList();

    public static (string Coin, string Chain) GetChainMapping(string chainName)
    {
        if (!ChainMap.TryGetValue(chainName, out var mapping))
        {
            throw new ArgumentException($"Unsupported chain: {chainName}", nameof(chainName));
        }

        return mapping;
    }
}

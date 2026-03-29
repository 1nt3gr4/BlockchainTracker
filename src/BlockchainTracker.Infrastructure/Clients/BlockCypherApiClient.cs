using BlockchainTracker.Domain.Interfaces;
using BlockchainTracker.Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BlockchainTracker.Infrastructure.Clients;

public class BlockCypherApiClient(
    IBlockCypherApi api,
    IConfiguration configuration,
    ILogger<BlockCypherApiClient> logger) : IBlockchainApiClient
{
    private static readonly Dictionary<string, (string Coin, string Chain)> ChainMap = new()
    {
        ["eth-main"] = ("eth", "main"),
        ["btc-main"] = ("btc", "main"),
        ["btc-test3"] = ("btc", "test3"),
        ["ltc-main"] = ("ltc", "main"),
        ["dash-main"] = ("dash", "main")
    };

    private readonly string? _token = configuration["BlockCypher:Token"];

    public async Task<BlockchainApiResponse> GetChainDataAsync(string chainName, CancellationToken ct = default)
    {
        if (!ChainMap.TryGetValue(chainName, out var mapping))
            throw new ArgumentException($"Unsupported chain: {chainName}", nameof(chainName));

        logger.LogDebug("Fetching chain data for {ChainName} ({Coin}/{Chain})", chainName, mapping.Coin, mapping.Chain);
        return await api.GetChainDataAsync(mapping.Coin, mapping.Chain, _token, ct);
    }

    public IReadOnlyList<string> GetSupportedChains() => ChainMap.Keys.ToList();
}

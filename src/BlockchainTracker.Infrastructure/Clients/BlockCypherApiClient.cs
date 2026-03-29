using BlockchainTracker.Domain.Helpers;
using BlockchainTracker.Domain.Interfaces;
using BlockchainTracker.Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BlockchainTracker.Infrastructure.Clients;

public sealed class BlockCypherApiClient(
    IBlockCypherApi api,
    IConfiguration configuration,
    ILogger<BlockCypherApiClient> logger)
    : IBlockchainApiClient
{
    private readonly string? _token = configuration["BlockCypher:Token"];

    public async Task<BlockchainApiResponse> GetChainDataAsync(string chainName, CancellationToken ct)
    {
        var mapping = BlockchainChainHelper.GetChainMapping(chainName);

        logger.LogDebug("Fetching chain data for {ChainName} ({Coin}/{Chain})", chainName, mapping.Coin, mapping.Chain);

        return await api.GetChainDataAsync(mapping.Coin, mapping.Chain, _token, ct);
    }
}

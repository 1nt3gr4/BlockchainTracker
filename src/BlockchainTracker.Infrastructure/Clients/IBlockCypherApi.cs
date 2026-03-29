using BlockchainTracker.Domain.Models;
using Refit;

namespace BlockchainTracker.Infrastructure.Clients;

public interface IBlockCypherApi
{
    [Get("/v1/{coin}/{chain}")]
    Task<BlockchainApiResponse> GetChainDataAsync(
        string coin,
        string chain,
        [AliasAs("token")] string? token,
        CancellationToken ct);
}

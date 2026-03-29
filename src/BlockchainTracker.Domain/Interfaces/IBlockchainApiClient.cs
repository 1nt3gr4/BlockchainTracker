using BlockchainTracker.Domain.Models;

namespace BlockchainTracker.Domain.Interfaces;

public interface IBlockchainApiClient
{
    Task<BlockchainApiResponse> GetChainDataAsync(string chainName, CancellationToken ct);
}

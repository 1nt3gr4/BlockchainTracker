using BlockchainTracker.Domain.Entities;

namespace BlockchainTracker.Domain.Interfaces;

public interface IBlockchainSnapshotRepository
{
    Task<BlockchainSnapshot?> GetLatestByChainAsync(string chainName, CancellationToken ct = default);
    Task<List<BlockchainSnapshot>> GetLatestPerChainAsync(CancellationToken ct = default);
    Task<(List<BlockchainSnapshot> Items, int TotalCount)> GetHistoryAsync(
        string chainName, int page, int pageSize, CancellationToken ct = default);
}

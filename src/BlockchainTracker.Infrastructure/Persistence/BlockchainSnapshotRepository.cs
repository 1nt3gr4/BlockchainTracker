using BlockchainTracker.Domain.Entities;
using BlockchainTracker.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BlockchainTracker.Infrastructure.Persistence;

public class BlockchainSnapshotRepository(BlockchainDbContext context) : IBlockchainSnapshotRepository
{
    public async Task<BlockchainSnapshot?> GetLatestByChainAsync(string chainName, CancellationToken ct = default)
    {
        return await context.Snapshots
            .AsNoTracking()
            .Where(s => s.ChainName == chainName)
            .OrderByDescending(s => s.FetchedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<BlockchainSnapshot>> GetLatestPerChainAsync(CancellationToken ct = default)
    {
        return await context.Snapshots
            .AsNoTracking()
            .GroupBy(s => s.ChainName)
            .Select(g => g.OrderByDescending(s => s.FetchedAt).First())
            .ToListAsync(ct);
    }

    public async Task<(List<BlockchainSnapshot> Items, int TotalCount)> GetHistoryAsync(
        string chainName, int page, int pageSize, CancellationToken ct = default)
    {
        var query = context.Snapshots
            .AsNoTracking()
            .Where(s => s.ChainName == chainName)
            .OrderByDescending(s => s.FetchedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<bool> ExistsAsync(string chainName, long height, string hash, CancellationToken ct = default)
    {
        return await context.Snapshots
            .AnyAsync(s => s.ChainName == chainName && s.Height == height && s.Hash == hash, ct);
    }

    public async Task AddAsync(BlockchainSnapshot snapshot, CancellationToken ct = default)
    {
        await context.Snapshots.AddAsync(snapshot, ct);
    }
}

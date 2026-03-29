using BlockchainTracker.Domain.Entities;
using BlockchainTracker.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BlockchainTracker.Infrastructure.Persistence;

public class BlockchainSnapshotRepository : IBlockchainSnapshotRepository
{
    private readonly BlockchainDbContext _context;

    public BlockchainSnapshotRepository(BlockchainDbContext context)
    {
        _context = context;
    }

    public async Task<BlockchainSnapshot?> GetLatestByChainAsync(string chainName, CancellationToken ct = default)
    {
        return await _context.Snapshots
            .Where(s => s.ChainName == chainName)
            .OrderByDescending(s => s.FetchedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<BlockchainSnapshot>> GetLatestPerChainAsync(CancellationToken ct = default)
    {
        return await _context.Snapshots
            .GroupBy(s => s.ChainName)
            .Select(g => g.OrderByDescending(s => s.FetchedAt).First())
            .ToListAsync(ct);
    }

    public async Task<(List<BlockchainSnapshot> Items, int TotalCount)> GetHistoryAsync(
        string chainName, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Snapshots
            .Where(s => s.ChainName == chainName)
            .OrderByDescending(s => s.FetchedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task AddAsync(BlockchainSnapshot snapshot, CancellationToken ct = default)
    {
        await _context.Snapshots.AddAsync(snapshot, ct);
    }

    public async Task<bool> ExistsAsync(string chainName, long height, string hash, CancellationToken ct = default)
    {
        return await _context.Snapshots
            .AnyAsync(s => s.ChainName == chainName && s.Height == height && s.Hash == hash, ct);
    }
}

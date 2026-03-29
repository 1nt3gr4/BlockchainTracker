using BlockchainTracker.Domain.Entities;
using BlockchainTracker.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BlockchainTracker.Infrastructure.Persistence;

public class BlockchainSnapshotRepository(IDbContextFactory<BlockchainDbContext> contextFactory) : IBlockchainSnapshotRepository
{
    public async Task<BlockchainSnapshot?> GetLatestByChainAsync(string chainName, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        return await context.Snapshots
            .AsNoTracking()
            .Where(s => s.ChainName == chainName)
            .OrderByDescending(s => s.FetchedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<BlockchainSnapshot>> GetLatestPerChainAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        return await context.Snapshots
            .AsNoTracking()
            .GroupBy(s => s.ChainName)
            .Select(g => g.OrderByDescending(s => s.FetchedAt).First())
            .ToListAsync(ct);
    }

    public async Task<(List<BlockchainSnapshot> Items, int TotalCount)> GetHistoryAsync(
        string chainName, int page, int pageSize, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
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
}

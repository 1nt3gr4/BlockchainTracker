using BlockchainTracker.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BlockchainTracker.Infrastructure.Persistence;

public class UnitOfWork(IDbContextFactory<BlockchainDbContext> contextFactory) : IUnitOfWork
{
    private readonly BlockchainDbContext _context = contextFactory.CreateDbContext();
    private IBlockchainSnapshotRepository? _snapshotRepository;

    public IBlockchainSnapshotRepository SnapshotRepository =>
        _snapshotRepository ??= new BlockchainSnapshotRepository(_context);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return _context.SaveChangesAsync(ct);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}

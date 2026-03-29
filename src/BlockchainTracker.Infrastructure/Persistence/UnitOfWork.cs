using BlockchainTracker.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BlockchainTracker.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly BlockchainDbContext _context;
    private IBlockchainSnapshotRepository? _snapshotRepository;

    public UnitOfWork(IDbContextFactory<BlockchainDbContext> contextFactory)
    {
        _context = contextFactory.CreateDbContext();
    }

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

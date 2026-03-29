using BlockchainTracker.Domain.Interfaces;

namespace BlockchainTracker.Infrastructure.Persistence;

public class UnitOfWork(BlockchainDbContext context, IBlockchainSnapshotRepository repository) : IUnitOfWork
{
    public IBlockchainSnapshotRepository Repository => repository;

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);
}

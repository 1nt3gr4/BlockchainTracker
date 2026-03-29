namespace BlockchainTracker.Domain.Interfaces;

public interface IUnitOfWork
{
    IBlockchainSnapshotRepository Repository { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

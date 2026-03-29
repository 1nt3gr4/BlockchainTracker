namespace BlockchainTracker.Domain.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    IBlockchainSnapshotRepository SnapshotRepository { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

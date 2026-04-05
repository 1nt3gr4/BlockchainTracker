namespace BlockchainTracker.Application.Interfaces;

public interface IBlockchainTrackerMetrics
{
    void RecordSnapshotFetched(string chainName);
    void RecordSnapshotSaved(string chainName);
    void RecordFetchError(string chainName);
    void RecordDuplicateSkipped(string chainName);
    void RecordFetchDuration(string chainName, double durationMs);
    void RecordCircuitBreakerTrip(string chainName);
}

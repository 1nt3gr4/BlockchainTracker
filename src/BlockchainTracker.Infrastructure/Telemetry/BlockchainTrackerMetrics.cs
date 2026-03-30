using System.Diagnostics.Metrics;

namespace BlockchainTracker.Infrastructure.Telemetry;

public sealed class BlockchainTrackerMetrics
{
    private const string MeterName = "BlockchainTracker";

    private readonly Counter<long> _snapshotsFetched;
    private readonly Counter<long> _snapshotsSaved;
    private readonly Counter<long> _fetchErrors;
    private readonly Counter<long> _duplicatesSkipped;
    private readonly Histogram<double> _fetchDuration;
    private readonly Counter<long> _circuitBreakerTrips;

    public BlockchainTrackerMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _snapshotsFetched = meter.CreateCounter<long>("blockchain_tracker.snapshots.fetched", "snapshots", "Total snapshots fetched from API");
        _snapshotsSaved = meter.CreateCounter<long>("blockchain_tracker.snapshots.saved", "snapshots", "Total snapshots saved to database");
        _fetchErrors = meter.CreateCounter<long>("blockchain_tracker.fetch.errors", "errors", "Total fetch errors");
        _duplicatesSkipped = meter.CreateCounter<long>("blockchain_tracker.snapshots.duplicates_skipped", "snapshots", "Total duplicate snapshots skipped");
        _fetchDuration = meter.CreateHistogram<double>("blockchain_tracker.fetch.duration", "ms", "Fetch duration in milliseconds");
        _circuitBreakerTrips = meter.CreateCounter<long>("blockchain_tracker.circuit_breaker.trips", "trips", "Total circuit breaker trips");
    }

    public void RecordSnapshotFetched(string chainName) => _snapshotsFetched.Add(1, new KeyValuePair<string, object?>("chain", chainName));
    public void RecordSnapshotSaved(string chainName) => _snapshotsSaved.Add(1, new KeyValuePair<string, object?>("chain", chainName));
    public void RecordFetchError(string chainName) => _fetchErrors.Add(1, new KeyValuePair<string, object?>("chain", chainName));
    public void RecordDuplicateSkipped(string chainName) => _duplicatesSkipped.Add(1, new KeyValuePair<string, object?>("chain", chainName));

    public void RecordFetchDuration(string chainName, double durationMs) =>
        _fetchDuration.Record(durationMs, new KeyValuePair<string, object?>("chain", chainName));

    public void RecordCircuitBreakerTrip(string chainName) => _circuitBreakerTrips.Add(1, new KeyValuePair<string, object?>("chain", chainName));
}

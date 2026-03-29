namespace BlockchainTracker.Domain.Configuration;

public class CacheSettings
{
    public const string SectionName = "Cache";

    public TimeSpan LatestSnapshotTtl { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan HistoryTtl { get; set; } = TimeSpan.FromMinutes(5);
}

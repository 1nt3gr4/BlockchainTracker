namespace BlockchainTracker.Domain.Configuration;

public class DistributedLockSettings
{
    public const string SectionName = "DistributedLock";

    public string PollingLockKey { get; set; } = "blockchain-tracker:polling-lock";
    public TimeSpan LockTtl { get; set; } = TimeSpan.FromSeconds(60);
}

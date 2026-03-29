namespace BlockchainTracker.Domain.Configuration;

public class PollingSettings
{
    public const string SectionName = "Polling";

    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxDegreeOfParallelism { get; set; } = 3;
}

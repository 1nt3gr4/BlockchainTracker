namespace BlockchainTracker.Domain.Configuration;

public class HealthCheckSettings
{
    public const string SectionName = "HealthCheck";

    public TimeSpan MaxStaleAge { get; set; } = TimeSpan.FromMinutes(5);
}

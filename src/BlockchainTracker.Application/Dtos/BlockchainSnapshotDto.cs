namespace BlockchainTracker.Application.Dtos;

public record BlockchainSnapshotDto
{
    public required string ChainName { get; init; }
    public long Height { get; init; }
    public required string Hash { get; init; }
    public DateTimeOffset Time { get; init; }
    public long PeerCount { get; init; }
    public long UnconfirmedCount { get; init; }
    public long? HighFeePerKb { get; init; }
    public long? MediumFeePerKb { get; init; }
    public long? LowFeePerKb { get; init; }
    public long? HighGasPrice { get; init; }
    public long? MediumGasPrice { get; init; }
    public long? LowGasPrice { get; init; }
    public long LastForkHeight { get; init; }
    public DateTimeOffset FetchedAt { get; init; }
}

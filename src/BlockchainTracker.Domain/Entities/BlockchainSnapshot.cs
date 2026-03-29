namespace BlockchainTracker.Domain.Entities;

public class BlockchainSnapshot
{
    public int Id { get; set; }
    public required string ChainName { get; set; }
    public long Height { get; set; }
    public required string Hash { get; set; }
    public DateTimeOffset Time { get; set; }
    public long PeerCount { get; set; }
    public long UnconfirmedCount { get; set; }
    public long? HighFeePerKb { get; set; }
    public long? MediumFeePerKb { get; set; }
    public long? LowFeePerKb { get; set; }
    public long? HighGasPrice { get; set; }
    public long? MediumGasPrice { get; set; }
    public long? LowGasPrice { get; set; }
    public long LastForkHeight { get; set; }
    public required string RawJson { get; set; }
    public DateTimeOffset FetchedAt { get; set; }
}

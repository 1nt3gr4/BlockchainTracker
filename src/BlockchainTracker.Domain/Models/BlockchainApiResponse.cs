namespace BlockchainTracker.Domain.Models;

public class BlockchainApiResponse
{
    public string Name { get; set; } = string.Empty;
    public long Height { get; set; }
    public string Hash { get; set; } = string.Empty;
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
}

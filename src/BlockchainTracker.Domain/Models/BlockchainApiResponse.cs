using System.Text.Json.Serialization;

namespace BlockchainTracker.Domain.Models;

public class BlockchainApiResponse
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("height")]
    public long Height { get; set; }

    [JsonPropertyName("hash")]
    public string Hash { get; set; } = string.Empty;

    [JsonPropertyName("time")]
    public DateTimeOffset Time { get; set; }

    [JsonPropertyName("peer_count")]
    public long PeerCount { get; set; }

    [JsonPropertyName("unconfirmed_count")]
    public long UnconfirmedCount { get; set; }

    [JsonPropertyName("high_fee_per_kb")]
    public long? HighFeePerKb { get; set; }

    [JsonPropertyName("medium_fee_per_kb")]
    public long? MediumFeePerKb { get; set; }

    [JsonPropertyName("low_fee_per_kb")]
    public long? LowFeePerKb { get; set; }

    [JsonPropertyName("high_gas_price")]
    public long? HighGasPrice { get; set; }

    [JsonPropertyName("medium_gas_price")]
    public long? MediumGasPrice { get; set; }

    [JsonPropertyName("low_gas_price")]
    public long? LowGasPrice { get; set; }

    [JsonPropertyName("last_fork_height")]
    public long LastForkHeight { get; set; }
}

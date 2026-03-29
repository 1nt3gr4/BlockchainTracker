namespace BlockchainTracker.Domain.Entities;

public class BlockchainSnapshot
{
    public int Id { get; set; }
    public required string ChainName { get; set; }
    public long Height { get; set; }
    public required string Hash { get; set; }
    public DateTimeOffset Time { get; set; }
    public required string RawJson { get; set; }
    public DateTimeOffset FetchedAt { get; set; }
}

namespace BlockchainTracker.Application.Dtos;

public record BlockchainSnapshotDto
{
    public required string ChainName { get; init; }
    public long Height { get; init; }
    public required string Hash { get; init; }
    public DateTimeOffset Time { get; init; }
    public required string RawJson { get; init; }
    public DateTimeOffset FetchedAt { get; init; }
}

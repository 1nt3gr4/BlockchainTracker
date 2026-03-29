using BlockchainTracker.Application.Mapping;
using BlockchainTracker.Domain.Entities;

namespace BlockchainTracker.UnitTests.Mapping;

public class BlockchainSnapshotMapperTests
{
    [Fact]
    public void MapToDto_MapsAllProperties()
    {
        var snapshot = new BlockchainSnapshot
        {
            Id = 1,
            ChainName = "eth-main",
            Height = 18000000,
            Hash = "0xabc123",
            Time = new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.Zero),
            RawJson = "{\"name\":\"ETH.main\"}",
            FetchedAt = new DateTimeOffset(2024, 1, 15, 12, 0, 30, TimeSpan.Zero)
        };

        var dto = BlockchainSnapshotMapper.MapToDto(snapshot);

        Assert.Equal(snapshot.ChainName, dto.ChainName);
        Assert.Equal(snapshot.Height, dto.Height);
        Assert.Equal(snapshot.Hash, dto.Hash);
        Assert.Equal(snapshot.Time, dto.Time);
        Assert.Equal(snapshot.RawJson, dto.RawJson);
        Assert.Equal(snapshot.FetchedAt, dto.FetchedAt);
    }
}

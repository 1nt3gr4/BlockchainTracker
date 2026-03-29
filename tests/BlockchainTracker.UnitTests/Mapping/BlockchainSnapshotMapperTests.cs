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
            Time = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc),
            PeerCount = 100,
            UnconfirmedCount = 500,
            HighGasPrice = 30000000000,
            MediumGasPrice = 20000000000,
            LowGasPrice = 10000000000,
            LastForkHeight = 17999990,
            RawJson = "{}",
            FetchedAt = new DateTime(2024, 1, 15, 12, 0, 30, DateTimeKind.Utc)
        };

        var dto = BlockchainSnapshotMapper.MapToDto(snapshot);

        Assert.Equal(snapshot.ChainName, dto.ChainName);
        Assert.Equal(snapshot.Height, dto.Height);
        Assert.Equal(snapshot.Hash, dto.Hash);
        Assert.Equal(snapshot.Time, dto.Time);
        Assert.Equal(snapshot.PeerCount, dto.PeerCount);
        Assert.Equal(snapshot.UnconfirmedCount, dto.UnconfirmedCount);
        Assert.Equal(snapshot.HighGasPrice, dto.HighGasPrice);
        Assert.Equal(snapshot.MediumGasPrice, dto.MediumGasPrice);
        Assert.Equal(snapshot.LowGasPrice, dto.LowGasPrice);
        Assert.Equal(snapshot.LastForkHeight, dto.LastForkHeight);
        Assert.Equal(snapshot.FetchedAt, dto.FetchedAt);
    }

    [Fact]
    public void MapToDto_NullableFeeFields_MappedCorrectly()
    {
        var snapshot = new BlockchainSnapshot
        {
            ChainName = "btc-main",
            Height = 800000,
            Hash = "000abc",
            Time = DateTime.UtcNow,
            HighFeePerKb = 50000,
            MediumFeePerKb = 25000,
            LowFeePerKb = 10000,
            RawJson = "{}",
            FetchedAt = DateTime.UtcNow
        };

        var dto = BlockchainSnapshotMapper.MapToDto(snapshot);

        Assert.Equal(50000, dto.HighFeePerKb);
        Assert.Null(dto.HighGasPrice);
    }
}

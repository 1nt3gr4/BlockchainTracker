using BlockchainTracker.Application.Dtos;
using BlockchainTracker.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace BlockchainTracker.Application.Mapping;

[Mapper]
public static partial class BlockchainSnapshotMapper
{
    [MapperIgnoreSource(nameof(BlockchainSnapshot.Id))]
    public static partial BlockchainSnapshotDto MapToDto(BlockchainSnapshot snapshot);
}

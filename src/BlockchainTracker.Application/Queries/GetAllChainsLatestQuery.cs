using BlockchainTracker.Application.Dtos;
using BlockchainTracker.Application.Interfaces;
using BlockchainTracker.Application.Mapping;
using BlockchainTracker.Domain.Configuration;
using BlockchainTracker.Domain.Interfaces;
using Mediator;
using Microsoft.Extensions.Options;

namespace BlockchainTracker.Application.Queries;

public record GetAllChainsLatestQuery : IQuery<IReadOnlyList<BlockchainSnapshotDto>>;

public sealed class GetAllChainsLatestQueryHandler(
    IBlockchainSnapshotRepository repository,
    ICacheService cache,
    IOptions<CacheSettings> cacheSettings)
    : IQueryHandler<GetAllChainsLatestQuery, IReadOnlyList<BlockchainSnapshotDto>>
{
    public async ValueTask<IReadOnlyList<BlockchainSnapshotDto>> Handle(GetAllChainsLatestQuery query, CancellationToken ct)
    {
        var cacheKey = CacheKeys.AllChainsLatest;
        var cached = await cache.GetAsync<IReadOnlyList<BlockchainSnapshotDto>>(cacheKey, ct);

        if (cached is not null)
        {
            return cached;
        }

        var snapshots = await repository.GetLatestPerChainAsync(ct);
        var dtos = snapshots.Select(BlockchainSnapshotMapper.MapToDto).ToList();
        await cache.SetAsync<IReadOnlyList<BlockchainSnapshotDto>>(cacheKey, dtos, cacheSettings.Value.LatestSnapshotTtl, ct);

        return dtos;
    }
}

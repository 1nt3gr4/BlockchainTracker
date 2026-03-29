using BlockchainTracker.Application.Dtos;
using BlockchainTracker.Application.Interfaces;
using BlockchainTracker.Application.Mapping;
using BlockchainTracker.Domain.Configuration;
using BlockchainTracker.Domain.Interfaces;
using Mediator;
using Microsoft.Extensions.Options;

namespace BlockchainTracker.Application.Queries;

public record GetChainLatestQuery(string ChainName) : IQuery<BlockchainSnapshotDto?>;

public sealed class GetChainLatestQueryHandler(
    IBlockchainSnapshotRepository repository,
    ICacheService cache,
    IOptions<CacheSettings> cacheSettings)
    : IQueryHandler<GetChainLatestQuery, BlockchainSnapshotDto?>
{
    public async ValueTask<BlockchainSnapshotDto?> Handle(GetChainLatestQuery query, CancellationToken ct)
    {
        var cacheKey = CacheKeys.ChainLatest(query.ChainName);
        var cached = await cache.GetAsync<BlockchainSnapshotDto>(cacheKey, ct);

        if (cached is not null)
        {
            return cached;
        }

        var snapshot = await repository.GetLatestByChainAsync(query.ChainName, ct);

        if (snapshot is null)
        {
            return null;
        }

        var dto = BlockchainSnapshotMapper.MapToDto(snapshot);
        await cache.SetAsync(cacheKey, dto, cacheSettings.Value.LatestSnapshotTtl, ct);

        return dto;
    }
}

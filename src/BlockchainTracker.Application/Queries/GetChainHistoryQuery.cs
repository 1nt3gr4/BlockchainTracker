using BlockchainTracker.Application.Dtos;
using BlockchainTracker.Application.Interfaces;
using BlockchainTracker.Application.Mapping;
using BlockchainTracker.Domain.Configuration;
using BlockchainTracker.Domain.Interfaces;
using Mediator;
using Microsoft.Extensions.Options;

namespace BlockchainTracker.Application.Queries;

public record GetChainHistoryQuery(string ChainName, int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<BlockchainSnapshotDto>>;

public sealed class GetChainHistoryQueryHandler(
    IBlockchainSnapshotRepository repository,
    ICacheService cache,
    IOptions<CacheSettings> cacheSettings)
    : IQueryHandler<GetChainHistoryQuery, PagedResult<BlockchainSnapshotDto>>
{
    public async ValueTask<PagedResult<BlockchainSnapshotDto>> Handle(GetChainHistoryQuery query, CancellationToken ct)
    {
        var version = await cache.GetAsync<long>(CacheKeys.ChainHistoryVersion(query.ChainName), ct);
        var cacheKey = CacheKeys.ChainHistory(query.ChainName, query.Page, query.PageSize, version);
        var cached = await cache.GetAsync<PagedResult<BlockchainSnapshotDto>>(cacheKey, ct);

        if (cached is not null)
        {
            return cached;
        }

        var (items, totalCount) = await repository.GetHistoryAsync(query.ChainName, query.Page, query.PageSize, ct);

        var result = new PagedResult<BlockchainSnapshotDto>
        {
            Items = items.Select(BlockchainSnapshotMapper.MapToDto).ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };

        await cache.SetAsync(cacheKey, result, cacheSettings.Value.HistoryTtl, ct);
        return result;
    }
}

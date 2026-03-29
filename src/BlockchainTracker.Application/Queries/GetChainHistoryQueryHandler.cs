using BlockchainTracker.Application.Dtos;
using BlockchainTracker.Application.Interfaces;
using BlockchainTracker.Application.Mapping;
using BlockchainTracker.Domain.Configuration;
using BlockchainTracker.Domain.Interfaces;
using Mediator;
using Microsoft.Extensions.Options;

namespace BlockchainTracker.Application.Queries;

public sealed class GetChainHistoryQueryHandler : IQueryHandler<GetChainHistoryQuery, PagedResult<BlockchainSnapshotDto>>
{
    private readonly IBlockchainSnapshotRepository _repository;
    private readonly ICacheService _cache;
    private readonly IOptions<CacheSettings> _cacheSettings;

    public GetChainHistoryQueryHandler(
        IBlockchainSnapshotRepository repository,
        ICacheService cache,
        IOptions<CacheSettings> cacheSettings)
    {
        _repository = repository;
        _cache = cache;
        _cacheSettings = cacheSettings;
    }

    public async ValueTask<PagedResult<BlockchainSnapshotDto>> Handle(GetChainHistoryQuery query, CancellationToken ct)
    {
        var cacheKey = $"chain:history:{query.ChainName}:{query.Page}:{query.PageSize}";
        var cached = await _cache.GetAsync<PagedResult<BlockchainSnapshotDto>>(cacheKey, ct);
        if (cached is not null) return cached;

        var (items, totalCount) = await _repository.GetHistoryAsync(query.ChainName, query.Page, query.PageSize, ct);

        var result = new PagedResult<BlockchainSnapshotDto>
        {
            Items = items.Select(BlockchainSnapshotMapper.MapToDto).ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };

        await _cache.SetAsync(cacheKey, result, _cacheSettings.Value.HistoryTtl, ct);
        return result;
    }
}

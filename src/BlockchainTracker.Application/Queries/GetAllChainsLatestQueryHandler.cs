using BlockchainTracker.Application.Dtos;
using BlockchainTracker.Application.Interfaces;
using BlockchainTracker.Application.Mapping;
using BlockchainTracker.Domain.Configuration;
using BlockchainTracker.Domain.Interfaces;
using Mediator;
using Microsoft.Extensions.Options;

namespace BlockchainTracker.Application.Queries;

public sealed class GetAllChainsLatestQueryHandler : IQueryHandler<GetAllChainsLatestQuery, IReadOnlyList<BlockchainSnapshotDto>>
{
    private readonly IBlockchainSnapshotRepository _repository;
    private readonly ICacheService _cache;
    private readonly IOptions<CacheSettings> _cacheSettings;

    public GetAllChainsLatestQueryHandler(
        IBlockchainSnapshotRepository repository,
        ICacheService cache,
        IOptions<CacheSettings> cacheSettings)
    {
        _repository = repository;
        _cache = cache;
        _cacheSettings = cacheSettings;
    }

    public async ValueTask<IReadOnlyList<BlockchainSnapshotDto>> Handle(GetAllChainsLatestQuery query, CancellationToken ct)
    {
        const string cacheKey = "chains:latest:all";
        var cached = await _cache.GetAsync<IReadOnlyList<BlockchainSnapshotDto>>(cacheKey, ct);
        if (cached is not null) return cached;

        var snapshots = await _repository.GetLatestPerChainAsync(ct);
        var dtos = snapshots.Select(BlockchainSnapshotMapper.MapToDto).ToList();
        await _cache.SetAsync(cacheKey, (IReadOnlyList<BlockchainSnapshotDto>)dtos, _cacheSettings.Value.LatestSnapshotTtl, ct);
        return dtos;
    }
}

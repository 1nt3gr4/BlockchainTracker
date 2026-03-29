using BlockchainTracker.Application.Dtos;
using BlockchainTracker.Application.Interfaces;
using BlockchainTracker.Application.Mapping;
using BlockchainTracker.Domain.Configuration;
using BlockchainTracker.Domain.Interfaces;
using Mediator;
using Microsoft.Extensions.Options;

namespace BlockchainTracker.Application.Queries;

public sealed class GetChainLatestQueryHandler : IQueryHandler<GetChainLatestQuery, BlockchainSnapshotDto?>
{
    private readonly IBlockchainSnapshotRepository _repository;
    private readonly ICacheService _cache;
    private readonly IOptions<CacheSettings> _cacheSettings;

    public GetChainLatestQueryHandler(
        IBlockchainSnapshotRepository repository,
        ICacheService cache,
        IOptions<CacheSettings> cacheSettings)
    {
        _repository = repository;
        _cache = cache;
        _cacheSettings = cacheSettings;
    }

    public async ValueTask<BlockchainSnapshotDto?> Handle(GetChainLatestQuery query, CancellationToken ct)
    {
        var cacheKey = $"chain:latest:{query.ChainName}";
        var cached = await _cache.GetAsync<BlockchainSnapshotDto>(cacheKey, ct);
        if (cached is not null) return cached;

        var snapshot = await _repository.GetLatestByChainAsync(query.ChainName, ct);
        if (snapshot is null) return null;

        var dto = BlockchainSnapshotMapper.MapToDto(snapshot);
        await _cache.SetAsync(cacheKey, dto, _cacheSettings.Value.LatestSnapshotTtl, ct);
        return dto;
    }
}

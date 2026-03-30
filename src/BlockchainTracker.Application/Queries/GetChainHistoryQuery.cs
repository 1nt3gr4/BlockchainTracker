using BlockchainTracker.Application.Dtos;
using BlockchainTracker.Application.Mapping;
using BlockchainTracker.Domain.Interfaces;
using Mediator;

namespace BlockchainTracker.Application.Queries;

public record GetChainHistoryQuery(string ChainName, int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<BlockchainSnapshotDto>>;

public sealed class GetChainHistoryQueryHandler(
    IBlockchainSnapshotRepository repository)
    : IQueryHandler<GetChainHistoryQuery, PagedResult<BlockchainSnapshotDto>>
{
    public async ValueTask<PagedResult<BlockchainSnapshotDto>> Handle(GetChainHistoryQuery query, CancellationToken ct)
    {
        var (items, totalCount) = await repository.GetHistoryAsync(query.ChainName, query.Page, query.PageSize, ct);

        return new PagedResult<BlockchainSnapshotDto>
        {
            Items = items.Select(BlockchainSnapshotMapper.MapToDto).ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}

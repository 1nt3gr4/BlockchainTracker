using BlockchainTracker.Application.Dtos;
using Mediator;

namespace BlockchainTracker.Application.Queries;

public record GetChainHistoryQuery(string ChainName, int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<BlockchainSnapshotDto>>;

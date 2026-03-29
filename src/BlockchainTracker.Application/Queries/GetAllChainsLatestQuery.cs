using BlockchainTracker.Application.Dtos;
using Mediator;

namespace BlockchainTracker.Application.Queries;

public record GetAllChainsLatestQuery : IQuery<IReadOnlyList<BlockchainSnapshotDto>>;

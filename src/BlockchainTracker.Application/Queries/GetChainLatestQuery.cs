using BlockchainTracker.Application.Dtos;
using Mediator;

namespace BlockchainTracker.Application.Queries;

public record GetChainLatestQuery(string ChainName) : IQuery<BlockchainSnapshotDto?>;

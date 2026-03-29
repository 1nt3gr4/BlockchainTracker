using Mediator;

namespace BlockchainTracker.Application.Queries;

public record GetTrackedChainsQuery : IQuery<IReadOnlyList<string>>;

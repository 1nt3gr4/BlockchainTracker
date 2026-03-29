using BlockchainTracker.Domain.Interfaces;
using Mediator;

namespace BlockchainTracker.Application.Queries;

public record GetTrackedChainsQuery : IQuery<IReadOnlyList<string>>;

public sealed class GetTrackedChainsQueryHandler(
    IBlockchainApiClient apiClient) : IQueryHandler<GetTrackedChainsQuery, IReadOnlyList<string>>
{
    public ValueTask<IReadOnlyList<string>> Handle(GetTrackedChainsQuery query, CancellationToken ct)
    {
        return ValueTask.FromResult(apiClient.GetSupportedChains());
    }
}

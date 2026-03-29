using BlockchainTracker.Domain.Interfaces;
using Mediator;

namespace BlockchainTracker.Application.Queries;

public sealed class GetTrackedChainsQueryHandler : IQueryHandler<GetTrackedChainsQuery, IReadOnlyList<string>>
{
    private readonly IBlockchainApiClient _apiClient;

    public GetTrackedChainsQueryHandler(IBlockchainApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public ValueTask<IReadOnlyList<string>> Handle(GetTrackedChainsQuery query, CancellationToken ct)
    {
        return ValueTask.FromResult(_apiClient.GetSupportedChains());
    }
}

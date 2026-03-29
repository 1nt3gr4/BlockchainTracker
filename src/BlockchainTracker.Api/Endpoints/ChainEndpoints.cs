using BlockchainTracker.Application.Dtos;
using BlockchainTracker.Application.Queries;
using BlockchainTracker.Infrastructure.Telemetry;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;

namespace BlockchainTracker.Api.Endpoints;

public static class ChainEndpoints
{
    public static RouteGroupBuilder MapChainEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/chains")
            .WithTags("Chains");

        group.MapGet("/", GetAllChainsLatest)
            .WithName("GetAllChainsLatest")
            .Produces<IReadOnlyList<BlockchainSnapshotDto>>();

        group.MapGet("/tracked", GetTrackedChains)
            .WithName("GetTrackedChains")
            .Produces<IReadOnlyList<string>>();

        group.MapGet("/{chainName}/latest", GetChainLatest)
            .WithName("GetChainLatest")
            .Produces<BlockchainSnapshotDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{chainName}/history", GetChainHistory)
            .WithName("GetChainHistory")
            .Produces<PagedResult<BlockchainSnapshotDto>>()
            .ProducesValidationProblem();

        return group;
    }

    private static async Task<Ok<IReadOnlyList<BlockchainSnapshotDto>>> GetAllChainsLatest(
        IMediator mediator, BlockchainTrackerMetrics metrics, CancellationToken ct)
    {
        metrics.RecordApiRequest("GetAllChainsLatest");

        var result = await mediator.Send(new GetAllChainsLatestQuery(), ct);
        return TypedResults.Ok(result);
    }

    private static async Task<Ok<IReadOnlyList<string>>> GetTrackedChains(
        IMediator mediator, BlockchainTrackerMetrics metrics, CancellationToken ct)
    {
        metrics.RecordApiRequest("GetTrackedChains");

        var result = await mediator.Send(new GetTrackedChainsQuery(), ct);
        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<BlockchainSnapshotDto>, NotFound>> GetChainLatest(
        string chainName, IMediator mediator, BlockchainTrackerMetrics metrics, CancellationToken ct)
    {
        metrics.RecordApiRequest("GetChainLatest");

        var result = await mediator.Send(new GetChainLatestQuery(chainName), ct);
        return result is not null
            ? TypedResults.Ok(result)
            : TypedResults.NotFound();
    }

    private static async Task<Ok<PagedResult<BlockchainSnapshotDto>>> GetChainHistory(
        string chainName, int page, int pageSize, IMediator mediator, BlockchainTrackerMetrics metrics, CancellationToken ct)
    {
        metrics.RecordApiRequest("GetChainHistory");

        var clampedPage = Math.Max(1, page);
        var clampedSize = Math.Clamp(pageSize, 1, 100);

        var result = await mediator.Send(new GetChainHistoryQuery(chainName, clampedPage, clampedSize), ct);
        return TypedResults.Ok(result);
    }
}

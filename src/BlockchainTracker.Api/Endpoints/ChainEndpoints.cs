using BlockchainTracker.Application.Dtos;
using BlockchainTracker.Application.Queries;
using FluentValidation;
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
        IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetAllChainsLatestQuery(), ct);
        return TypedResults.Ok(result);
    }

    private static async Task<Ok<IReadOnlyList<string>>> GetTrackedChains(
        IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetTrackedChainsQuery(), ct);
        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<BlockchainSnapshotDto>, NotFound>> GetChainLatest(
        string chainName, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetChainLatestQuery(chainName), ct);
        return result is not null
            ? TypedResults.Ok(result)
            : TypedResults.NotFound();
    }

    private static async Task<Results<Ok<PagedResult<BlockchainSnapshotDto>>, ValidationProblem>> GetChainHistory(
        string chainName, int page, int pageSize,
        IValidator<GetChainHistoryQuery> validator, IMediator mediator, CancellationToken ct)
    {
        var query = new GetChainHistoryQuery(chainName, page, pageSize);
        var validation = await validator.ValidateAsync(query, ct);

        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.ToDictionary());
        }

        var result = await mediator.Send(query, ct);
        return TypedResults.Ok(result);
    }
}

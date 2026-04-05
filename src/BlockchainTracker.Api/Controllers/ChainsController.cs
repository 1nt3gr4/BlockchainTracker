using BlockchainTracker.Application.Dtos;
using BlockchainTracker.Application.Queries;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace BlockchainTracker.Api.Controllers;

[ApiController]
[Route("api/chains")]
public class ChainsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<BlockchainSnapshotDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllChainsLatest(CancellationToken ct)
    {
        var result = await mediator.Send(new GetAllChainsLatestQuery(), ct);
        return Ok(result);
    }

    [HttpGet("tracked")]
    [ProducesResponseType<IReadOnlyList<string>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTrackedChains(CancellationToken ct)
    {
        var result = await mediator.Send(new GetTrackedChainsQuery(), ct);
        return Ok(result);
    }

    [HttpGet("{chainName}/latest")]
    [ProducesResponseType<BlockchainSnapshotDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChainLatest(string chainName, CancellationToken ct)
    {
        var result = await mediator.Send(new GetChainLatestQuery(chainName), ct);
        return result is not null ? Ok(result) : NotFound();
    }

    [HttpGet("{chainName}/history")]
    [ProducesResponseType<PagedResult<BlockchainSnapshotDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetChainHistory(
        string chainName, int page, int pageSize, CancellationToken ct)
    {
        var result = await mediator.Send(new GetChainHistoryQuery(chainName, page, pageSize), ct);
        return Ok(result);
    }
}

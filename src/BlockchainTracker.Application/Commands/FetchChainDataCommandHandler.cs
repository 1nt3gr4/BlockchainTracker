using System.Text.Json;
using BlockchainTracker.Domain.Entities;
using BlockchainTracker.Domain.Interfaces;
using Mediator;

namespace BlockchainTracker.Application.Commands;

public sealed class FetchChainDataCommandHandler : ICommandHandler<FetchChainDataCommand, bool>
{
    private readonly IBlockchainApiClient _apiClient;
    private readonly IUnitOfWork _unitOfWork;

    public FetchChainDataCommandHandler(IBlockchainApiClient apiClient, IUnitOfWork unitOfWork)
    {
        _apiClient = apiClient;
        _unitOfWork = unitOfWork;
    }

    public async ValueTask<bool> Handle(FetchChainDataCommand command, CancellationToken ct)
    {
        var response = await _apiClient.GetChainDataAsync(command.ChainName, ct);

        if (await _unitOfWork.SnapshotRepository.ExistsAsync(command.ChainName, response.Height, response.Hash, ct))
            return false;

        var snapshot = new BlockchainSnapshot
        {
            ChainName = command.ChainName,
            Height = response.Height,
            Hash = response.Hash,
            Time = response.Time,
            PeerCount = response.PeerCount,
            UnconfirmedCount = response.UnconfirmedCount,
            HighFeePerKb = response.HighFeePerKb,
            MediumFeePerKb = response.MediumFeePerKb,
            LowFeePerKb = response.LowFeePerKb,
            HighGasPrice = response.HighGasPrice,
            MediumGasPrice = response.MediumGasPrice,
            LowGasPrice = response.LowGasPrice,
            LastForkHeight = response.LastForkHeight,
            RawJson = JsonSerializer.Serialize(response),
            FetchedAt = DateTime.UtcNow
        };

        await _unitOfWork.SnapshotRepository.AddAsync(snapshot, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return true;
    }
}

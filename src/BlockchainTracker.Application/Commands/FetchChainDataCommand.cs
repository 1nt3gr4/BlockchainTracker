using System.Text.Json;
using BlockchainTracker.Domain.Entities;
using BlockchainTracker.Domain.Interfaces;
using Mediator;

namespace BlockchainTracker.Application.Commands;

public record FetchChainDataCommand(string ChainName) : ICommand<bool>;

public sealed class FetchChainDataCommandHandler(
    IBlockchainApiClient apiClient,
    IUnitOfWork unitOfWork) : ICommandHandler<FetchChainDataCommand, bool>
{
    public async ValueTask<bool> Handle(FetchChainDataCommand command, CancellationToken ct)
    {
        var response = await apiClient.GetChainDataAsync(command.ChainName, ct);

        if (await unitOfWork.SnapshotRepository.ExistsAsync(command.ChainName, response.Height, response.Hash, ct))
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
            FetchedAt = DateTimeOffset.UtcNow
        };

        await unitOfWork.SnapshotRepository.AddAsync(snapshot, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return true;
    }
}

namespace BlockchainTracker.Application.Interfaces;

public interface IBlockchainDataFetcherService
{
    Task<bool> FetchAndSaveAsync(string chainName, CancellationToken ct = default);
}

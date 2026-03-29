namespace BlockchainTracker.Application;

public static class CacheKeys
{
    public const string AllChainsLatest = "chains:latest:all";

    public static string ChainLatest(string chainName) => $"chain:latest:{chainName}";

    public static string ChainHistory(string chainName, int page, int pageSize) =>
        $"chain:history:{chainName}:{page}:{pageSize}";
}

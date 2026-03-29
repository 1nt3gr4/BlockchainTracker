namespace BlockchainTracker.Application;

public static class CacheKeys
{
    public const string AllChainsLatest = "chains:latest:all";

    public static string ChainLatest(string chainName) => $"chain:latest:{chainName}";

    public static string ChainHistoryVersion(string chainName) => $"chain:history:version:{chainName}";

    public static string ChainHistory(string chainName, int page, int pageSize, long version = 0) =>
        $"chain:history:{chainName}:v{version}:{page}:{pageSize}";
}

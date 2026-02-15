using Azure.Identity;

namespace IntuneManager.Core.Models;

public static class CloudEndpoints
{
    public static (string GraphEndpoint, Uri AuthorityHost) GetEndpoints(CloudEnvironment cloud)
    {
        return cloud switch
        {
            CloudEnvironment.Commercial => ("https://graph.microsoft.com", AzureAuthorityHosts.AzurePublicCloud),
            CloudEnvironment.GCC => ("https://graph.microsoft.com", AzureAuthorityHosts.AzurePublicCloud),
            CloudEnvironment.GCCHigh => ("https://graph.microsoft.us", AzureAuthorityHosts.AzureGovernment),
            CloudEnvironment.DoD => ("https://dod-graph.microsoft.us", AzureAuthorityHosts.AzureGovernment),
            _ => throw new ArgumentOutOfRangeException(nameof(cloud), cloud, "Unsupported cloud environment")
        };
    }

    public static string[] GetScopes(CloudEnvironment cloud)
    {
        var (graphEndpoint, _) = GetEndpoints(cloud);
        return [$"{graphEndpoint}/.default"];
    }
}

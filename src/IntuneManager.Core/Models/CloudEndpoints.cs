using Azure.Identity;

namespace IntuneManager.Core.Models;

public static class CloudEndpoints
{
    public static (string GraphBaseUrl, Uri AuthorityHost) GetEndpoints(CloudEnvironment cloud)
    {
        return cloud switch
        {
            CloudEnvironment.Commercial => ("https://graph.microsoft.com/beta", AzureAuthorityHosts.AzurePublicCloud),
            CloudEnvironment.GCC => ("https://graph.microsoft.com/beta", AzureAuthorityHosts.AzurePublicCloud),
            CloudEnvironment.GCCHigh => ("https://graph.microsoft.us/beta", AzureAuthorityHosts.AzureGovernment),
            CloudEnvironment.DoD => ("https://dod-graph.microsoft.us/beta", AzureAuthorityHosts.AzureGovernment),
            _ => throw new ArgumentOutOfRangeException(nameof(cloud), cloud, "Unsupported cloud environment")
        };
    }

    public static string[] GetScopes(CloudEnvironment cloud)
    {
        var (graphBaseUrl, _) = GetEndpoints(cloud);
        // Scopes use the root host, not the versioned URL
        var rootUrl = graphBaseUrl[..graphBaseUrl.IndexOf("/beta", StringComparison.Ordinal)];
        return [$"{rootUrl}/.default"];
    }
}

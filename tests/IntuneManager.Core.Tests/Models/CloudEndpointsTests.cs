using Azure.Identity;
using IntuneManager.Core.Models;

namespace IntuneManager.Core.Tests.Models;

public class CloudEndpointsTests
{
    [Theory]
    [InlineData(CloudEnvironment.Commercial, "https://graph.microsoft.com")]
    [InlineData(CloudEnvironment.GCC, "https://graph.microsoft.com")]
    [InlineData(CloudEnvironment.GCCHigh, "https://graph.microsoft.us")]
    [InlineData(CloudEnvironment.DoD, "https://dod-graph.microsoft.us")]
    public void GetEndpoints_ReturnsCorrectGraphEndpoint(CloudEnvironment cloud, string expectedEndpoint)
    {
        var (graphEndpoint, _) = CloudEndpoints.GetEndpoints(cloud);
        Assert.Equal(expectedEndpoint, graphEndpoint);
    }

    [Theory]
    [InlineData(CloudEnvironment.Commercial)]
    [InlineData(CloudEnvironment.GCC)]
    public void GetEndpoints_PublicCloud_ReturnsPublicAuthorityHost(CloudEnvironment cloud)
    {
        var (_, authorityHost) = CloudEndpoints.GetEndpoints(cloud);
        Assert.Equal(AzureAuthorityHosts.AzurePublicCloud, authorityHost);
    }

    [Theory]
    [InlineData(CloudEnvironment.GCCHigh)]
    [InlineData(CloudEnvironment.DoD)]
    public void GetEndpoints_GovernmentCloud_ReturnsGovernmentAuthorityHost(CloudEnvironment cloud)
    {
        var (_, authorityHost) = CloudEndpoints.GetEndpoints(cloud);
        Assert.Equal(AzureAuthorityHosts.AzureGovernment, authorityHost);
    }

    [Theory]
    [InlineData(CloudEnvironment.Commercial, "https://graph.microsoft.com/.default")]
    [InlineData(CloudEnvironment.GCCHigh, "https://graph.microsoft.us/.default")]
    [InlineData(CloudEnvironment.DoD, "https://dod-graph.microsoft.us/.default")]
    public void GetScopes_ReturnsCorrectScope(CloudEnvironment cloud, string expectedScope)
    {
        var scopes = CloudEndpoints.GetScopes(cloud);
        Assert.Single(scopes);
        Assert.Equal(expectedScope, scopes[0]);
    }
}

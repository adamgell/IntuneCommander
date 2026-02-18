using Azure.Identity;
using IntuneManager.Core.Models;

namespace IntuneManager.Core.Tests.Models;

public class CloudEndpointsTests
{
    [Fact]
    public void GetEndpoints_Commercial_ReturnsCorrectGraphEndpoint()
    {
        var (graphBaseUrl, _) = CloudEndpoints.GetEndpoints(CloudEnvironment.Commercial);
        Assert.Equal("https://graph.microsoft.com/beta", graphBaseUrl);
    }

    [Fact]
    public void GetEndpoints_GCC_ReturnsCorrectGraphEndpoint()
    {
        var (graphBaseUrl, _) = CloudEndpoints.GetEndpoints(CloudEnvironment.GCC);
        Assert.Equal("https://graph.microsoft.com/beta", graphBaseUrl);
    }

    [Fact]
    public void GetEndpoints_GCCHigh_ReturnsCorrectGraphEndpoint()
    {
        var (graphBaseUrl, _) = CloudEndpoints.GetEndpoints(CloudEnvironment.GCCHigh);
        Assert.Equal("https://graph.microsoft.us/beta", graphBaseUrl);
    }

    [Fact]
    public void GetEndpoints_DoD_ReturnsCorrectGraphEndpoint()
    {
        var (graphBaseUrl, _) = CloudEndpoints.GetEndpoints(CloudEnvironment.DoD);
        Assert.Equal("https://dod-graph.microsoft.us/beta", graphBaseUrl);
    }

    [Fact]
    public void GetEndpoints_Commercial_ReturnsPublicAuthorityHost()
    {
        var (_, authorityHost) = CloudEndpoints.GetEndpoints(CloudEnvironment.Commercial);
        Assert.Equal(AzureAuthorityHosts.AzurePublicCloud, authorityHost);
    }

    [Fact]
    public void GetEndpoints_GCC_ReturnsPublicAuthorityHost()
    {
        var (_, authorityHost) = CloudEndpoints.GetEndpoints(CloudEnvironment.GCC);
        Assert.Equal(AzureAuthorityHosts.AzurePublicCloud, authorityHost);
    }

    [Fact]
    public void GetEndpoints_GCCHigh_ReturnsGovernmentAuthorityHost()
    {
        var (_, authorityHost) = CloudEndpoints.GetEndpoints(CloudEnvironment.GCCHigh);
        Assert.Equal(AzureAuthorityHosts.AzureGovernment, authorityHost);
    }

    [Fact]
    public void GetEndpoints_DoD_ReturnsGovernmentAuthorityHost()
    {
        var (_, authorityHost) = CloudEndpoints.GetEndpoints(CloudEnvironment.DoD);
        Assert.Equal(AzureAuthorityHosts.AzureGovernment, authorityHost);
    }

    [Fact]
    public void GetScopes_Commercial_ReturnsCorrectScope()
    {
        var scopes = CloudEndpoints.GetScopes(CloudEnvironment.Commercial);
        Assert.Single(scopes);
        Assert.Equal("https://graph.microsoft.com/.default", scopes[0]);
    }

    [Fact]
    public void GetScopes_GCCHigh_ReturnsCorrectScope()
    {
        var scopes = CloudEndpoints.GetScopes(CloudEnvironment.GCCHigh);
        Assert.Single(scopes);
        Assert.Equal("https://graph.microsoft.us/.default", scopes[0]);
    }

    [Fact]
    public void GetScopes_DoD_ReturnsCorrectScope()
    {
        var scopes = CloudEndpoints.GetScopes(CloudEnvironment.DoD);
        Assert.Single(scopes);
        Assert.Equal("https://dod-graph.microsoft.us/.default", scopes[0]);
    }
}

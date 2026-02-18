using Azure.Core;
using IntuneManager.Core.Models;
using Microsoft.Graph.Beta;

namespace IntuneManager.Core.Auth;

public class IntuneGraphClientFactory
{
    private readonly IAuthenticationProvider _authProvider;

    public IntuneGraphClientFactory(IAuthenticationProvider authProvider)
    {
        _authProvider = authProvider;
    }

    public async Task<GraphServiceClient> CreateClientAsync(TenantProfile profile, CancellationToken cancellationToken = default)
    {
        var credential = await _authProvider.GetCredentialAsync(profile, cancellationToken);
        var (graphBaseUrl, _) = CloudEndpoints.GetEndpoints(profile.Cloud);
        var scopes = CloudEndpoints.GetScopes(profile.Cloud);

        return new GraphServiceClient(credential, scopes, graphBaseUrl);
    }
}

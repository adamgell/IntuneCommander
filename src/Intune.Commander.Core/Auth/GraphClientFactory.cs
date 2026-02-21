using Azure.Core;
using Azure.Identity;
using Intune.Commander.Core.Models;
using Microsoft.Graph.Beta;

namespace Intune.Commander.Core.Auth;

public class IntuneGraphClientFactory
{
    private readonly IAuthenticationProvider _authProvider;

    public IntuneGraphClientFactory(IAuthenticationProvider authProvider)
    {
        _authProvider = authProvider;
    }

    public async Task<GraphServiceClient> CreateClientAsync(
        TenantProfile profile,
        Func<DeviceCodeInfo, CancellationToken, Task>? deviceCodeCallback = null,
        CancellationToken cancellationToken = default)
    {
        var credential = await _authProvider.GetCredentialAsync(profile, deviceCodeCallback, cancellationToken);
        var (graphBaseUrl, _) = CloudEndpoints.GetEndpoints(profile.Cloud);
        var scopes = CloudEndpoints.GetScopes(profile.Cloud);

        return new GraphServiceClient(credential, scopes, graphBaseUrl);
    }
}

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
        var (client, _, _) = await CreateClientWithCredentialAsync(profile, deviceCodeCallback, cancellationToken);
        return client;
    }

    /// <summary>
    /// Creates a <see cref="GraphServiceClient"/> and returns it together with the
    /// underlying <see cref="TokenCredential"/> and scopes, so callers can use the
    /// credential independently (e.g. to acquire tokens for permission checking).
    /// </summary>
    public async Task<(GraphServiceClient Client, TokenCredential Credential, string[] Scopes)> CreateClientWithCredentialAsync(
        TenantProfile profile,
        Func<DeviceCodeInfo, CancellationToken, Task>? deviceCodeCallback = null,
        CancellationToken cancellationToken = default)
    {
        var credential = await _authProvider.GetCredentialAsync(profile, deviceCodeCallback, cancellationToken);
        var (graphBaseUrl, _) = CloudEndpoints.GetEndpoints(profile.Cloud);
        var scopes = CloudEndpoints.GetScopes(profile.Cloud);

        return (new GraphServiceClient(credential, scopes, graphBaseUrl), credential, scopes);
    }
}

using Azure.Core;
using Azure.Identity;
using IntuneManager.Core.Models;

namespace IntuneManager.Core.Auth;

public class ClientSecretAuthProvider : IAuthenticationProvider
{
    public Task<TokenCredential> GetCredentialAsync(TenantProfile profile, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profile.ClientSecret))
        {
            throw new InvalidOperationException("Client secret is required for ClientSecret authentication.");
        }

        var (_, authorityHost) = CloudEndpoints.GetEndpoints(profile.Cloud);

        var credential = new ClientSecretCredential(
            profile.TenantId,
            profile.ClientId,
            profile.ClientSecret,
            new TokenCredentialOptions
            {
                AuthorityHost = authorityHost
            });

        return Task.FromResult<TokenCredential>(credential);
    }
}

using Azure.Core;
using Azure.Identity;
using IntuneManager.Core.Models;

namespace IntuneManager.Core.Auth;

public class InteractiveBrowserAuthProvider : IAuthenticationProvider
{
    public Task<TokenCredential> GetCredentialAsync(TenantProfile profile, CancellationToken cancellationToken = default)
    {
        var (_, authorityHost) = CloudEndpoints.GetEndpoints(profile.Cloud);

        var credential = new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
        {
            TenantId = profile.TenantId,
            ClientId = profile.ClientId,
            AuthorityHost = authorityHost,
            TokenCachePersistenceOptions = new TokenCachePersistenceOptions
            {
                Name = $"IntuneManager-{profile.Id}"
            }
        });

        return Task.FromResult<TokenCredential>(credential);
    }
}

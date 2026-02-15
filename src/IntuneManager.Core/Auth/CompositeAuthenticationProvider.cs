using Azure.Core;
using IntuneManager.Core.Models;

namespace IntuneManager.Core.Auth;

public class CompositeAuthenticationProvider : IAuthenticationProvider
{
    private readonly InteractiveBrowserAuthProvider _interactiveProvider;
    private readonly ClientSecretAuthProvider _clientSecretProvider;

    public CompositeAuthenticationProvider()
    {
        _interactiveProvider = new InteractiveBrowserAuthProvider();
        _clientSecretProvider = new ClientSecretAuthProvider();
    }

    public Task<TokenCredential> GetCredentialAsync(TenantProfile profile, CancellationToken cancellationToken = default)
    {
        return profile.AuthMethod switch
        {
            AuthMethod.Interactive => _interactiveProvider.GetCredentialAsync(profile, cancellationToken),
            AuthMethod.ClientSecret => _clientSecretProvider.GetCredentialAsync(profile, cancellationToken),
            _ => throw new NotSupportedException($"Authentication method '{profile.AuthMethod}' is not supported yet.")
        };
    }
}

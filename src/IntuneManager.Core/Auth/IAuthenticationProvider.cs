using Azure.Core;
using IntuneManager.Core.Models;

namespace IntuneManager.Core.Auth;

public interface IAuthenticationProvider
{
    Task<TokenCredential> GetCredentialAsync(TenantProfile profile, CancellationToken cancellationToken = default);
}

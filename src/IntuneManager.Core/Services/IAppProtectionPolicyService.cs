using Microsoft.Graph.Beta.Models;

namespace IntuneManager.Core.Services;

public interface IAppProtectionPolicyService
{
    Task<List<ManagedAppPolicy>> ListAppProtectionPoliciesAsync(CancellationToken cancellationToken = default);
    Task<ManagedAppPolicy?> GetAppProtectionPolicyAsync(string id, CancellationToken cancellationToken = default);
    Task<ManagedAppPolicy> CreateAppProtectionPolicyAsync(ManagedAppPolicy policy, CancellationToken cancellationToken = default);
    Task<ManagedAppPolicy> UpdateAppProtectionPolicyAsync(ManagedAppPolicy policy, CancellationToken cancellationToken = default);
    Task DeleteAppProtectionPolicyAsync(string id, CancellationToken cancellationToken = default);
}

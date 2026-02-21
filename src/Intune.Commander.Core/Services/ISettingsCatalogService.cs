using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Core.Services;

public interface ISettingsCatalogService
{
    Task<List<DeviceManagementConfigurationPolicy>> ListSettingsCatalogPoliciesAsync(CancellationToken cancellationToken = default);
    Task<DeviceManagementConfigurationPolicy?> GetSettingsCatalogPolicyAsync(string id, CancellationToken cancellationToken = default);
    Task<List<DeviceManagementConfigurationPolicyAssignment>> GetAssignmentsAsync(string policyId, CancellationToken cancellationToken = default);
}

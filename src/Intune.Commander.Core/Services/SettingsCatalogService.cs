using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;
using Microsoft.Kiota.Abstractions;

namespace Intune.Commander.Core.Services;

public class SettingsCatalogService : ISettingsCatalogService
{
    private readonly GraphServiceClient _graphClient;

    public SettingsCatalogService(GraphServiceClient graphClient)
    {
        _graphClient = graphClient;
    }

    public async Task<List<DeviceManagementConfigurationPolicy>> ListSettingsCatalogPoliciesAsync(CancellationToken cancellationToken = default)
    {
        var result = new List<DeviceManagementConfigurationPolicy>();

        var response = await _graphClient.DeviceManagement.ConfigurationPolicies
            .GetAsync(req =>
            {
                req.QueryParameters.Top = 999;
                req.QueryParameters.Select = new[]
                {
                    "id", "name", "description", "platforms", "technologies",
                    "createdDateTime", "lastModifiedDateTime", "settingCount",
                    "roleScopeTagIds", "isAssigned", "templateReference"
                };
            }, cancellationToken);

        while (response != null)
        {
            if (response.Value != null)
                result.AddRange(response.Value);

            if (!string.IsNullOrEmpty(response.OdataNextLink))
            {
                response = await _graphClient.DeviceManagement.ConfigurationPolicies
                    .WithUrl(response.OdataNextLink)
                    .GetAsync(cancellationToken: cancellationToken);
            }
            else
            {
                break;
            }
        }

        return result;
    }

    public async Task<DeviceManagementConfigurationPolicy?> GetSettingsCatalogPolicyAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _graphClient.DeviceManagement.ConfigurationPolicies[id]
            .GetAsync(cancellationToken: cancellationToken);
    }

    public async Task<List<DeviceManagementConfigurationPolicyAssignment>> GetAssignmentsAsync(string policyId, CancellationToken cancellationToken = default)
    {
        var response = await _graphClient.DeviceManagement.ConfigurationPolicies[policyId]
            .Assignments.GetAsync(cancellationToken: cancellationToken);

        return response?.Value ?? [];
    }

    public async Task<List<DeviceManagementConfigurationSetting>> GetPolicySettingsAsync(string policyId, CancellationToken cancellationToken = default)
    {
        var result = new List<DeviceManagementConfigurationSetting>();

        var response = await _graphClient.DeviceManagement.ConfigurationPolicies[policyId]
            .Settings.GetAsync(req =>
            {
                req.QueryParameters.Top = 999;
            }, cancellationToken);

        while (response != null)
        {
            if (response.Value != null)
                result.AddRange(response.Value);

            if (!string.IsNullOrEmpty(response.OdataNextLink))
            {
                response = await _graphClient.DeviceManagement.ConfigurationPolicies[policyId]
                    .Settings.WithUrl(response.OdataNextLink)
                    .GetAsync(cancellationToken: cancellationToken);
            }
            else
            {
                break;
            }
        }

        return result;
    }

    public async Task<DeviceManagementConfigurationPolicy> CreateSettingsCatalogPolicyAsync(DeviceManagementConfigurationPolicy policy, CancellationToken cancellationToken = default)
    {
        var result = await _graphClient.DeviceManagement.ConfigurationPolicies
            .PostAsync(policy, cancellationToken: cancellationToken);

        return result ?? throw new InvalidOperationException("Failed to create settings catalog policy");
    }

    public async Task AssignSettingsCatalogPolicyAsync(string policyId, List<DeviceManagementConfigurationPolicyAssignment> assignments, CancellationToken cancellationToken = default)
    {
        await _graphClient.DeviceManagement.ConfigurationPolicies[policyId]
            .Assign.PostAsAssignPostResponseAsync(
                new Microsoft.Graph.Beta.DeviceManagement.ConfigurationPolicies.Item.Assign.AssignPostRequestBody
                {
                    Assignments = assignments
                },
                cancellationToken: cancellationToken);
    }
}

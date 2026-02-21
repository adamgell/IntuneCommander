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
                req.QueryParameters.Top = 200;
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
}

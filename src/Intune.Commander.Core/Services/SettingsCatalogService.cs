using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;
using Microsoft.Kiota.Abstractions;

namespace Intune.Commander.Core.Services;

public class SettingsCatalogService : ISettingsCatalogService
{
    private readonly GraphServiceClient _graphClient;

    // The configurationPolicies endpoint can return HTTP 500 on certain Cosmos DB
    // skip-token page boundaries. Use smaller pages to reduce the chance of hitting
    // a broken cursor, and retry on transient 500s before returning partial results.
    private const int PageSize = 100;
    private const int MaxRetries = 3;

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
                req.QueryParameters.Top = PageSize;
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

            if (string.IsNullOrEmpty(response.OdataNextLink))
                break;

            // Retry on transient 500s — the Cosmos DB backend can fail on specific
            // skip-token cursors; retrying with backoff often succeeds on the same URL.
            var nextLink = response.OdataNextLink;
            response = null;
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    response = await _graphClient.DeviceManagement.ConfigurationPolicies
                        .WithUrl(nextLink)
                        .GetAsync(cancellationToken: cancellationToken);
                    break; // success — exit retry loop
                }
                catch (ApiException apiEx) when (apiEx.ResponseStatusCode == 500 && attempt < MaxRetries)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), cancellationToken);
                }
                catch (ApiException apiEx) when (apiEx.ResponseStatusCode == 500)
                {
                    // All retries exhausted — return the items collected so far rather
                    // than throwing and losing all previously fetched pages.
                    return result;
                }
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

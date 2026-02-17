using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace IntuneManager.Core.Services;

public class CompliancePolicyService : ICompliancePolicyService
{
    private readonly GraphServiceClient _graphClient;

    public CompliancePolicyService(GraphServiceClient graphClient)
    {
        _graphClient = graphClient;
    }

    public async Task<List<DeviceCompliancePolicy>> ListCompliancePoliciesAsync(CancellationToken cancellationToken = default)
    {
        var result = new List<DeviceCompliancePolicy>();

        var response = await _graphClient.DeviceManagement.DeviceCompliancePolicies
            .GetAsync(cancellationToken: cancellationToken);

        if (response != null)
        {
            // PageIterator handles first page + all subsequent pages.
            var pageIterator = PageIterator<DeviceCompliancePolicy, DeviceCompliancePolicyCollectionResponse>
                .CreatePageIterator(_graphClient, response, item =>
                {
                    result.Add(item);
                    return true;
                });

            await pageIterator.IterateAsync(cancellationToken);
        }

        return result;
    }

    public async Task<DeviceCompliancePolicy?> GetCompliancePolicyAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _graphClient.DeviceManagement.DeviceCompliancePolicies[id]
            .GetAsync(cancellationToken: cancellationToken);
    }

    public async Task<DeviceCompliancePolicy> CreateCompliancePolicyAsync(DeviceCompliancePolicy policy, CancellationToken cancellationToken = default)
    {
        var result = await _graphClient.DeviceManagement.DeviceCompliancePolicies
            .PostAsync(policy, cancellationToken: cancellationToken);

        return result ?? throw new InvalidOperationException("Failed to create compliance policy");
    }

    public async Task<DeviceCompliancePolicy> UpdateCompliancePolicyAsync(DeviceCompliancePolicy policy, CancellationToken cancellationToken = default)
    {
        var id = policy.Id ?? throw new ArgumentException("Compliance policy must have an ID for update");

        var result = await _graphClient.DeviceManagement.DeviceCompliancePolicies[id]
            .PatchAsync(policy, cancellationToken: cancellationToken);

        return result ?? throw new InvalidOperationException("Failed to update compliance policy");
    }

    public async Task DeleteCompliancePolicyAsync(string id, CancellationToken cancellationToken = default)
    {
        await _graphClient.DeviceManagement.DeviceCompliancePolicies[id]
            .DeleteAsync(cancellationToken: cancellationToken);
    }

    public async Task<List<DeviceCompliancePolicyAssignment>> GetAssignmentsAsync(string policyId, CancellationToken cancellationToken = default)
    {
        var response = await _graphClient.DeviceManagement.DeviceCompliancePolicies[policyId]
            .Assignments.GetAsync(cancellationToken: cancellationToken);

        return response?.Value ?? [];
    }

    public async Task AssignPolicyAsync(string policyId, List<DeviceCompliancePolicyAssignment> assignments, CancellationToken cancellationToken = default)
    {
        // The Graph API uses the assign action to set assignments
        await _graphClient.DeviceManagement.DeviceCompliancePolicies[policyId]
            .Assign.PostAsAssignPostResponseAsync(
                new Microsoft.Graph.DeviceManagement.DeviceCompliancePolicies.Item.Assign.AssignPostRequestBody
                {
                    Assignments = assignments
                },
                cancellationToken: cancellationToken);
    }
}

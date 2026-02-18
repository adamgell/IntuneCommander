using System.Text.Json;
using IntuneManager.Core.Models;
using Microsoft.Graph.Beta.Models;

namespace IntuneManager.Core.Services;

public class ImportService : IImportService
{
    private readonly IConfigurationProfileService _configProfileService;
    private readonly ICompliancePolicyService? _compliancePolicyService;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ImportService(IConfigurationProfileService configProfileService, ICompliancePolicyService? compliancePolicyService = null)
    {
        _configProfileService = configProfileService;
        _compliancePolicyService = compliancePolicyService;
    }

    public async Task<DeviceConfiguration?> ReadDeviceConfigurationAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        return JsonSerializer.Deserialize<DeviceConfiguration>(json, JsonOptions);
    }

    public async Task<List<DeviceConfiguration>> ReadDeviceConfigurationsFromFolderAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        var configs = new List<DeviceConfiguration>();
        var configFolder = Path.Combine(folderPath, "DeviceConfigurations");

        if (!Directory.Exists(configFolder))
            return configs;

        foreach (var file in Directory.GetFiles(configFolder, "*.json"))
        {
            var config = await ReadDeviceConfigurationAsync(file, cancellationToken);
            if (config != null)
                configs.Add(config);
        }

        return configs;
    }

    public async Task<MigrationTable> ReadMigrationTableAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(folderPath, "migration-table.json");

        if (!File.Exists(filePath))
            return new MigrationTable();

        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        return JsonSerializer.Deserialize<MigrationTable>(json, JsonOptions) ?? new MigrationTable();
    }

    public async Task<DeviceConfiguration> ImportDeviceConfigurationAsync(
        DeviceConfiguration config,
        MigrationTable migrationTable,
        CancellationToken cancellationToken = default)
    {
        var originalId = config.Id;

        // Clear the ID so Graph creates a new object
        config.Id = null;
        // Clear read-only properties that can't be set during creation
        config.CreatedDateTime = null;
        config.LastModifiedDateTime = null;
        config.Version = null;

        var created = await _configProfileService.CreateDeviceConfigurationAsync(config, cancellationToken);

        // Update migration table with the mapping
        if (originalId != null && created.Id != null)
        {
            migrationTable.AddOrUpdate(new MigrationEntry
            {
                ObjectType = "DeviceConfiguration",
                OriginalId = originalId,
                NewId = created.Id,
                Name = created.DisplayName ?? "Unknown"
            });
        }

        return created;
    }

    public async Task<CompliancePolicyExport?> ReadCompliancePolicyAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        return JsonSerializer.Deserialize<CompliancePolicyExport>(json, JsonOptions);
    }

    public async Task<List<CompliancePolicyExport>> ReadCompliancePoliciesFromFolderAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        var results = new List<CompliancePolicyExport>();
        var policyFolder = Path.Combine(folderPath, "CompliancePolicies");

        if (!Directory.Exists(policyFolder))
            return results;

        foreach (var file in Directory.GetFiles(policyFolder, "*.json"))
        {
            var export = await ReadCompliancePolicyAsync(file, cancellationToken);
            if (export != null)
                results.Add(export);
        }

        return results;
    }

    public async Task<DeviceCompliancePolicy> ImportCompliancePolicyAsync(
        CompliancePolicyExport export,
        MigrationTable migrationTable,
        CancellationToken cancellationToken = default)
    {
        if (_compliancePolicyService == null)
            throw new InvalidOperationException("Compliance policy service is not available");

        var policy = export.Policy;
        var originalId = policy.Id;

        // Clear read-only properties
        policy.Id = null;
        policy.CreatedDateTime = null;
        policy.LastModifiedDateTime = null;
        policy.Version = null;

        var created = await _compliancePolicyService.CreateCompliancePolicyAsync(policy, cancellationToken);

        // Re-create assignments if present
        if (export.Assignments.Count > 0 && created.Id != null)
        {
            // Clear assignment IDs so Graph creates new ones
            foreach (var assignment in export.Assignments)
            {
                assignment.Id = null;
            }

            await _compliancePolicyService.AssignPolicyAsync(created.Id, export.Assignments, cancellationToken);
        }

        // Update migration table
        if (originalId != null && created.Id != null)
        {
            migrationTable.AddOrUpdate(new MigrationEntry
            {
                ObjectType = "CompliancePolicy",
                OriginalId = originalId,
                NewId = created.Id,
                Name = created.DisplayName ?? "Unknown"
            });
        }

        return created;
    }
}

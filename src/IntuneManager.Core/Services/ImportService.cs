using System.Text.Json;
using IntuneManager.Core.Models;
using Microsoft.Graph.Beta.Models;

namespace IntuneManager.Core.Services;

public class ImportService : IImportService
{
    private readonly IConfigurationProfileService _configProfileService;
    private readonly ICompliancePolicyService? _compliancePolicyService;
    private readonly IEndpointSecurityService? _endpointSecurityService;
    private readonly IAdministrativeTemplateService? _administrativeTemplateService;
    private readonly IEnrollmentConfigurationService? _enrollmentConfigurationService;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ImportService(
        IConfigurationProfileService configProfileService,
        ICompliancePolicyService? compliancePolicyService = null,
        IEndpointSecurityService? endpointSecurityService = null,
        IAdministrativeTemplateService? administrativeTemplateService = null,
        IEnrollmentConfigurationService? enrollmentConfigurationService = null)
    {
        _configProfileService = configProfileService;
        _compliancePolicyService = compliancePolicyService;
        _endpointSecurityService = endpointSecurityService;
        _administrativeTemplateService = administrativeTemplateService;
        _enrollmentConfigurationService = enrollmentConfigurationService;
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

    public async Task<EndpointSecurityExport?> ReadEndpointSecurityIntentAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        return JsonSerializer.Deserialize<EndpointSecurityExport>(json, JsonOptions);
    }

    public async Task<List<EndpointSecurityExport>> ReadEndpointSecurityIntentsFromFolderAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        var results = new List<EndpointSecurityExport>();
        var intentsFolder = Path.Combine(folderPath, "EndpointSecurity");

        if (!Directory.Exists(intentsFolder))
            return results;

        foreach (var file in Directory.GetFiles(intentsFolder, "*.json"))
        {
            var export = await ReadEndpointSecurityIntentAsync(file, cancellationToken);
            if (export != null)
                results.Add(export);
        }

        return results;
    }

    public async Task<DeviceManagementIntent> ImportEndpointSecurityIntentAsync(
        EndpointSecurityExport export,
        MigrationTable migrationTable,
        CancellationToken cancellationToken = default)
    {
        if (_endpointSecurityService == null)
            throw new InvalidOperationException("Endpoint security service is not available");

        var intent = export.Intent;
        var originalId = intent.Id;

        intent.Id = null;

        var created = await _endpointSecurityService.CreateEndpointSecurityIntentAsync(intent, cancellationToken);

        if (export.Assignments.Count > 0 && created.Id != null)
        {
            foreach (var assignment in export.Assignments)
            {
                assignment.Id = null;
            }

            await _endpointSecurityService.AssignIntentAsync(created.Id, export.Assignments, cancellationToken);
        }

        if (originalId != null && created.Id != null)
        {
            migrationTable.AddOrUpdate(new MigrationEntry
            {
                ObjectType = "EndpointSecurityIntent",
                OriginalId = originalId,
                NewId = created.Id,
                Name = created.DisplayName ?? "Unknown"
            });
        }

        return created;
    }

    public async Task<AdministrativeTemplateExport?> ReadAdministrativeTemplateAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        return JsonSerializer.Deserialize<AdministrativeTemplateExport>(json, JsonOptions);
    }

    public async Task<List<AdministrativeTemplateExport>> ReadAdministrativeTemplatesFromFolderAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        var results = new List<AdministrativeTemplateExport>();
        var templatesFolder = Path.Combine(folderPath, "AdministrativeTemplates");

        if (!Directory.Exists(templatesFolder))
            return results;

        foreach (var file in Directory.GetFiles(templatesFolder, "*.json"))
        {
            var export = await ReadAdministrativeTemplateAsync(file, cancellationToken);
            if (export != null)
                results.Add(export);
        }

        return results;
    }

    public async Task<GroupPolicyConfiguration> ImportAdministrativeTemplateAsync(
        AdministrativeTemplateExport export,
        MigrationTable migrationTable,
        CancellationToken cancellationToken = default)
    {
        if (_administrativeTemplateService == null)
            throw new InvalidOperationException("Administrative template service is not available");

        var template = export.Template;
        var originalId = template.Id;

        template.Id = null;
        template.CreatedDateTime = null;
        template.LastModifiedDateTime = null;

        var created = await _administrativeTemplateService.CreateAdministrativeTemplateAsync(template, cancellationToken);

        if (export.Assignments.Count > 0 && created.Id != null)
        {
            foreach (var assignment in export.Assignments)
            {
                assignment.Id = null;
            }

            await _administrativeTemplateService.AssignAdministrativeTemplateAsync(created.Id, export.Assignments, cancellationToken);
        }

        if (originalId != null && created.Id != null)
        {
            migrationTable.AddOrUpdate(new MigrationEntry
            {
                ObjectType = "AdministrativeTemplate",
                OriginalId = originalId,
                NewId = created.Id,
                Name = created.DisplayName ?? "Unknown"
            });
        }

        return created;
    }

    public async Task<DeviceEnrollmentConfiguration?> ReadEnrollmentConfigurationAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        return JsonSerializer.Deserialize<DeviceEnrollmentConfiguration>(json, JsonOptions);
    }

    public async Task<List<DeviceEnrollmentConfiguration>> ReadEnrollmentConfigurationsFromFolderAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        var results = new List<DeviceEnrollmentConfiguration>();
        var configsFolder = Path.Combine(folderPath, "EnrollmentConfigurations");

        if (!Directory.Exists(configsFolder))
            return results;

        foreach (var file in Directory.GetFiles(configsFolder, "*.json"))
        {
            var config = await ReadEnrollmentConfigurationAsync(file, cancellationToken);
            if (config != null)
                results.Add(config);
        }

        return results;
    }

    public async Task<DeviceEnrollmentConfiguration> ImportEnrollmentConfigurationAsync(
        DeviceEnrollmentConfiguration configuration,
        MigrationTable migrationTable,
        CancellationToken cancellationToken = default)
    {
        if (_enrollmentConfigurationService == null)
            throw new InvalidOperationException("Enrollment configuration service is not available");

        var originalId = configuration.Id;

        configuration.Id = null;
        configuration.CreatedDateTime = null;
        configuration.LastModifiedDateTime = null;

        var created = await _enrollmentConfigurationService.CreateEnrollmentConfigurationAsync(configuration, cancellationToken);

        if (originalId != null && created.Id != null)
        {
            migrationTable.AddOrUpdate(new MigrationEntry
            {
                ObjectType = "EnrollmentConfiguration",
                OriginalId = originalId,
                NewId = created.Id,
                Name = created.DisplayName ?? "Unknown"
            });
        }

        return created;
    }
}

using System.Text.Json;
using IntuneManager.Core.Models;
using Microsoft.Graph.Beta.Models;

namespace IntuneManager.Core.Services;

public class ExportService : IExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task ExportDeviceConfigurationAsync(
        DeviceConfiguration config,
        string outputPath,
        MigrationTable migrationTable,
        CancellationToken cancellationToken = default)
    {
        var folderPath = Path.Combine(outputPath, "DeviceConfigurations");
        Directory.CreateDirectory(folderPath);

        var sanitizedName = SanitizeFileName(config.DisplayName ?? config.Id ?? "unknown");
        var filePath = Path.Combine(folderPath, $"{sanitizedName}.json");

        // Serialize using System.Text.Json with the Graph model
        var json = JsonSerializer.Serialize(config, config.GetType(), JsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);

        // Add to migration table
        if (config.Id != null)
        {
            migrationTable.AddOrUpdate(new MigrationEntry
            {
                ObjectType = "DeviceConfiguration",
                OriginalId = config.Id,
                Name = config.DisplayName ?? "Unknown"
            });
        }
    }

    public async Task ExportDeviceConfigurationsAsync(
        IEnumerable<DeviceConfiguration> configs,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var migrationTable = new MigrationTable();

        foreach (var config in configs)
        {
            await ExportDeviceConfigurationAsync(config, outputPath, migrationTable, cancellationToken);
        }

        await SaveMigrationTableAsync(migrationTable, outputPath, cancellationToken);
    }

    public async Task SaveMigrationTableAsync(MigrationTable table, string outputPath, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(outputPath, "migration-table.json");
        var json = JsonSerializer.Serialize(table, JsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).Trim();
    }

    public async Task ExportCompliancePolicyAsync(
        DeviceCompliancePolicy policy,
        IReadOnlyList<DeviceCompliancePolicyAssignment> assignments,
        string outputPath,
        MigrationTable migrationTable,
        CancellationToken cancellationToken = default)
    {
        var folderPath = Path.Combine(outputPath, "CompliancePolicies");
        Directory.CreateDirectory(folderPath);

        var sanitizedName = SanitizeFileName(policy.DisplayName ?? policy.Id ?? "unknown");
        var filePath = Path.Combine(folderPath, $"{sanitizedName}.json");

        var export = new CompliancePolicyExport
        {
            Policy = policy,
            Assignments = assignments.ToList()
        };

        var json = JsonSerializer.Serialize(export, JsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);

        if (policy.Id != null)
        {
            migrationTable.AddOrUpdate(new MigrationEntry
            {
                ObjectType = "CompliancePolicy",
                OriginalId = policy.Id,
                Name = policy.DisplayName ?? "Unknown"
            });
        }
    }

    public async Task ExportCompliancePoliciesAsync(
        IEnumerable<(DeviceCompliancePolicy Policy, IReadOnlyList<DeviceCompliancePolicyAssignment> Assignments)> policies,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var migrationTable = new MigrationTable();

        foreach (var (policy, assignments) in policies)
        {
            await ExportCompliancePolicyAsync(policy, assignments, outputPath, migrationTable, cancellationToken);
        }

        await SaveMigrationTableAsync(migrationTable, outputPath, cancellationToken);
    }

    public async Task ExportApplicationAsync(
        MobileApp app,
        IReadOnlyList<MobileAppAssignment> assignments,
        string outputPath,
        MigrationTable migrationTable,
        CancellationToken cancellationToken = default)
    {
        var folderPath = Path.Combine(outputPath, "Applications");
        Directory.CreateDirectory(folderPath);

        var sanitizedName = SanitizeFileName(app.DisplayName ?? app.Id ?? "unknown");
        var filePath = Path.Combine(folderPath, $"{sanitizedName}.json");

        var export = new ApplicationExport
        {
            Application = app,
            Assignments = assignments.ToList()
        };

        var json = JsonSerializer.Serialize(export, JsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);

        if (app.Id != null)
        {
            migrationTable.AddOrUpdate(new MigrationEntry
            {
                ObjectType = "Application",
                OriginalId = app.Id,
                Name = app.DisplayName ?? "Unknown"
            });
        }
    }

    public async Task ExportApplicationsAsync(
        IEnumerable<(MobileApp App, IReadOnlyList<MobileAppAssignment> Assignments)> apps,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var migrationTable = new MigrationTable();

        foreach (var (app, assignments) in apps)
        {
            await ExportApplicationAsync(app, assignments, outputPath, migrationTable, cancellationToken);
        }

        await SaveMigrationTableAsync(migrationTable, outputPath, cancellationToken);
    }

    public async Task ExportEndpointSecurityIntentAsync(
        DeviceManagementIntent intent,
        IReadOnlyList<DeviceManagementIntentAssignment> assignments,
        string outputPath,
        MigrationTable migrationTable,
        CancellationToken cancellationToken = default)
    {
        var folderPath = Path.Combine(outputPath, "EndpointSecurity");
        Directory.CreateDirectory(folderPath);

        var sanitizedName = SanitizeFileName(intent.DisplayName ?? intent.Id ?? "unknown");
        var filePath = Path.Combine(folderPath, $"{sanitizedName}.json");

        var export = new EndpointSecurityExport
        {
            Intent = intent,
            Assignments = assignments.ToList()
        };

        var json = JsonSerializer.Serialize(export, JsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);

        if (intent.Id != null)
        {
            migrationTable.AddOrUpdate(new MigrationEntry
            {
                ObjectType = "EndpointSecurityIntent",
                OriginalId = intent.Id,
                Name = intent.DisplayName ?? "Unknown"
            });
        }
    }

    public async Task ExportEndpointSecurityIntentsAsync(
        IEnumerable<(DeviceManagementIntent Intent, IReadOnlyList<DeviceManagementIntentAssignment> Assignments)> intents,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var migrationTable = new MigrationTable();

        foreach (var (intent, assignments) in intents)
        {
            await ExportEndpointSecurityIntentAsync(intent, assignments, outputPath, migrationTable, cancellationToken);
        }

        await SaveMigrationTableAsync(migrationTable, outputPath, cancellationToken);
    }

    public async Task ExportAdministrativeTemplateAsync(
        GroupPolicyConfiguration template,
        IReadOnlyList<GroupPolicyConfigurationAssignment> assignments,
        string outputPath,
        MigrationTable migrationTable,
        CancellationToken cancellationToken = default)
    {
        var folderPath = Path.Combine(outputPath, "AdministrativeTemplates");
        Directory.CreateDirectory(folderPath);

        var sanitizedName = SanitizeFileName(template.DisplayName ?? template.Id ?? "unknown");
        var filePath = Path.Combine(folderPath, $"{sanitizedName}.json");

        var export = new AdministrativeTemplateExport
        {
            Template = template,
            Assignments = assignments.ToList()
        };

        var json = JsonSerializer.Serialize(export, JsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);

        if (template.Id != null)
        {
            migrationTable.AddOrUpdate(new MigrationEntry
            {
                ObjectType = "AdministrativeTemplate",
                OriginalId = template.Id,
                Name = template.DisplayName ?? "Unknown"
            });
        }
    }

    public async Task ExportAdministrativeTemplatesAsync(
        IEnumerable<(GroupPolicyConfiguration Template, IReadOnlyList<GroupPolicyConfigurationAssignment> Assignments)> templates,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var migrationTable = new MigrationTable();

        foreach (var (template, assignments) in templates)
        {
            await ExportAdministrativeTemplateAsync(template, assignments, outputPath, migrationTable, cancellationToken);
        }

        await SaveMigrationTableAsync(migrationTable, outputPath, cancellationToken);
    }

    public async Task ExportEnrollmentConfigurationAsync(
        DeviceEnrollmentConfiguration configuration,
        string outputPath,
        MigrationTable migrationTable,
        CancellationToken cancellationToken = default)
    {
        var folderPath = Path.Combine(outputPath, "EnrollmentConfigurations");
        Directory.CreateDirectory(folderPath);

        var sanitizedName = SanitizeFileName(configuration.DisplayName ?? configuration.Id ?? "unknown");
        var filePath = Path.Combine(folderPath, $"{sanitizedName}.json");

        var json = JsonSerializer.Serialize(configuration, configuration.GetType(), JsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);

        if (configuration.Id != null)
        {
            migrationTable.AddOrUpdate(new MigrationEntry
            {
                ObjectType = "EnrollmentConfiguration",
                OriginalId = configuration.Id,
                Name = configuration.DisplayName ?? "Unknown"
            });
        }
    }

    public async Task ExportEnrollmentConfigurationsAsync(
        IEnumerable<DeviceEnrollmentConfiguration> configurations,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var migrationTable = new MigrationTable();

        foreach (var configuration in configurations)
        {
            await ExportEnrollmentConfigurationAsync(configuration, outputPath, migrationTable, cancellationToken);
        }

        await SaveMigrationTableAsync(migrationTable, outputPath, cancellationToken);
    }

    public async Task ExportAppProtectionPolicyAsync(
        ManagedAppPolicy policy,
        string outputPath,
        MigrationTable migrationTable,
        CancellationToken cancellationToken = default)
    {
        var folderPath = Path.Combine(outputPath, "AppProtectionPolicies");
        Directory.CreateDirectory(folderPath);

        var sanitizedName = SanitizeFileName(policy.DisplayName ?? policy.Id ?? "unknown");
        var filePath = Path.Combine(folderPath, $"{sanitizedName}.json");

        var json = JsonSerializer.Serialize(policy, policy.GetType(), JsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);

        if (policy.Id != null)
        {
            migrationTable.AddOrUpdate(new MigrationEntry
            {
                ObjectType = "AppProtectionPolicy",
                OriginalId = policy.Id,
                Name = policy.DisplayName ?? "Unknown"
            });
        }
    }

    public async Task ExportAppProtectionPoliciesAsync(
        IEnumerable<ManagedAppPolicy> policies,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var migrationTable = new MigrationTable();

        foreach (var policy in policies)
        {
            await ExportAppProtectionPolicyAsync(policy, outputPath, migrationTable, cancellationToken);
        }

        await SaveMigrationTableAsync(migrationTable, outputPath, cancellationToken);
    }

    public async Task ExportManagedDeviceAppConfigurationAsync(
        ManagedDeviceMobileAppConfiguration configuration,
        string outputPath,
        MigrationTable migrationTable,
        CancellationToken cancellationToken = default)
    {
        var folderPath = Path.Combine(outputPath, "ManagedDeviceAppConfigurations");
        Directory.CreateDirectory(folderPath);

        var sanitizedName = SanitizeFileName(configuration.DisplayName ?? configuration.Id ?? "unknown");
        var filePath = Path.Combine(folderPath, $"{sanitizedName}.json");

        var json = JsonSerializer.Serialize(configuration, configuration.GetType(), JsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);

        if (configuration.Id != null)
        {
            migrationTable.AddOrUpdate(new MigrationEntry
            {
                ObjectType = "ManagedDeviceAppConfiguration",
                OriginalId = configuration.Id,
                Name = configuration.DisplayName ?? "Unknown"
            });
        }
    }

    public async Task ExportManagedDeviceAppConfigurationsAsync(
        IEnumerable<ManagedDeviceMobileAppConfiguration> configurations,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var migrationTable = new MigrationTable();

        foreach (var configuration in configurations)
        {
            await ExportManagedDeviceAppConfigurationAsync(configuration, outputPath, migrationTable, cancellationToken);
        }

        await SaveMigrationTableAsync(migrationTable, outputPath, cancellationToken);
    }

    public async Task ExportTargetedManagedAppConfigurationAsync(
        TargetedManagedAppConfiguration configuration,
        string outputPath,
        MigrationTable migrationTable,
        CancellationToken cancellationToken = default)
    {
        var folderPath = Path.Combine(outputPath, "TargetedManagedAppConfigurations");
        Directory.CreateDirectory(folderPath);

        var sanitizedName = SanitizeFileName(configuration.DisplayName ?? configuration.Id ?? "unknown");
        var filePath = Path.Combine(folderPath, $"{sanitizedName}.json");

        var json = JsonSerializer.Serialize(configuration, configuration.GetType(), JsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);

        if (configuration.Id != null)
        {
            migrationTable.AddOrUpdate(new MigrationEntry
            {
                ObjectType = "TargetedManagedAppConfiguration",
                OriginalId = configuration.Id,
                Name = configuration.DisplayName ?? "Unknown"
            });
        }
    }

    public async Task ExportTargetedManagedAppConfigurationsAsync(
        IEnumerable<TargetedManagedAppConfiguration> configurations,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var migrationTable = new MigrationTable();

        foreach (var configuration in configurations)
        {
            await ExportTargetedManagedAppConfigurationAsync(configuration, outputPath, migrationTable, cancellationToken);
        }

        await SaveMigrationTableAsync(migrationTable, outputPath, cancellationToken);
    }

    public async Task ExportTermsAndConditionsAsync(
        TermsAndConditions termsAndConditions,
        string outputPath,
        MigrationTable migrationTable,
        CancellationToken cancellationToken = default)
    {
        var folderPath = Path.Combine(outputPath, "TermsAndConditions");
        Directory.CreateDirectory(folderPath);

        var sanitizedName = SanitizeFileName(termsAndConditions.DisplayName ?? termsAndConditions.Id ?? "unknown");
        var filePath = Path.Combine(folderPath, $"{sanitizedName}.json");

        var json = JsonSerializer.Serialize(termsAndConditions, termsAndConditions.GetType(), JsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);

        if (termsAndConditions.Id != null)
        {
            migrationTable.AddOrUpdate(new MigrationEntry
            {
                ObjectType = "TermsAndConditions",
                OriginalId = termsAndConditions.Id,
                Name = termsAndConditions.DisplayName ?? "Unknown"
            });
        }
    }

    public async Task ExportTermsAndConditionsCollectionAsync(
        IEnumerable<TermsAndConditions> termsCollection,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var migrationTable = new MigrationTable();

        foreach (var termsAndConditions in termsCollection)
        {
            await ExportTermsAndConditionsAsync(termsAndConditions, outputPath, migrationTable, cancellationToken);
        }

        await SaveMigrationTableAsync(migrationTable, outputPath, cancellationToken);
    }
}

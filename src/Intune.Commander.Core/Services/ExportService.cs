using System.Text.Json;
using Intune.Commander.Core.Models;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Core.Services;

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

    public async Task ExportScopeTagAsync(
        RoleScopeTag scopeTag,
        string outputPath,
        MigrationTable migrationTable,
        CancellationToken cancellationToken = default)
    {
        var folderPath = Path.Combine(outputPath, "ScopeTags");
        Directory.CreateDirectory(folderPath);

        var sanitizedName = SanitizeFileName(scopeTag.DisplayName ?? scopeTag.Id ?? "unknown");
        var filePath = Path.Combine(folderPath, $"{sanitizedName}.json");

        var json = JsonSerializer.Serialize(scopeTag, scopeTag.GetType(), JsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);

        if (scopeTag.Id != null)
        {
            migrationTable.AddOrUpdate(new MigrationEntry
            {
                ObjectType = "ScopeTag",
                OriginalId = scopeTag.Id,
                Name = scopeTag.DisplayName ?? "Unknown"
            });
        }
    }

    public async Task ExportScopeTagsAsync(
        IEnumerable<RoleScopeTag> scopeTags,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var migrationTable = new MigrationTable();

        foreach (var scopeTag in scopeTags)
        {
            await ExportScopeTagAsync(scopeTag, outputPath, migrationTable, cancellationToken);
        }

        await SaveMigrationTableAsync(migrationTable, outputPath, cancellationToken);
    }

    public async Task ExportRoleDefinitionAsync(
        RoleDefinition roleDefinition,
        string outputPath,
        MigrationTable migrationTable,
        CancellationToken cancellationToken = default)
    {
        var folderPath = Path.Combine(outputPath, "RoleDefinitions");
        Directory.CreateDirectory(folderPath);

        var sanitizedName = SanitizeFileName(roleDefinition.DisplayName ?? roleDefinition.Id ?? "unknown");
        var filePath = Path.Combine(folderPath, $"{sanitizedName}.json");

        var json = JsonSerializer.Serialize(roleDefinition, roleDefinition.GetType(), JsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);

        if (roleDefinition.Id != null)
        {
            migrationTable.AddOrUpdate(new MigrationEntry
            {
                ObjectType = "RoleDefinition",
                OriginalId = roleDefinition.Id,
                Name = roleDefinition.DisplayName ?? "Unknown"
            });
        }
    }

    public async Task ExportRoleDefinitionsAsync(
        IEnumerable<RoleDefinition> roleDefinitions,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var migrationTable = new MigrationTable();

        foreach (var roleDefinition in roleDefinitions)
        {
            await ExportRoleDefinitionAsync(roleDefinition, outputPath, migrationTable, cancellationToken);
        }

        await SaveMigrationTableAsync(migrationTable, outputPath, cancellationToken);
    }

    public async Task ExportIntuneBrandingProfileAsync(
        IntuneBrandingProfile profile,
        string outputPath,
        MigrationTable migrationTable,
        CancellationToken cancellationToken = default)
    {
        var folderPath = Path.Combine(outputPath, "IntuneBrandingProfiles");
        Directory.CreateDirectory(folderPath);

        var sanitizedName = SanitizeFileName(profile.ProfileName ?? profile.Id ?? "unknown");
        var filePath = Path.Combine(folderPath, $"{sanitizedName}.json");

        var json = JsonSerializer.Serialize(profile, profile.GetType(), JsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);

        if (profile.Id != null)
        {
            migrationTable.AddOrUpdate(new MigrationEntry
            {
                ObjectType = "IntuneBrandingProfile",
                OriginalId = profile.Id,
                Name = profile.ProfileName ?? "Unknown"
            });
        }
    }

    public async Task ExportIntuneBrandingProfilesAsync(
        IEnumerable<IntuneBrandingProfile> profiles,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var migrationTable = new MigrationTable();

        foreach (var profile in profiles)
        {
            await ExportIntuneBrandingProfileAsync(profile, outputPath, migrationTable, cancellationToken);
        }

        await SaveMigrationTableAsync(migrationTable, outputPath, cancellationToken);
    }

    public async Task ExportAzureBrandingLocalizationAsync(
        OrganizationalBrandingLocalization localization,
        string outputPath,
        MigrationTable migrationTable,
        CancellationToken cancellationToken = default)
    {
        var folderPath = Path.Combine(outputPath, "AzureBrandingLocalizations");
        Directory.CreateDirectory(folderPath);

        var sanitizedName = SanitizeFileName(localization.Id ?? "unknown");
        var filePath = Path.Combine(folderPath, $"{sanitizedName}.json");

        var json = JsonSerializer.Serialize(localization, localization.GetType(), JsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);

        if (localization.Id != null)
        {
            migrationTable.AddOrUpdate(new MigrationEntry
            {
                ObjectType = "AzureBrandingLocalization",
                OriginalId = localization.Id,
                Name = localization.Id ?? "Unknown"
            });
        }
    }

    public async Task ExportAzureBrandingLocalizationsAsync(
        IEnumerable<OrganizationalBrandingLocalization> localizations,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var migrationTable = new MigrationTable();

        foreach (var localization in localizations)
        {
            await ExportAzureBrandingLocalizationAsync(localization, outputPath, migrationTable, cancellationToken);
        }

        await SaveMigrationTableAsync(migrationTable, outputPath, cancellationToken);
    }

    public async Task ExportAutopilotProfileAsync(
        WindowsAutopilotDeploymentProfile profile,
        string outputPath,
        MigrationTable migrationTable,
        CancellationToken cancellationToken = default)
    {
        var folderPath = Path.Combine(outputPath, "AutopilotProfiles");
        Directory.CreateDirectory(folderPath);

        var sanitizedName = SanitizeFileName(profile.DisplayName ?? profile.Id ?? "unknown");
        var filePath = Path.Combine(folderPath, $"{sanitizedName}.json");

        var json = JsonSerializer.Serialize(profile, profile.GetType(), JsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);

        if (profile.Id != null)
        {
            migrationTable.AddOrUpdate(new MigrationEntry
            {
                ObjectType = "AutopilotProfile",
                OriginalId = profile.Id,
                Name = profile.DisplayName ?? "Unknown"
            });
        }
    }

    public async Task ExportAutopilotProfilesAsync(
        IEnumerable<WindowsAutopilotDeploymentProfile> profiles,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var migrationTable = new MigrationTable();

        foreach (var profile in profiles)
        {
            await ExportAutopilotProfileAsync(profile, outputPath, migrationTable, cancellationToken);
        }

        await SaveMigrationTableAsync(migrationTable, outputPath, cancellationToken);
    }

    public async Task ExportDeviceHealthScriptAsync(
        DeviceHealthScript script,
        string outputPath,
        MigrationTable migrationTable,
        CancellationToken cancellationToken = default)
    {
        var folderPath = Path.Combine(outputPath, "DeviceHealthScripts");
        Directory.CreateDirectory(folderPath);

        var sanitizedName = SanitizeFileName(script.DisplayName ?? script.Id ?? "unknown");
        var filePath = Path.Combine(folderPath, $"{sanitizedName}.json");

        var json = JsonSerializer.Serialize(script, script.GetType(), JsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);

        if (script.Id != null)
        {
            migrationTable.AddOrUpdate(new MigrationEntry
            {
                ObjectType = "DeviceHealthScript",
                OriginalId = script.Id,
                Name = script.DisplayName ?? "Unknown"
            });
        }
    }

    public async Task ExportDeviceHealthScriptsAsync(
        IEnumerable<DeviceHealthScript> scripts,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var migrationTable = new MigrationTable();

        foreach (var script in scripts)
        {
            await ExportDeviceHealthScriptAsync(script, outputPath, migrationTable, cancellationToken);
        }

        await SaveMigrationTableAsync(migrationTable, outputPath, cancellationToken);
    }

    public async Task ExportMacCustomAttributeAsync(
        DeviceCustomAttributeShellScript script,
        string outputPath,
        MigrationTable migrationTable,
        CancellationToken cancellationToken = default)
    {
        var folderPath = Path.Combine(outputPath, "MacCustomAttributes");
        Directory.CreateDirectory(folderPath);

        var sanitizedName = SanitizeFileName(script.DisplayName ?? script.Id ?? "unknown");
        var filePath = Path.Combine(folderPath, $"{sanitizedName}.json");

        var json = JsonSerializer.Serialize(script, script.GetType(), JsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);

        if (script.Id != null)
        {
            migrationTable.AddOrUpdate(new MigrationEntry
            {
                ObjectType = "MacCustomAttribute",
                OriginalId = script.Id,
                Name = script.DisplayName ?? "Unknown"
            });
        }
    }

    public async Task ExportMacCustomAttributesAsync(
        IEnumerable<DeviceCustomAttributeShellScript> scripts,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var migrationTable = new MigrationTable();

        foreach (var script in scripts)
        {
            await ExportMacCustomAttributeAsync(script, outputPath, migrationTable, cancellationToken);
        }

        await SaveMigrationTableAsync(migrationTable, outputPath, cancellationToken);
    }

    public async Task ExportFeatureUpdateProfileAsync(
        WindowsFeatureUpdateProfile profile,
        string outputPath,
        MigrationTable migrationTable,
        CancellationToken cancellationToken = default)
    {
        var folderPath = Path.Combine(outputPath, "FeatureUpdates");
        Directory.CreateDirectory(folderPath);

        var sanitizedName = SanitizeFileName(profile.DisplayName ?? profile.Id ?? "unknown");
        var filePath = Path.Combine(folderPath, $"{sanitizedName}.json");

        var json = JsonSerializer.Serialize(profile, profile.GetType(), JsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);

        if (profile.Id != null)
        {
            migrationTable.AddOrUpdate(new MigrationEntry
            {
                ObjectType = "FeatureUpdateProfile",
                OriginalId = profile.Id,
                Name = profile.DisplayName ?? "Unknown"
            });
        }
    }

    public async Task ExportFeatureUpdateProfilesAsync(
        IEnumerable<WindowsFeatureUpdateProfile> profiles,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var migrationTable = new MigrationTable();

        foreach (var profile in profiles)
        {
            await ExportFeatureUpdateProfileAsync(profile, outputPath, migrationTable, cancellationToken);
        }

        await SaveMigrationTableAsync(migrationTable, outputPath, cancellationToken);
    }

    public async Task ExportNamedLocationAsync(
        NamedLocation namedLocation,
        string outputPath,
        MigrationTable migrationTable,
        CancellationToken cancellationToken = default)
    {
        var folderPath = Path.Combine(outputPath, "NamedLocations");
        Directory.CreateDirectory(folderPath);

        var displayName = namedLocation.AdditionalData?.TryGetValue("displayName", out var value) == true
            ? value?.ToString()
            : null;

        var sanitizedName = SanitizeFileName(displayName ?? namedLocation.Id ?? "unknown");
        var filePath = Path.Combine(folderPath, $"{sanitizedName}.json");

        var json = JsonSerializer.Serialize(namedLocation, namedLocation.GetType(), JsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);

        if (namedLocation.Id != null)
        {
            migrationTable.AddOrUpdate(new MigrationEntry
            {
                ObjectType = "NamedLocation",
                OriginalId = namedLocation.Id,
                Name = displayName ?? "Unknown"
            });
        }
    }

    public async Task ExportNamedLocationsAsync(
        IEnumerable<NamedLocation> namedLocations,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var migrationTable = new MigrationTable();

        foreach (var namedLocation in namedLocations)
        {
            await ExportNamedLocationAsync(namedLocation, outputPath, migrationTable, cancellationToken);
        }

        await SaveMigrationTableAsync(migrationTable, outputPath, cancellationToken);
    }

    public async Task ExportAuthenticationStrengthPolicyAsync(
        AuthenticationStrengthPolicy policy,
        string outputPath,
        MigrationTable migrationTable,
        CancellationToken cancellationToken = default)
    {
        var folderPath = Path.Combine(outputPath, "AuthenticationStrengths");
        Directory.CreateDirectory(folderPath);

        var sanitizedName = SanitizeFileName(policy.DisplayName ?? policy.Id ?? "unknown");
        var filePath = Path.Combine(folderPath, $"{sanitizedName}.json");

        var json = JsonSerializer.Serialize(policy, policy.GetType(), JsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);

        if (policy.Id != null)
        {
            migrationTable.AddOrUpdate(new MigrationEntry
            {
                ObjectType = "AuthenticationStrengthPolicy",
                OriginalId = policy.Id,
                Name = policy.DisplayName ?? "Unknown"
            });
        }
    }

    public async Task ExportAuthenticationStrengthPoliciesAsync(
        IEnumerable<AuthenticationStrengthPolicy> policies,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var migrationTable = new MigrationTable();

        foreach (var policy in policies)
        {
            await ExportAuthenticationStrengthPolicyAsync(policy, outputPath, migrationTable, cancellationToken);
        }

        await SaveMigrationTableAsync(migrationTable, outputPath, cancellationToken);
    }

    public async Task ExportAuthenticationContextAsync(
        AuthenticationContextClassReference contextClassReference,
        string outputPath,
        MigrationTable migrationTable,
        CancellationToken cancellationToken = default)
    {
        var folderPath = Path.Combine(outputPath, "AuthenticationContexts");
        Directory.CreateDirectory(folderPath);

        var displayName = contextClassReference.DisplayName ?? contextClassReference.Id;
        var sanitizedName = SanitizeFileName(displayName ?? "unknown");
        var filePath = Path.Combine(folderPath, $"{sanitizedName}.json");

        var json = JsonSerializer.Serialize(contextClassReference, contextClassReference.GetType(), JsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);

        if (contextClassReference.Id != null)
        {
            migrationTable.AddOrUpdate(new MigrationEntry
            {
                ObjectType = "AuthenticationContext",
                OriginalId = contextClassReference.Id,
                Name = contextClassReference.DisplayName ?? "Unknown"
            });
        }
    }

    public async Task ExportAuthenticationContextsAsync(
        IEnumerable<AuthenticationContextClassReference> contextClassReferences,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var migrationTable = new MigrationTable();

        foreach (var contextClassReference in contextClassReferences)
        {
            await ExportAuthenticationContextAsync(contextClassReference, outputPath, migrationTable, cancellationToken);
        }

        await SaveMigrationTableAsync(migrationTable, outputPath, cancellationToken);
    }

    public async Task ExportTermsOfUseAgreementAsync(
        Agreement agreement,
        string outputPath,
        MigrationTable migrationTable,
        CancellationToken cancellationToken = default)
    {
        var folderPath = Path.Combine(outputPath, "TermsOfUse");
        Directory.CreateDirectory(folderPath);

        var sanitizedName = SanitizeFileName(agreement.DisplayName ?? agreement.Id ?? "unknown");
        var filePath = Path.Combine(folderPath, $"{sanitizedName}.json");

        var json = JsonSerializer.Serialize(agreement, agreement.GetType(), JsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);

        if (agreement.Id != null)
        {
            migrationTable.AddOrUpdate(new MigrationEntry
            {
                ObjectType = "TermsOfUseAgreement",
                OriginalId = agreement.Id,
                Name = agreement.DisplayName ?? "Unknown"
            });
        }
    }

    public async Task ExportTermsOfUseAgreementsAsync(
        IEnumerable<Agreement> agreements,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var migrationTable = new MigrationTable();

        foreach (var agreement in agreements)
        {
            await ExportTermsOfUseAgreementAsync(agreement, outputPath, migrationTable, cancellationToken);
        }

        await SaveMigrationTableAsync(migrationTable, outputPath, cancellationToken);
    }

    public async Task ExportDeviceManagementScriptAsync(
        DeviceManagementScript script,
        string outputPath,
        MigrationTable migrationTable,
        CancellationToken cancellationToken = default)
    {
        var folderPath = Path.Combine(outputPath, "DeviceManagementScripts");
        Directory.CreateDirectory(folderPath);

        var sanitizedName = SanitizeFileName(script.DisplayName ?? script.Id ?? "unknown");
        var filePath = Path.Combine(folderPath, $"{sanitizedName}.json");

        var json = JsonSerializer.Serialize(script, script.GetType(), JsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);

        if (script.Id != null)
        {
            migrationTable.AddOrUpdate(new MigrationEntry
            {
                ObjectType = "DeviceManagementScript",
                OriginalId = script.Id,
                Name = script.DisplayName ?? "Unknown"
            });
        }
    }

    public async Task ExportDeviceManagementScriptsAsync(
        IEnumerable<DeviceManagementScript> scripts,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var migrationTable = new MigrationTable();

        foreach (var script in scripts)
        {
            await ExportDeviceManagementScriptAsync(script, outputPath, migrationTable, cancellationToken);
        }

        await SaveMigrationTableAsync(migrationTable, outputPath, cancellationToken);
    }

    public async Task ExportDeviceShellScriptAsync(
        DeviceShellScript script,
        string outputPath,
        MigrationTable migrationTable,
        CancellationToken cancellationToken = default)
    {
        var folderPath = Path.Combine(outputPath, "DeviceShellScripts");
        Directory.CreateDirectory(folderPath);

        var sanitizedName = SanitizeFileName(script.DisplayName ?? script.Id ?? "unknown");
        var filePath = Path.Combine(folderPath, $"{sanitizedName}.json");

        var json = JsonSerializer.Serialize(script, script.GetType(), JsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);

        if (script.Id != null)
        {
            migrationTable.AddOrUpdate(new MigrationEntry
            {
                ObjectType = "DeviceShellScript",
                OriginalId = script.Id,
                Name = script.DisplayName ?? "Unknown"
            });
        }
    }

    public async Task ExportDeviceShellScriptsAsync(
        IEnumerable<DeviceShellScript> scripts,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var migrationTable = new MigrationTable();

        foreach (var script in scripts)
        {
            await ExportDeviceShellScriptAsync(script, outputPath, migrationTable, cancellationToken);
        }

        await SaveMigrationTableAsync(migrationTable, outputPath, cancellationToken);
    }

    public async Task ExportComplianceScriptAsync(
        DeviceComplianceScript script,
        string outputPath,
        MigrationTable migrationTable,
        CancellationToken cancellationToken = default)
    {
        var folderPath = Path.Combine(outputPath, "ComplianceScripts");
        Directory.CreateDirectory(folderPath);

        var sanitizedName = SanitizeFileName(script.DisplayName ?? script.Id ?? "unknown");
        var filePath = Path.Combine(folderPath, $"{sanitizedName}.json");

        var json = JsonSerializer.Serialize(script, script.GetType(), JsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);

        if (script.Id != null)
        {
            migrationTable.AddOrUpdate(new MigrationEntry
            {
                ObjectType = "ComplianceScript",
                OriginalId = script.Id,
                Name = script.DisplayName ?? "Unknown"
            });
        }
    }

    public async Task ExportComplianceScriptsAsync(
        IEnumerable<DeviceComplianceScript> scripts,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var migrationTable = new MigrationTable();

        foreach (var script in scripts)
        {
            await ExportComplianceScriptAsync(script, outputPath, migrationTable, cancellationToken);
        }

        await SaveMigrationTableAsync(migrationTable, outputPath, cancellationToken);
    }
}

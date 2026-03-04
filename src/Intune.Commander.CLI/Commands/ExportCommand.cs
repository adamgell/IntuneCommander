using Intune.Commander.CLI.Helpers;
using Intune.Commander.CLI.Models;
using Intune.Commander.Core.Auth;
using Intune.Commander.Core.Models;
using Intune.Commander.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace Intune.Commander.CLI.Commands;

public static class ExportCommand
{
    private static readonly string[] AllTypes =
    [
        "configurations", "compliance", "applications", "endpoint-security", "administrative-templates",
        "settings-catalog", "enrollment-configurations", "app-protection", "managed-device-app-configurations",
        "targeted-managed-app-configurations", "terms-and-conditions", "scope-tags", "role-definitions",
        "intune-branding", "azure-branding", "autopilot", "device-health-scripts", "mac-custom-attributes",
        "feature-updates", "named-locations", "authentication-strengths", "authentication-contexts",
        "terms-of-use", "device-management-scripts", "device-shell-scripts", "compliance-scripts",
        "quality-updates", "driver-updates"
    ];

    public static Command Build()
    {
        var command = new Command("export", "Export Intune configurations");

        var profile = new Option<string?>("--profile");
        var tenantId = new Option<string?>("--tenant-id");
        var clientId = new Option<string?>("--client-id");
        var secret = new Option<string?>("--secret");
        var cloud = new Option<string?>("--cloud");
        var output = new Option<string>("--output") { IsRequired = true };
        var types = new Option<string>("--types", () => "all", "Comma-separated list of types to export, or \"all\"");
        types.AddCompletions(ctx =>
        {
            // Suggest "all" plus each individual type not yet present in the token
            var current = ctx.WordToComplete ?? string.Empty;
            var candidates = new[] { "all" }.Concat(AllTypes);
            return candidates.Where(t => t.StartsWith(current, StringComparison.OrdinalIgnoreCase));
        });

        command.AddOption(profile);
        command.AddOption(tenantId);
        command.AddOption(clientId);
        command.AddOption(secret);
        command.AddOption(cloud);
        command.AddOption(output);
        command.AddOption(types);

        command.SetHandler(async context =>
        {
            await ExecuteAsync(
                context.ParseResult.GetValueForOption(profile),
                context.ParseResult.GetValueForOption(tenantId),
                context.ParseResult.GetValueForOption(clientId),
                context.ParseResult.GetValueForOption(secret),
                context.ParseResult.GetValueForOption(cloud),
                context.ParseResult.GetValueForOption(output)!,
                context.ParseResult.GetValueForOption(types) ?? "all",
                context.GetCancellationToken());
        });
        return command;
    }

    private static async Task ExecuteAsync(
        string? profile,
        string? tenantId,
        string? clientId,
        string? secret,
        string? cloud,
        string output,
        string types,
        CancellationToken cancellationToken)
    {
        using var provider = CliServices.CreateServiceProvider();
        var profileService = provider.GetRequiredService<ProfileService>();
        var graphClientFactory = provider.GetRequiredService<IntuneGraphClientFactory>();
        var exportService = provider.GetRequiredService<IExportService>();

        var resolvedProfile = await ProfileResolver.ResolveAsync(profileService, profile, tenantId, clientId, secret, cloud, cancellationToken);
        var graphClient = await graphClientFactory.CreateClientAsync(resolvedProfile, AuthHelper.DeviceCodeToStderr, cancellationToken);

        Directory.CreateDirectory(output);
        var selectedTypes = ParseTypes(types);
        var migrationTable = new MigrationTable();
        var count = 0;

        var configurationProfileService = new ConfigurationProfileService(graphClient);
        var compliancePolicyService = new CompliancePolicyService(graphClient);
        var applicationService = new ApplicationService(graphClient);
        var endpointSecurityService = new EndpointSecurityService(graphClient);
        var administrativeTemplateService = new AdministrativeTemplateService(graphClient);
        var settingsCatalogService = new SettingsCatalogService(graphClient);
        var enrollmentConfigurationService = new EnrollmentConfigurationService(graphClient);
        var appProtectionPolicyService = new AppProtectionPolicyService(graphClient);
        var managedAppConfigurationService = new ManagedAppConfigurationService(graphClient);
        var termsAndConditionsService = new TermsAndConditionsService(graphClient);
        var scopeTagService = new ScopeTagService(graphClient);
        var roleDefinitionService = new RoleDefinitionService(graphClient);
        var intuneBrandingService = new IntuneBrandingService(graphClient);
        var azureBrandingService = new AzureBrandingService(graphClient);
        var autopilotService = new AutopilotService(graphClient);
        var deviceHealthScriptService = new DeviceHealthScriptService(graphClient);
        var macCustomAttributeService = new MacCustomAttributeService(graphClient);
        var featureUpdateProfileService = new FeatureUpdateProfileService(graphClient);
        var namedLocationService = new NamedLocationService(graphClient);
        var authenticationStrengthService = new AuthenticationStrengthService(graphClient);
        var authenticationContextService = new AuthenticationContextService(graphClient);
        var termsOfUseService = new TermsOfUseService(graphClient);
        var deviceManagementScriptService = new DeviceManagementScriptService(graphClient);
        var deviceShellScriptService = new DeviceShellScriptService(graphClient);
        var complianceScriptService = new ComplianceScriptService(graphClient);
        var qualityUpdateProfileService = new QualityUpdateProfileService(graphClient);
        var driverUpdateProfileService = new DriverUpdateProfileService(graphClient);

        if (selectedTypes.Contains("configurations"))
        {
            Console.Error.WriteLine("Exporting device configurations...");
            var items = await configurationProfileService.ListDeviceConfigurationsAsync(cancellationToken);
            foreach (var item in items)
            {
                await exportService.ExportDeviceConfigurationAsync(item, output, migrationTable, cancellationToken);
                count++;
            }
        }

        if (selectedTypes.Contains("compliance"))
        {
            Console.Error.WriteLine("Exporting compliance policies...");
            var items = await compliancePolicyService.ListCompliancePoliciesAsync(cancellationToken);
            foreach (var item in items)
            {
                var assignments = item.Id is null ? [] : await compliancePolicyService.GetAssignmentsAsync(item.Id, cancellationToken);
                await exportService.ExportCompliancePolicyAsync(item, assignments, output, migrationTable, cancellationToken);
                count++;
            }
        }

        if (selectedTypes.Contains("applications"))
        {
            Console.Error.WriteLine("Exporting applications...");
            var items = await applicationService.ListApplicationsAsync(cancellationToken);
            foreach (var item in items)
            {
                var assignments = item.Id is null ? [] : await applicationService.GetAssignmentsAsync(item.Id, cancellationToken);
                await exportService.ExportApplicationAsync(item, assignments, output, migrationTable, cancellationToken);
                count++;
            }
        }

        if (selectedTypes.Contains("endpoint-security"))
        {
            Console.Error.WriteLine("Exporting endpoint security intents...");
            var items = await endpointSecurityService.ListEndpointSecurityIntentsAsync(cancellationToken);
            foreach (var item in items)
            {
                var assignments = item.Id is null ? [] : await endpointSecurityService.GetAssignmentsAsync(item.Id, cancellationToken);
                await exportService.ExportEndpointSecurityIntentAsync(item, assignments, output, migrationTable, cancellationToken);
                count++;
            }
        }

        if (selectedTypes.Contains("administrative-templates"))
        {
            Console.Error.WriteLine("Exporting administrative templates...");
            var items = await administrativeTemplateService.ListAdministrativeTemplatesAsync(cancellationToken);
            foreach (var item in items)
            {
                var assignments = item.Id is null ? [] : await administrativeTemplateService.GetAssignmentsAsync(item.Id, cancellationToken);
                await exportService.ExportAdministrativeTemplateAsync(item, assignments, output, migrationTable, cancellationToken);
                count++;
            }
        }

        if (selectedTypes.Contains("settings-catalog"))
        {
            Console.Error.WriteLine("Exporting settings catalog policies...");
            var items = await settingsCatalogService.ListSettingsCatalogPoliciesAsync(cancellationToken);
            foreach (var item in items)
            {
                var settings = item.Id is null ? [] : await settingsCatalogService.GetPolicySettingsAsync(item.Id, cancellationToken);
                var assignments = item.Id is null ? [] : await settingsCatalogService.GetAssignmentsAsync(item.Id, cancellationToken);
                await exportService.ExportSettingsCatalogPolicyAsync(item, settings, assignments, output, migrationTable, cancellationToken);
                count++;
            }
        }

        if (selectedTypes.Contains("enrollment-configurations"))
        {
            Console.Error.WriteLine("Exporting enrollment configurations...");
            var items = await enrollmentConfigurationService.ListEnrollmentConfigurationsAsync(cancellationToken);
            foreach (var item in items)
            {
                await exportService.ExportEnrollmentConfigurationAsync(item, output, migrationTable, cancellationToken);
                count++;
            }
        }

        if (selectedTypes.Contains("app-protection"))
        {
            Console.Error.WriteLine("Exporting app protection policies...");
            var items = await appProtectionPolicyService.ListAppProtectionPoliciesAsync(cancellationToken);
            foreach (var item in items)
            {
                await exportService.ExportAppProtectionPolicyAsync(item, output, migrationTable, cancellationToken);
                count++;
            }
        }

        if (selectedTypes.Contains("managed-device-app-configurations"))
        {
            Console.Error.WriteLine("Exporting managed device app configurations...");
            var items = await managedAppConfigurationService.ListManagedDeviceAppConfigurationsAsync(cancellationToken);
            foreach (var item in items)
            {
                await exportService.ExportManagedDeviceAppConfigurationAsync(item, output, migrationTable, cancellationToken);
                count++;
            }
        }

        if (selectedTypes.Contains("targeted-managed-app-configurations"))
        {
            Console.Error.WriteLine("Exporting targeted managed app configurations...");
            var items = await managedAppConfigurationService.ListTargetedManagedAppConfigurationsAsync(cancellationToken);
            foreach (var item in items)
            {
                await exportService.ExportTargetedManagedAppConfigurationAsync(item, output, migrationTable, cancellationToken);
                count++;
            }
        }

        if (selectedTypes.Contains("terms-and-conditions"))
        {
            Console.Error.WriteLine("Exporting terms and conditions...");
            var items = await termsAndConditionsService.ListTermsAndConditionsAsync(cancellationToken);
            foreach (var item in items)
            {
                await exportService.ExportTermsAndConditionsAsync(item, output, migrationTable, cancellationToken);
                count++;
            }
        }

        if (selectedTypes.Contains("scope-tags"))
        {
            Console.Error.WriteLine("Exporting scope tags...");
            var items = await scopeTagService.ListScopeTagsAsync(cancellationToken);
            foreach (var item in items)
            {
                await exportService.ExportScopeTagAsync(item, output, migrationTable, cancellationToken);
                count++;
            }
        }

        if (selectedTypes.Contains("role-definitions"))
        {
            Console.Error.WriteLine("Exporting role definitions...");
            var items = await roleDefinitionService.ListRoleDefinitionsAsync(cancellationToken);
            foreach (var item in items)
            {
                await exportService.ExportRoleDefinitionAsync(item, output, migrationTable, cancellationToken);
                count++;
            }
        }

        if (selectedTypes.Contains("intune-branding"))
        {
            Console.Error.WriteLine("Exporting Intune branding profiles...");
            var items = await intuneBrandingService.ListIntuneBrandingProfilesAsync(cancellationToken);
            foreach (var item in items)
            {
                await exportService.ExportIntuneBrandingProfileAsync(item, output, migrationTable, cancellationToken);
                count++;
            }
        }

        if (selectedTypes.Contains("azure-branding"))
        {
            Console.Error.WriteLine("Exporting Azure branding localizations...");
            var items = await azureBrandingService.ListBrandingLocalizationsAsync(cancellationToken);
            foreach (var item in items)
            {
                await exportService.ExportAzureBrandingLocalizationAsync(item, output, migrationTable, cancellationToken);
                count++;
            }
        }

        if (selectedTypes.Contains("autopilot"))
        {
            Console.Error.WriteLine("Exporting autopilot profiles...");
            var items = await autopilotService.ListAutopilotProfilesAsync(cancellationToken);
            foreach (var item in items)
            {
                await exportService.ExportAutopilotProfileAsync(item, output, migrationTable, cancellationToken);
                count++;
            }
        }

        if (selectedTypes.Contains("device-health-scripts"))
        {
            Console.Error.WriteLine("Exporting device health scripts...");
            var items = await deviceHealthScriptService.ListDeviceHealthScriptsAsync(cancellationToken);
            foreach (var item in items)
            {
                await exportService.ExportDeviceHealthScriptAsync(item, output, migrationTable, cancellationToken);
                count++;
            }
        }

        if (selectedTypes.Contains("mac-custom-attributes"))
        {
            Console.Error.WriteLine("Exporting mac custom attributes...");
            var items = await macCustomAttributeService.ListMacCustomAttributesAsync(cancellationToken);
            foreach (var item in items)
            {
                await exportService.ExportMacCustomAttributeAsync(item, output, migrationTable, cancellationToken);
                count++;
            }
        }

        if (selectedTypes.Contains("feature-updates"))
        {
            Console.Error.WriteLine("Exporting feature update profiles...");
            var items = await featureUpdateProfileService.ListFeatureUpdateProfilesAsync(cancellationToken);
            foreach (var item in items)
            {
                await exportService.ExportFeatureUpdateProfileAsync(item, output, migrationTable, cancellationToken);
                count++;
            }
        }

        if (selectedTypes.Contains("named-locations"))
        {
            Console.Error.WriteLine("Exporting named locations...");
            var items = await namedLocationService.ListNamedLocationsAsync(cancellationToken);
            foreach (var item in items)
            {
                await exportService.ExportNamedLocationAsync(item, output, migrationTable, cancellationToken);
                count++;
            }
        }

        if (selectedTypes.Contains("authentication-strengths"))
        {
            Console.Error.WriteLine("Exporting authentication strength policies...");
            var items = await authenticationStrengthService.ListAuthenticationStrengthPoliciesAsync(cancellationToken);
            foreach (var item in items)
            {
                await exportService.ExportAuthenticationStrengthPolicyAsync(item, output, migrationTable, cancellationToken);
                count++;
            }
        }

        if (selectedTypes.Contains("authentication-contexts"))
        {
            Console.Error.WriteLine("Exporting authentication contexts...");
            var items = await authenticationContextService.ListAuthenticationContextsAsync(cancellationToken);
            foreach (var item in items)
            {
                await exportService.ExportAuthenticationContextAsync(item, output, migrationTable, cancellationToken);
                count++;
            }
        }

        if (selectedTypes.Contains("terms-of-use"))
        {
            Console.Error.WriteLine("Exporting terms of use agreements...");
            var items = await termsOfUseService.ListTermsOfUseAgreementsAsync(cancellationToken);
            foreach (var item in items)
            {
                await exportService.ExportTermsOfUseAgreementAsync(item, output, migrationTable, cancellationToken);
                count++;
            }
        }

        if (selectedTypes.Contains("device-management-scripts"))
        {
            Console.Error.WriteLine("Exporting device management scripts...");
            var items = await deviceManagementScriptService.ListDeviceManagementScriptsAsync(cancellationToken);
            foreach (var item in items)
            {
                await exportService.ExportDeviceManagementScriptAsync(item, output, migrationTable, cancellationToken);
                count++;
            }
        }

        if (selectedTypes.Contains("device-shell-scripts"))
        {
            Console.Error.WriteLine("Exporting device shell scripts...");
            var items = await deviceShellScriptService.ListDeviceShellScriptsAsync(cancellationToken);
            foreach (var item in items)
            {
                await exportService.ExportDeviceShellScriptAsync(item, output, migrationTable, cancellationToken);
                count++;
            }
        }

        if (selectedTypes.Contains("compliance-scripts"))
        {
            Console.Error.WriteLine("Exporting compliance scripts...");
            var items = await complianceScriptService.ListComplianceScriptsAsync(cancellationToken);
            foreach (var item in items)
            {
                await exportService.ExportComplianceScriptAsync(item, output, migrationTable, cancellationToken);
                count++;
            }
        }

        if (selectedTypes.Contains("quality-updates"))
        {
            Console.Error.WriteLine("Exporting quality update profiles...");
            var items = await qualityUpdateProfileService.ListQualityUpdateProfilesAsync(cancellationToken);
            foreach (var item in items)
            {
                await exportService.ExportQualityUpdateProfileAsync(item, output, migrationTable, cancellationToken);
                count++;
            }
        }

        if (selectedTypes.Contains("driver-updates"))
        {
            Console.Error.WriteLine("Exporting driver update profiles...");
            var items = await driverUpdateProfileService.ListDriverUpdateProfilesAsync(cancellationToken);
            foreach (var item in items)
            {
                await exportService.ExportDriverUpdateProfileAsync(item, output, migrationTable, cancellationToken);
                count++;
            }
        }

        await exportService.SaveMigrationTableAsync(migrationTable, output, cancellationToken);
        OutputFormatter.WriteJsonToStdout(new CommandResult { Command = "export", Count = count, Path = output, DryRun = false });
    }

    private static HashSet<string> ParseTypes(string types)
    {
        if (string.Equals(types, "all", StringComparison.OrdinalIgnoreCase))
            return [.. AllTypes];

        var selected = types
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var invalid = selected.Where(t => !AllTypes.Contains(t, StringComparer.OrdinalIgnoreCase)).ToArray();
        if (invalid.Length > 0)
            throw new InvalidOperationException($"Unsupported --types value(s): {string.Join(", ", invalid)}");

        return selected;
    }
}

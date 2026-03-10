using Azure.Identity;
using Intune.Commander.CLI.Helpers;
using Intune.Commander.CLI.Models;
using Intune.Commander.Core.Auth;
using Intune.Commander.Core.Models;
using Intune.Commander.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph.Beta.Models;
using Microsoft.Kiota.Abstractions;
using System.CommandLine;
using System.Text.Json;
using CliCommand = System.CommandLine.Command;

namespace Intune.Commander.CLI.Commands;

public static class ImportCommand
{
    private static readonly string[] SummaryKeys =
    [
        "deviceConfigurations",
        "compliancePolicies",
        "endpointSecurityIntents",
        "administrativeTemplates",
        "enrollmentConfigurations",
        "appProtectionPolicies",
        "managedDeviceAppConfigurations",
        "targetedManagedAppConfigurations",
        "termsAndConditions",
        "scopeTags",
        "roleDefinitions",
        "intuneBrandingProfiles",
        "azureBrandingLocalizations",
        "autopilotProfiles",
        "deviceHealthScripts",
        "macCustomAttributes",
        "featureUpdateProfiles",
        "namedLocations",
        "authenticationStrengthPolicies",
        "authenticationContexts",
        "termsOfUseAgreements",
        "deviceManagementScripts",
        "deviceShellScripts",
        "complianceScripts",
        "qualityUpdateProfiles",
        "driverUpdateProfiles",
        "settingsCatalogPolicies"
    ];

    private static readonly DryRunValidationTarget[] DryRunValidationTargets =
    [
        new("deviceConfigurations", "DeviceConfigurations", static (service, filePath, cancellationToken) => ReadAsObjectAsync(service.ReadDeviceConfigurationAsync, filePath, cancellationToken)),
        new("compliancePolicies", "CompliancePolicies", static (service, filePath, cancellationToken) => ReadAsObjectAsync(service.ReadCompliancePolicyAsync, filePath, cancellationToken)),
        new("endpointSecurityIntents", "EndpointSecurity", static (service, filePath, cancellationToken) => ReadAsObjectAsync(service.ReadEndpointSecurityIntentAsync, filePath, cancellationToken)),
        new("administrativeTemplates", "AdministrativeTemplates", static (service, filePath, cancellationToken) => ReadAsObjectAsync(service.ReadAdministrativeTemplateAsync, filePath, cancellationToken)),
        new("enrollmentConfigurations", "EnrollmentConfigurations", static (service, filePath, cancellationToken) => ReadAsObjectAsync(service.ReadEnrollmentConfigurationAsync, filePath, cancellationToken)),
        new("appProtectionPolicies", "AppProtectionPolicies", static (service, filePath, cancellationToken) => ReadAsObjectAsync(service.ReadAppProtectionPolicyAsync, filePath, cancellationToken)),
        new("managedDeviceAppConfigurations", "ManagedDeviceAppConfigurations", static (service, filePath, cancellationToken) => ReadAsObjectAsync(service.ReadManagedDeviceAppConfigurationAsync, filePath, cancellationToken)),
        new("targetedManagedAppConfigurations", "TargetedManagedAppConfigurations", static (service, filePath, cancellationToken) => ReadAsObjectAsync(service.ReadTargetedManagedAppConfigurationAsync, filePath, cancellationToken)),
        new("termsAndConditions", "TermsAndConditions", static (service, filePath, cancellationToken) => ReadAsObjectAsync(service.ReadTermsAndConditionsAsync, filePath, cancellationToken)),
        new("scopeTags", "ScopeTags", static (service, filePath, cancellationToken) => ReadAsObjectAsync(service.ReadScopeTagAsync, filePath, cancellationToken)),
        new("roleDefinitions", "RoleDefinitions", static (service, filePath, cancellationToken) => ReadAsObjectAsync(service.ReadRoleDefinitionAsync, filePath, cancellationToken)),
        new("intuneBrandingProfiles", "IntuneBrandingProfiles", static (service, filePath, cancellationToken) => ReadAsObjectAsync(service.ReadIntuneBrandingProfileAsync, filePath, cancellationToken)),
        new("azureBrandingLocalizations", "AzureBrandingLocalizations", static (service, filePath, cancellationToken) => ReadAsObjectAsync(service.ReadAzureBrandingLocalizationAsync, filePath, cancellationToken)),
        new("autopilotProfiles", "AutopilotProfiles", static (service, filePath, cancellationToken) => ReadAsObjectAsync(service.ReadAutopilotProfileAsync, filePath, cancellationToken)),
        new("deviceHealthScripts", "DeviceHealthScripts", static (service, filePath, cancellationToken) => ReadAsObjectAsync(service.ReadDeviceHealthScriptAsync, filePath, cancellationToken)),
        new("macCustomAttributes", "MacCustomAttributes", static (service, filePath, cancellationToken) => ReadAsObjectAsync(service.ReadMacCustomAttributeAsync, filePath, cancellationToken)),
        new("featureUpdateProfiles", "FeatureUpdates", static (service, filePath, cancellationToken) => ReadAsObjectAsync(service.ReadFeatureUpdateProfileAsync, filePath, cancellationToken)),
        new("namedLocations", "NamedLocations", static (service, filePath, cancellationToken) => ReadAsObjectAsync(service.ReadNamedLocationAsync, filePath, cancellationToken)),
        new("authenticationStrengthPolicies", "AuthenticationStrengths", static (service, filePath, cancellationToken) => ReadAsObjectAsync(service.ReadAuthenticationStrengthPolicyAsync, filePath, cancellationToken)),
        new("authenticationContexts", "AuthenticationContexts", static (service, filePath, cancellationToken) => ReadAsObjectAsync(service.ReadAuthenticationContextAsync, filePath, cancellationToken)),
        new("termsOfUseAgreements", "TermsOfUse", static (service, filePath, cancellationToken) => ReadAsObjectAsync(service.ReadTermsOfUseAgreementAsync, filePath, cancellationToken)),
        new("deviceManagementScripts", "DeviceManagementScripts", static (service, filePath, cancellationToken) => ReadAsObjectAsync(service.ReadDeviceManagementScriptAsync, filePath, cancellationToken)),
        new("deviceShellScripts", "DeviceShellScripts", static (service, filePath, cancellationToken) => ReadAsObjectAsync(service.ReadDeviceShellScriptAsync, filePath, cancellationToken)),
        new("complianceScripts", "ComplianceScripts", static (service, filePath, cancellationToken) => ReadAsObjectAsync(service.ReadComplianceScriptAsync, filePath, cancellationToken)),
        new("qualityUpdateProfiles", "QualityUpdates", static (service, filePath, cancellationToken) => ReadAsObjectAsync(service.ReadQualityUpdateProfileAsync, filePath, cancellationToken)),
        new("driverUpdateProfiles", "DriverUpdates", static (service, filePath, cancellationToken) => ReadAsObjectAsync(service.ReadDriverUpdateProfileAsync, filePath, cancellationToken)),
        new("settingsCatalogPolicies", "SettingsCatalog", static (service, filePath, cancellationToken) => ReadAsObjectAsync(service.ReadSettingsCatalogPolicyAsync, filePath, cancellationToken))
    ];

    public static CliCommand Build()
    {
        var command = new CliCommand("import", "Import Intune configurations from an export folder");

        var profile = new Option<string?>("--profile");
        var tenantId = new Option<string?>("--tenant-id");
        var clientId = new Option<string?>("--client-id");
        var secret = new Option<string?>("--secret");
        var cloud = new Option<string?>("--cloud");
        var folder = new Option<string>("--folder") { IsRequired = true };
        var dryRun = new Option<bool>("--dry-run");

        command.AddOption(profile);
        command.AddOption(tenantId);
        command.AddOption(clientId);
        command.AddOption(secret);
        command.AddOption(cloud);
        command.AddOption(folder);
        command.AddOption(dryRun);

        command.SetHandler(async context =>
        {
            context.ExitCode = await ExecuteAsync(
                context.ParseResult.GetValueForOption(profile),
                context.ParseResult.GetValueForOption(tenantId),
                context.ParseResult.GetValueForOption(clientId),
                context.ParseResult.GetValueForOption(secret),
                context.ParseResult.GetValueForOption(cloud),
                context.ParseResult.GetValueForOption(folder)!,
                context.ParseResult.GetValueForOption(dryRun),
                context.GetCancellationToken());
        });

        return command;
    }

    private static async Task<int> ExecuteAsync(
        string? profile,
        string? tenantId,
        string? clientId,
        string? secret,
        string? cloud,
        string folder,
        bool dryRun,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(folder))
        {
            Console.Error.WriteLine($"Import folder \"{folder}\" was not found.");
            return 1;
        }

        if (dryRun)
        {
            var dryRunImportService = new ImportService(new DryRunConfigurationProfileService());
            var dryRunOutput = await ValidateFolderAsync(dryRunImportService, folder, cancellationToken);
            OutputFormatter.WriteJsonToStdout(dryRunOutput);
            if (dryRunOutput.Summary.ValidationErrorCount > 0)
            {
                Console.Error.WriteLine($"Dry-run validation found {dryRunOutput.Summary.ValidationErrorCount} invalid file(s).");
                return 1;
            }

            return 0;
        }

        try
        {
            using var provider = CliServices.CreateServiceProvider();
            var profileService = provider.GetRequiredService<ProfileService>();
            var graphClientFactory = provider.GetRequiredService<IntuneGraphClientFactory>();
            var exportService = provider.GetRequiredService<IExportService>();

            var resolvedProfile = await ProfileResolver.ResolveAsync(profileService, profile, tenantId, clientId, secret, cloud, cancellationToken);
            var graphClient = await graphClientFactory.CreateClientAsync(resolvedProfile, AuthHelper.DeviceCodeToStderr, cancellationToken);

            var importService = new ImportService(
                new ConfigurationProfileService(graphClient),
                new CompliancePolicyService(graphClient),
                new EndpointSecurityService(graphClient),
                new AdministrativeTemplateService(graphClient),
                new EnrollmentConfigurationService(graphClient),
                new AppProtectionPolicyService(graphClient),
                new ManagedAppConfigurationService(graphClient),
                new TermsAndConditionsService(graphClient),
                new ScopeTagService(graphClient),
                new RoleDefinitionService(graphClient),
                new IntuneBrandingService(graphClient),
                new AzureBrandingService(graphClient),
                new AutopilotService(graphClient),
                new DeviceHealthScriptService(graphClient),
                new MacCustomAttributeService(graphClient),
                new FeatureUpdateProfileService(graphClient),
                new NamedLocationService(graphClient),
                new AuthenticationStrengthService(graphClient),
                new AuthenticationContextService(graphClient),
                new TermsOfUseService(graphClient),
                new DeviceManagementScriptService(graphClient),
                new DeviceShellScriptService(graphClient),
                new ComplianceScriptService(graphClient),
                new QualityUpdateProfileService(graphClient),
                new DriverUpdateProfileService(graphClient),
                new SettingsCatalogService(graphClient));

            return await ExecuteImportAsync(importService, exportService, folder, cancellationToken);
        }
        catch (Exception ex) when (ShouldCaptureImportException(ex))
        {
            Console.Error.WriteLine(FormatImportFailureMessage(ex));
            return 1;
        }
    }

    internal static async Task<int> ExecuteImportAsync(
        IImportService importService,
        IExportService exportService,
        string folder,
        CancellationToken cancellationToken,
        TextWriter? stdout = null,
        TextWriter? stderr = null)
    {
        try
        {
            var output = await ProcessFolderAsync(importService, folder, dryRun: false, exportService, cancellationToken);
            OutputFormatter.WriteJsonToStdout(output, stdout);
            return 0;
        }
        catch (Exception ex) when (ShouldCaptureImportException(ex))
        {
            (stderr ?? Console.Error).WriteLine(FormatImportFailureMessage(ex));
            return 1;
        }
    }

    private static async Task<ImportCommandOutput> ValidateFolderAsync(
        IImportService importService,
        string folder,
        CancellationToken cancellationToken)
    {
        var perTypeCounts = CreatePerTypeCounts();
        var validationErrors = new List<ImportValidationError>();
        var migrationTable = await ReadMigrationTableForDryRunAsync(importService, folder, validationErrors, cancellationToken);
        var validItems = 0;

        foreach (var target in DryRunValidationTargets)
            validItems += await ValidateBatchAsync(importService, folder, target, perTypeCounts, validationErrors, cancellationToken);

        return CreateOutput(folder, dryRun: true, validItems, migrationTable, perTypeCounts, validationErrors);
    }

    private static async Task<ImportCommandOutput> ProcessFolderAsync(
        IImportService importService,
        string folder,
        bool dryRun,
        IExportService? exportService,
        CancellationToken cancellationToken)
    {
        var migrationTable = await importService.ReadMigrationTableAsync(folder, cancellationToken);
        var perTypeCounts = CreatePerTypeCounts();
        var imported = 0;

        imported += await ProcessBatchAsync(
            perTypeCounts,
            "deviceConfigurations",
            () => importService.ReadDeviceConfigurationsFromFolderAsync(folder, cancellationToken),
            dryRun ? null : WrapImporter<DeviceConfiguration, DeviceConfiguration>(importService.ImportDeviceConfigurationAsync),
            migrationTable,
            cancellationToken);

        imported += await ProcessBatchAsync(
            perTypeCounts,
            "compliancePolicies",
            () => importService.ReadCompliancePoliciesFromFolderAsync(folder, cancellationToken),
            dryRun ? null : WrapImporter<CompliancePolicyExport, DeviceCompliancePolicy>(importService.ImportCompliancePolicyAsync),
            migrationTable,
            cancellationToken);

        imported += await ProcessBatchAsync(
            perTypeCounts,
            "endpointSecurityIntents",
            () => importService.ReadEndpointSecurityIntentsFromFolderAsync(folder, cancellationToken),
            dryRun ? null : WrapImporter<EndpointSecurityExport, DeviceManagementIntent>(importService.ImportEndpointSecurityIntentAsync),
            migrationTable,
            cancellationToken);

        imported += await ProcessBatchAsync(
            perTypeCounts,
            "administrativeTemplates",
            () => importService.ReadAdministrativeTemplatesFromFolderAsync(folder, cancellationToken),
            dryRun ? null : WrapImporter<AdministrativeTemplateExport, GroupPolicyConfiguration>(importService.ImportAdministrativeTemplateAsync),
            migrationTable,
            cancellationToken);

        imported += await ProcessBatchAsync(
            perTypeCounts,
            "enrollmentConfigurations",
            () => importService.ReadEnrollmentConfigurationsFromFolderAsync(folder, cancellationToken),
            dryRun ? null : WrapImporter<DeviceEnrollmentConfiguration, DeviceEnrollmentConfiguration>(importService.ImportEnrollmentConfigurationAsync),
            migrationTable,
            cancellationToken);

        imported += await ProcessBatchAsync(
            perTypeCounts,
            "appProtectionPolicies",
            () => importService.ReadAppProtectionPoliciesFromFolderAsync(folder, cancellationToken),
            dryRun ? null : WrapImporter<ManagedAppPolicy, ManagedAppPolicy>(importService.ImportAppProtectionPolicyAsync),
            migrationTable,
            cancellationToken);

        imported += await ProcessBatchAsync(
            perTypeCounts,
            "managedDeviceAppConfigurations",
            () => importService.ReadManagedDeviceAppConfigurationsFromFolderAsync(folder, cancellationToken),
            dryRun ? null : WrapImporter<ManagedDeviceMobileAppConfiguration, ManagedDeviceMobileAppConfiguration>(importService.ImportManagedDeviceAppConfigurationAsync),
            migrationTable,
            cancellationToken);

        imported += await ProcessBatchAsync(
            perTypeCounts,
            "targetedManagedAppConfigurations",
            () => importService.ReadTargetedManagedAppConfigurationsFromFolderAsync(folder, cancellationToken),
            dryRun ? null : WrapImporter<TargetedManagedAppConfiguration, TargetedManagedAppConfiguration>(importService.ImportTargetedManagedAppConfigurationAsync),
            migrationTable,
            cancellationToken);

        imported += await ProcessBatchAsync(
            perTypeCounts,
            "termsAndConditions",
            () => importService.ReadTermsAndConditionsFromFolderAsync(folder, cancellationToken),
            dryRun ? null : WrapImporter<TermsAndConditions, TermsAndConditions>(importService.ImportTermsAndConditionsAsync),
            migrationTable,
            cancellationToken);

        imported += await ProcessBatchAsync(
            perTypeCounts,
            "scopeTags",
            () => importService.ReadScopeTagsFromFolderAsync(folder, cancellationToken),
            dryRun ? null : WrapImporter<RoleScopeTag, RoleScopeTag>(importService.ImportScopeTagAsync),
            migrationTable,
            cancellationToken);

        imported += await ProcessBatchAsync(
            perTypeCounts,
            "roleDefinitions",
            () => importService.ReadRoleDefinitionsFromFolderAsync(folder, cancellationToken),
            dryRun ? null : WrapImporter<RoleDefinition, RoleDefinition>(importService.ImportRoleDefinitionAsync),
            migrationTable,
            cancellationToken);

        imported += await ProcessBatchAsync(
            perTypeCounts,
            "intuneBrandingProfiles",
            () => importService.ReadIntuneBrandingProfilesFromFolderAsync(folder, cancellationToken),
            dryRun ? null : WrapImporter<IntuneBrandingProfile, IntuneBrandingProfile>(importService.ImportIntuneBrandingProfileAsync),
            migrationTable,
            cancellationToken);

        imported += await ProcessBatchAsync(
            perTypeCounts,
            "azureBrandingLocalizations",
            () => importService.ReadAzureBrandingLocalizationsFromFolderAsync(folder, cancellationToken),
            dryRun ? null : WrapImporter<OrganizationalBrandingLocalization, OrganizationalBrandingLocalization>(importService.ImportAzureBrandingLocalizationAsync),
            migrationTable,
            cancellationToken);

        imported += await ProcessBatchAsync(
            perTypeCounts,
            "autopilotProfiles",
            () => importService.ReadAutopilotProfilesFromFolderAsync(folder, cancellationToken),
            dryRun ? null : WrapImporter<WindowsAutopilotDeploymentProfile, WindowsAutopilotDeploymentProfile>(importService.ImportAutopilotProfileAsync),
            migrationTable,
            cancellationToken);

        imported += await ProcessBatchAsync(
            perTypeCounts,
            "deviceHealthScripts",
            () => importService.ReadDeviceHealthScriptsFromFolderAsync(folder, cancellationToken),
            dryRun ? null : WrapImporter<DeviceHealthScript, DeviceHealthScript>(importService.ImportDeviceHealthScriptAsync),
            migrationTable,
            cancellationToken);

        imported += await ProcessBatchAsync(
            perTypeCounts,
            "macCustomAttributes",
            () => importService.ReadMacCustomAttributesFromFolderAsync(folder, cancellationToken),
            dryRun ? null : WrapImporter<DeviceCustomAttributeShellScript, DeviceCustomAttributeShellScript>(importService.ImportMacCustomAttributeAsync),
            migrationTable,
            cancellationToken);

        imported += await ProcessBatchAsync(
            perTypeCounts,
            "featureUpdateProfiles",
            () => importService.ReadFeatureUpdateProfilesFromFolderAsync(folder, cancellationToken),
            dryRun ? null : WrapImporter<WindowsFeatureUpdateProfile, WindowsFeatureUpdateProfile>(importService.ImportFeatureUpdateProfileAsync),
            migrationTable,
            cancellationToken);

        imported += await ProcessBatchAsync(
            perTypeCounts,
            "namedLocations",
            () => importService.ReadNamedLocationsFromFolderAsync(folder, cancellationToken),
            dryRun ? null : WrapImporter<NamedLocation, NamedLocation>(importService.ImportNamedLocationAsync),
            migrationTable,
            cancellationToken);

        imported += await ProcessBatchAsync(
            perTypeCounts,
            "authenticationStrengthPolicies",
            () => importService.ReadAuthenticationStrengthPoliciesFromFolderAsync(folder, cancellationToken),
            dryRun ? null : WrapImporter<AuthenticationStrengthPolicy, AuthenticationStrengthPolicy>(importService.ImportAuthenticationStrengthPolicyAsync),
            migrationTable,
            cancellationToken);

        imported += await ProcessBatchAsync(
            perTypeCounts,
            "authenticationContexts",
            () => importService.ReadAuthenticationContextsFromFolderAsync(folder, cancellationToken),
            dryRun ? null : WrapImporter<AuthenticationContextClassReference, AuthenticationContextClassReference>(importService.ImportAuthenticationContextAsync),
            migrationTable,
            cancellationToken);

        imported += await ProcessBatchAsync(
            perTypeCounts,
            "termsOfUseAgreements",
            () => importService.ReadTermsOfUseAgreementsFromFolderAsync(folder, cancellationToken),
            dryRun ? null : WrapImporter<Agreement, Agreement>(importService.ImportTermsOfUseAgreementAsync),
            migrationTable,
            cancellationToken);

        imported += await ProcessBatchAsync(
            perTypeCounts,
            "deviceManagementScripts",
            () => importService.ReadDeviceManagementScriptsFromFolderAsync(folder, cancellationToken),
            dryRun ? null : WrapImporter<DeviceManagementScript, DeviceManagementScript>(importService.ImportDeviceManagementScriptAsync),
            migrationTable,
            cancellationToken);

        imported += await ProcessBatchAsync(
            perTypeCounts,
            "deviceShellScripts",
            () => importService.ReadDeviceShellScriptsFromFolderAsync(folder, cancellationToken),
            dryRun ? null : WrapImporter<DeviceShellScript, DeviceShellScript>(importService.ImportDeviceShellScriptAsync),
            migrationTable,
            cancellationToken);

        imported += await ProcessBatchAsync(
            perTypeCounts,
            "complianceScripts",
            () => importService.ReadComplianceScriptsFromFolderAsync(folder, cancellationToken),
            dryRun ? null : WrapImporter<DeviceComplianceScript, DeviceComplianceScript>(importService.ImportComplianceScriptAsync),
            migrationTable,
            cancellationToken);

        imported += await ProcessBatchAsync(
            perTypeCounts,
            "qualityUpdateProfiles",
            () => importService.ReadQualityUpdateProfilesFromFolderAsync(folder, cancellationToken),
            dryRun ? null : WrapImporter<WindowsQualityUpdateProfile, WindowsQualityUpdateProfile>(importService.ImportQualityUpdateProfileAsync),
            migrationTable,
            cancellationToken);

        imported += await ProcessBatchAsync(
            perTypeCounts,
            "driverUpdateProfiles",
            () => importService.ReadDriverUpdateProfilesFromFolderAsync(folder, cancellationToken),
            dryRun ? null : WrapImporter<WindowsDriverUpdateProfile, WindowsDriverUpdateProfile>(importService.ImportDriverUpdateProfileAsync),
            migrationTable,
            cancellationToken);

        imported += await ProcessBatchAsync(
            perTypeCounts,
            "settingsCatalogPolicies",
            () => importService.ReadSettingsCatalogPoliciesFromFolderAsync(folder, cancellationToken),
            dryRun ? null : WrapImporter<SettingsCatalogExport, DeviceManagementConfigurationPolicy>(importService.ImportSettingsCatalogPolicyAsync),
            migrationTable,
            cancellationToken);

        if (!dryRun)
            await exportService!.SaveMigrationTableAsync(migrationTable, folder, cancellationToken);

        return CreateOutput(folder, dryRun, imported, migrationTable, perTypeCounts, []);
    }

    private static Dictionary<string, int> CreatePerTypeCounts()
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var key in SummaryKeys)
            counts[key] = 0;

        return counts;
    }

    private static ImportCommandOutput CreateOutput(
        string folder,
        bool dryRun,
        int count,
        MigrationTable migrationTable,
        Dictionary<string, int> perTypeCounts,
        IReadOnlyList<ImportValidationError> validationErrors) =>
        new()
        {
            Result = new CommandResult
            {
                Command = "import",
                Count = count,
                Path = folder,
                DryRun = dryRun
            },
            MigrationTable = migrationTable,
            Summary = new ImportSummary
            {
                Total = count,
                PerTypeCounts = perTypeCounts,
                ValidationErrorCount = validationErrors.Count
            },
            ValidationErrors = validationErrors
        };

    private static Func<T, MigrationTable, CancellationToken, Task>? WrapImporter<T, TResult>(
        Func<T, MigrationTable, CancellationToken, Task<TResult>> importer) =>
        async (item, migrationTable, cancellationToken) =>
        {
            _ = await importer(item, migrationTable, cancellationToken);
        };

    private static async Task<int> ProcessBatchAsync<T>(
        Dictionary<string, int> perTypeCounts,
        string key,
        Func<Task<List<T>>> reader,
        Func<T, MigrationTable, CancellationToken, Task>? importer,
        MigrationTable migrationTable,
        CancellationToken cancellationToken)
    {
        var items = await reader();
        perTypeCounts[key] = items.Count;

        if (importer == null)
            return items.Count;

        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await importer(item, migrationTable, cancellationToken);
        }

        return items.Count;
    }

    private static async Task<int> ValidateBatchAsync(
        IImportService importService,
        string folder,
        DryRunValidationTarget target,
        Dictionary<string, int> perTypeCounts,
        List<ImportValidationError> validationErrors,
        CancellationToken cancellationToken)
    {
        var targetFolder = Path.Combine(folder, target.FolderName);
        if (!Directory.Exists(targetFolder))
        {
            perTypeCounts[target.SummaryKey] = 0;
            return 0;
        }

        var validItems = 0;
        foreach (var file in Directory.EnumerateFiles(targetFolder, "*.json"))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var item = await target.ReadFile(importService, file, cancellationToken);
                if (item is null)
                {
                    validationErrors.Add(new ImportValidationError
                    {
                        SummaryKey = target.SummaryKey,
                        RelativePath = Path.GetRelativePath(folder, file),
                        ErrorType = "NullPayload",
                        Message = "The file deserialized to null."
                    });
                    continue;
                }

                validItems++;
            }
            catch (Exception ex) when (ShouldCaptureValidationException(ex))
            {
                validationErrors.Add(CreateValidationError(target.SummaryKey, folder, file, ex));
            }
        }

        perTypeCounts[target.SummaryKey] = validItems;
        return validItems;
    }

    private static async Task<MigrationTable> ReadMigrationTableForDryRunAsync(
        IImportService importService,
        string folder,
        List<ImportValidationError> validationErrors,
        CancellationToken cancellationToken)
    {
        try
        {
            return await importService.ReadMigrationTableAsync(folder, cancellationToken);
        }
        catch (Exception ex) when (ShouldCaptureValidationException(ex))
        {
            validationErrors.Add(CreateValidationError("migrationTable", folder, Path.Combine(folder, "migration-table.json"), ex));
            return new MigrationTable();
        }
    }

    private static bool ShouldCaptureValidationException(Exception ex) =>
        ex is JsonException
        or IOException
        or UnauthorizedAccessException
        or NotSupportedException;

    private static bool ShouldCaptureImportException(Exception ex) =>
        ShouldCaptureValidationException(ex)
        || ex is ApiException
        or AuthenticationFailedException
        or HttpRequestException
        or InvalidOperationException;

    private static string FormatImportFailureMessage(Exception ex) => $"Import failed: {ex.Message}";

    private static ImportValidationError CreateValidationError(string summaryKey, string folder, string filePath, Exception ex) =>
        new()
        {
            SummaryKey = summaryKey,
            RelativePath = Path.GetRelativePath(folder, filePath),
            ErrorType = ex.GetType().Name,
            Message = ex.Message
        };

    private static async Task<object?> ReadAsObjectAsync<T>(
        Func<string, CancellationToken, Task<T?>> reader,
        string filePath,
        CancellationToken cancellationToken) where T : class =>
        await reader(filePath, cancellationToken);

    private sealed record DryRunValidationTarget(
        string SummaryKey,
        string FolderName,
        Func<IImportService, string, CancellationToken, Task<object?>> ReadFile);
}

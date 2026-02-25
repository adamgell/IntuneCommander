using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Intune.Commander.Core.Models;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{

    private static string FormatAssignmentSettings(MobileAppAssignmentSettings? settings)
    {
        if (settings is Win32LobAppAssignmentSettings w32)
        {
            var parts = new List<string>();
            if (w32.Notifications.HasValue)
                parts.Add($"Notifications: {w32.Notifications.Value.ToString().ToLowerInvariant()}");
            if (w32.InstallTimeSettings != null)
                parts.Add("Install Time: configured");
            if (w32.DeliveryOptimizationPriority.HasValue)
                parts.Add($"Delivery Priority: {w32.DeliveryOptimizationPriority.Value.ToString().ToLowerInvariant()}");
            return parts.Count > 0 ? string.Join("; ", parts) : "N/A";
        }

        return settings != null ? "N/A" : "N/A";
    }

    // --- CSV Export ---

    [RelayCommand]
    private async Task ExportAppAssignmentsCsvAsync(CancellationToken cancellationToken)
    {
        if (AppAssignmentRows.Count == 0)
        {
            StatusText = "No application assignments data to export";
            return;
        }

        IsBusy = true;
        StatusText = "Exporting Application Assignments to CSV...";

        try
        {
            var outputPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "IntuneExport");
            Directory.CreateDirectory(outputPath);

            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var csvPath = Path.Combine(outputPath, $"ApplicationAssignments-{timestamp}.csv");

            var sb = new StringBuilder();

            // Header row matching the CSV columns
            sb.AppendLine("\"App Name\",\"Publisher\",\"Description\",\"App Type\",\"Version\"," +
                          "\"Bundle ID\",\"Package ID\",\"Is Featured\",\"Created Date\",\"Last Modified\"," +
                          "\"Assignment Type\",\"Target Name\",\"Target Group ID\",\"Install Intent\"," +
                          "\"Assignment Settings\",\"Is Exclusion\",\"App Store URL\",\"Privacy URL\"," +
                          "\"Information URL\",\"Minimum OS Version\",\"Minimum Free Disk Space (MB)\"," +
                          "\"Minimum Memory (MB)\",\"Minimum Processors\",\"Categories\",\"Notes\"");

            foreach (var row in AppAssignmentRows)
            {
                sb.AppendLine(string.Join(",",
                    CsvEscape(row.AppName),
                    CsvEscape(row.Publisher),
                    CsvEscape(row.Description),
                    CsvEscape(row.AppType),
                    CsvEscape(row.Version),
                    CsvEscape(row.BundleId),
                    CsvEscape(row.PackageId),
                    CsvEscape(row.IsFeatured),
                    CsvEscape(row.CreatedDate),
                    CsvEscape(row.LastModified),
                    CsvEscape(row.AssignmentType),
                    CsvEscape(row.TargetName),
                    CsvEscape(row.TargetGroupId),
                    CsvEscape(row.InstallIntent),
                    CsvEscape(row.AssignmentSettings),
                    CsvEscape(row.IsExclusion),
                    CsvEscape(row.AppStoreUrl),
                    CsvEscape(row.PrivacyUrl),
                    CsvEscape(row.InformationUrl),
                    CsvEscape(row.MinimumOsVersion),
                    CsvEscape(row.MinimumFreeDiskSpaceMB),
                    CsvEscape(row.MinimumMemoryMB),
                    CsvEscape(row.MinimumProcessors),
                    CsvEscape(row.Categories),
                    CsvEscape(row.Notes)));
            }

            await File.WriteAllTextAsync(csvPath, sb.ToString(), Encoding.UTF8, cancellationToken);
            StatusText = $"Exported {AppAssignmentRows.Count} rows to {csvPath}";
        }
        catch (Exception ex)
        {
            SetError($"CSV export failed: {ex.Message}");
            StatusText = "CSV export failed";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string CsvEscape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";
        // Neutralize spreadsheet formula injection: prefix formula-starting chars with a tab
        var sanitized = value;
        if (sanitized.Length > 0 && (sanitized[0] == '=' || sanitized[0] == '+' || sanitized[0] == '-' || sanitized[0] == '@'))
            sanitized = "\t" + sanitized;
        return "\"" + sanitized.Replace("\"", "\"\"") + "\"";
    }

    // --- Group CSV Export ---

    [RelayCommand]
    private async Task ExportGroupsCsvAsync(CancellationToken cancellationToken)
    {
        var isDynamic = IsDynamicGroupsCategory;
        var rows = isDynamic ? DynamicGroupRows : AssignedGroupRows;
        var label = isDynamic ? "Dynamic Groups" : "Assigned Groups";

        if (rows.Count == 0)
        {
            StatusText = $"No {label.ToLowerInvariant()} data to export";
            return;
        }

        IsBusy = true;
        StatusText = $"Exporting {label} to CSV...";

        try
        {
            var outputPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "IntuneExport");
            Directory.CreateDirectory(outputPath);

            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var fileName = isDynamic ? "DynamicGroups" : "AssignedGroups";
            var csvPath = Path.Combine(outputPath, $"{fileName}-{timestamp}.csv");

            var sb = new StringBuilder();

            if (isDynamic)
            {
                sb.AppendLine("\"Group Name\",\"Description\",\"Membership Rule\",\"Processing State\"," +
                              "\"Group Type\",\"Total Members\",\"Users\",\"Devices\",\"Nested Groups\"," +
                              "\"Security Enabled\",\"Mail Enabled\",\"Created Date\",\"Group ID\"");

                foreach (var row in rows)
                {
                    sb.AppendLine(string.Join(",",
                        CsvEscape(row.GroupName), CsvEscape(row.Description),
                        CsvEscape(row.MembershipRule), CsvEscape(row.ProcessingState),
                        CsvEscape(row.GroupType), CsvEscape(row.TotalMembers),
                        CsvEscape(row.Users), CsvEscape(row.Devices),
                        CsvEscape(row.NestedGroups), CsvEscape(row.SecurityEnabled),
                        CsvEscape(row.MailEnabled), CsvEscape(row.CreatedDate),
                        CsvEscape(row.GroupId)));
                }
            }
            else
            {
                sb.AppendLine("\"Group Name\",\"Description\",\"Group Type\"," +
                              "\"Total Members\",\"Users\",\"Devices\",\"Nested Groups\"," +
                              "\"Security Enabled\",\"Mail Enabled\",\"Created Date\",\"Group ID\"");

                foreach (var row in rows)
                {
                    sb.AppendLine(string.Join(",",
                        CsvEscape(row.GroupName), CsvEscape(row.Description),
                        CsvEscape(row.GroupType), CsvEscape(row.TotalMembers),
                        CsvEscape(row.Users), CsvEscape(row.Devices),
                        CsvEscape(row.NestedGroups), CsvEscape(row.SecurityEnabled),
                        CsvEscape(row.MailEnabled), CsvEscape(row.CreatedDate),
                        CsvEscape(row.GroupId)));
                }
            }

            await File.WriteAllTextAsync(csvPath, sb.ToString(), Encoding.UTF8, cancellationToken);
            StatusText = $"Exported {rows.Count} {label.ToLowerInvariant()} to {csvPath}";
        }
        catch (Exception ex)
        {
            SetError($"CSV export failed: {ex.Message}");
            StatusText = "CSV export failed";
        }
        finally
        {
            IsBusy = false;
        }
    }


    // --- Settings Catalog CSV Export ---

    [RelayCommand]
    private async Task ExportSettingsCatalogCsvAsync(CancellationToken cancellationToken)
    {
        if (SettingsCatalogPolicies.Count == 0)
        {
            StatusText = "No settings catalog policies to export";
            return;
        }

        IsBusy = true;
        StatusText = "Exporting Settings Catalog to CSV...";

        try
        {
            var outputPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "IntuneExport");
            Directory.CreateDirectory(outputPath);

            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var csvPath = Path.Combine(outputPath, $"SettingsCatalog-{timestamp}.csv");

            var sb = new StringBuilder();
            sb.AppendLine("\"Name\",\"ID\",\"Description\",\"Platforms\",\"Technologies\"," +
                          "\"Created Date\",\"Last Modified\",\"Setting Count\",\"Is Assigned\"," +
                          "\"Template Display Name\"");

            foreach (var policy in SettingsCatalogPolicies)
            {
                sb.AppendLine(string.Join(",",
                    CsvEscape(policy.Name),
                    CsvEscape(policy.Id),
                    CsvEscape(policy.Description),
                    CsvEscape(policy.Platforms?.ToString()),
                    CsvEscape(policy.Technologies?.ToString()),
                    CsvEscape(policy.CreatedDateTime?.ToString("yyyy-MM-dd HH:mm:ss")),
                    CsvEscape(policy.LastModifiedDateTime?.ToString("yyyy-MM-dd HH:mm:ss")),
                    CsvEscape(policy.SettingCount?.ToString()),
                    CsvEscape(policy.IsAssigned?.ToString()),
                    CsvEscape(policy.TemplateReference?.TemplateDisplayName)));
            }

            await File.WriteAllTextAsync(csvPath, sb.ToString(), Encoding.UTF8, cancellationToken);
            StatusText = $"Exported {SettingsCatalogPolicies.Count} settings catalog policies to {csvPath}";
        }
        catch (Exception ex)
        {
            SetError($"CSV export failed: {ex.Message}");
            StatusText = "CSV export failed";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // --- Compliance Policies CSV Export ---

    [RelayCommand]
    private async Task ExportCompliancePoliciesCsvAsync(CancellationToken cancellationToken)
    {
        if (CompliancePolicies.Count == 0)
        {
            StatusText = "No compliance policies to export";
            return;
        }

        IsBusy = true;
        StatusText = "Exporting Compliance Policies to CSV...";

        try
        {
            var outputPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "IntuneExport");
            Directory.CreateDirectory(outputPath);

            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var csvPath = Path.Combine(outputPath, $"CompliancePolicies-{timestamp}.csv");

            var sb = new StringBuilder();
            sb.AppendLine("\"Name\",\"ID\",\"Description\",\"Policy Type\",\"Created Date\"," +
                          "\"Last Modified\",\"Version\"");

            foreach (var policy in CompliancePolicies)
            {
                sb.AppendLine(string.Join(",",
                    CsvEscape(policy.DisplayName),
                    CsvEscape(policy.Id),
                    CsvEscape(policy.Description),
                    CsvEscape(policy.OdataType),
                    CsvEscape(policy.CreatedDateTime?.ToString("yyyy-MM-dd HH:mm:ss")),
                    CsvEscape(policy.LastModifiedDateTime?.ToString("yyyy-MM-dd HH:mm:ss")),
                    CsvEscape(policy.Version?.ToString())));
            }

            await File.WriteAllTextAsync(csvPath, sb.ToString(), Encoding.UTF8, cancellationToken);
            StatusText = $"Exported {CompliancePolicies.Count} compliance policies to {csvPath}";
        }
        catch (Exception ex)
        {
            SetError($"CSV export failed: {ex.Message}");
            StatusText = "CSV export failed";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // --- Admin Templates CSV Export ---

    [RelayCommand]
    private async Task ExportAdminTemplatesCsvAsync(CancellationToken cancellationToken)
    {
        if (AdministrativeTemplates.Count == 0)
        {
            StatusText = "No administrative templates to export";
            return;
        }

        IsBusy = true;
        StatusText = "Exporting Administrative Templates to CSV...";

        try
        {
            var outputPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "IntuneExport");
            Directory.CreateDirectory(outputPath);

            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var csvPath = Path.Combine(outputPath, $"AdministrativeTemplates-{timestamp}.csv");

            var sb = new StringBuilder();
            sb.AppendLine("\"Name\",\"ID\",\"Description\",\"Policy Type\",\"Created Date\",\"Last Modified\"");

            foreach (var template in AdministrativeTemplates)
            {
                sb.AppendLine(string.Join(",",
                    CsvEscape(template.DisplayName),
                    CsvEscape(template.Id),
                    CsvEscape(template.Description),
                    CsvEscape(template.PolicyConfigurationIngestionType?.ToString()),
                    CsvEscape(template.CreatedDateTime?.ToString("yyyy-MM-dd HH:mm:ss")),
                    CsvEscape(template.LastModifiedDateTime?.ToString("yyyy-MM-dd HH:mm:ss"))));
            }

            await File.WriteAllTextAsync(csvPath, sb.ToString(), Encoding.UTF8, cancellationToken);
            StatusText = $"Exported {AdministrativeTemplates.Count} administrative templates to {csvPath}";
        }
        catch (Exception ex)
        {
            SetError($"CSV export failed: {ex.Message}");
            StatusText = "CSV export failed";
        }
        finally
        {
            IsBusy = false;
        }
    }


    // --- Export ---

    [RelayCommand]
    private async Task ExportSelectedAsync(CancellationToken cancellationToken)
    {
        ClearError();
        IsBusy = true;

        try
        {
            var outputPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "IntuneExport");

            var migrationTable = new MigrationTable();

            if (IsDeviceConfigCategory && SelectedConfiguration != null)
            {
                StatusText = $"Exporting {SelectedConfiguration.DisplayName}...";
                await _exportService.ExportDeviceConfigurationAsync(
                    SelectedConfiguration, outputPath, migrationTable, cancellationToken);
            }
            else if (IsCompliancePolicyCategory && SelectedCompliancePolicy != null)
            {
                StatusText = $"Exporting {SelectedCompliancePolicy.DisplayName}...";
                var assignments = _compliancePolicyService != null && SelectedCompliancePolicy.Id != null
                    ? await _compliancePolicyService.GetAssignmentsAsync(SelectedCompliancePolicy.Id, cancellationToken)
                    : [];
                await _exportService.ExportCompliancePolicyAsync(
                    SelectedCompliancePolicy, assignments, outputPath, migrationTable, cancellationToken);
            }
            else if (IsApplicationCategory && SelectedApplication != null)
            {
                StatusText = $"Exporting {SelectedApplication.DisplayName}...";
                var assignments = _applicationService != null && SelectedApplication.Id != null
                    ? await _applicationService.GetAssignmentsAsync(SelectedApplication.Id, cancellationToken)
                    : [];
                await _exportService.ExportApplicationAsync(
                    SelectedApplication, assignments, outputPath, migrationTable, cancellationToken);
            }
            else if (IsEndpointSecurityCategory && SelectedEndpointSecurityIntent != null)
            {
                StatusText = $"Exporting {SelectedEndpointSecurityIntent.DisplayName}...";
                var assignments = _endpointSecurityService != null && SelectedEndpointSecurityIntent.Id != null
                    ? await _endpointSecurityService.GetAssignmentsAsync(SelectedEndpointSecurityIntent.Id, cancellationToken)
                    : [];
                await _exportService.ExportEndpointSecurityIntentAsync(
                    SelectedEndpointSecurityIntent, assignments, outputPath, migrationTable, cancellationToken);
            }
            else if (IsAdministrativeTemplatesCategory && SelectedAdministrativeTemplate != null)
            {
                StatusText = $"Exporting {SelectedAdministrativeTemplate.DisplayName}...";
                var assignments = _administrativeTemplateService != null && SelectedAdministrativeTemplate.Id != null
                    ? await _administrativeTemplateService.GetAssignmentsAsync(SelectedAdministrativeTemplate.Id, cancellationToken)
                    : [];
                await _exportService.ExportAdministrativeTemplateAsync(
                    SelectedAdministrativeTemplate, assignments, outputPath, migrationTable, cancellationToken);
            }
            else if (IsSettingsCatalogCategory && SelectedSettingsCatalogPolicy != null)
            {
                StatusText = $"Exporting {SelectedSettingsCatalogPolicy.Name}...";
                var settings = _settingsCatalogService != null && SelectedSettingsCatalogPolicy.Id != null
                    ? await _settingsCatalogService.GetPolicySettingsAsync(SelectedSettingsCatalogPolicy.Id, cancellationToken)
                    : [];
                var assignments = _settingsCatalogService != null && SelectedSettingsCatalogPolicy.Id != null
                    ? await _settingsCatalogService.GetAssignmentsAsync(SelectedSettingsCatalogPolicy.Id, cancellationToken)
                    : [];
                await _exportService.ExportSettingsCatalogPolicyAsync(
                    SelectedSettingsCatalogPolicy, settings, assignments, outputPath, migrationTable, cancellationToken);
            }
            else if (IsEnrollmentConfigurationsCategory && SelectedEnrollmentConfiguration != null)
            {
                StatusText = $"Exporting {SelectedEnrollmentConfiguration.DisplayName}...";
                await _exportService.ExportEnrollmentConfigurationAsync(
                    SelectedEnrollmentConfiguration, outputPath, migrationTable, cancellationToken);
            }
            else if (IsAppProtectionPoliciesCategory && SelectedAppProtectionPolicy != null)
            {
                StatusText = $"Exporting {SelectedAppProtectionPolicy.DisplayName}...";
                await _exportService.ExportAppProtectionPolicyAsync(
                    SelectedAppProtectionPolicy, outputPath, migrationTable, cancellationToken);
            }
            else if (IsManagedDeviceAppConfigurationsCategory && SelectedManagedDeviceAppConfiguration != null)
            {
                StatusText = $"Exporting {SelectedManagedDeviceAppConfiguration.DisplayName}...";
                await _exportService.ExportManagedDeviceAppConfigurationAsync(
                    SelectedManagedDeviceAppConfiguration, outputPath, migrationTable, cancellationToken);
            }
            else if (IsTargetedManagedAppConfigurationsCategory && SelectedTargetedManagedAppConfiguration != null)
            {
                StatusText = $"Exporting {SelectedTargetedManagedAppConfiguration.DisplayName}...";
                await _exportService.ExportTargetedManagedAppConfigurationAsync(
                    SelectedTargetedManagedAppConfiguration, outputPath, migrationTable, cancellationToken);
            }
            else if (IsTermsAndConditionsCategory && SelectedTermsAndConditions != null)
            {
                StatusText = $"Exporting {SelectedTermsAndConditions.DisplayName}...";
                await _exportService.ExportTermsAndConditionsAsync(
                    SelectedTermsAndConditions, outputPath, migrationTable, cancellationToken);
            }
            else if (IsScopeTagsCategory && SelectedScopeTag != null)
            {
                StatusText = $"Exporting {SelectedScopeTag.DisplayName}...";
                await _exportService.ExportScopeTagAsync(
                    SelectedScopeTag, outputPath, migrationTable, cancellationToken);
            }
            else if (IsRoleDefinitionsCategory && SelectedRoleDefinition != null)
            {
                StatusText = $"Exporting {SelectedRoleDefinition.DisplayName}...";
                await _exportService.ExportRoleDefinitionAsync(
                    SelectedRoleDefinition, outputPath, migrationTable, cancellationToken);
            }
            else if (IsIntuneBrandingCategory && SelectedIntuneBrandingProfile != null)
            {
                StatusText = $"Exporting {SelectedIntuneBrandingProfile.DisplayName}...";
                await _exportService.ExportIntuneBrandingProfileAsync(
                    SelectedIntuneBrandingProfile, outputPath, migrationTable, cancellationToken);
            }
            else if (IsAzureBrandingCategory && SelectedAzureBrandingLocalization != null)
            {
                StatusText = $"Exporting {SelectedAzureBrandingLocalization.Id ?? "branding localization"}...";
                await _exportService.ExportAzureBrandingLocalizationAsync(
                    SelectedAzureBrandingLocalization, outputPath, migrationTable, cancellationToken);
            }
            else if (IsAutopilotProfilesCategory && SelectedAutopilotProfile != null)
            {
                StatusText = $"Exporting {SelectedAutopilotProfile.DisplayName}...";
                await _exportService.ExportAutopilotProfileAsync(
                    SelectedAutopilotProfile, outputPath, migrationTable, cancellationToken);
            }
            else if (IsDeviceHealthScriptsCategory && SelectedDeviceHealthScript != null)
            {
                StatusText = $"Exporting {SelectedDeviceHealthScript.DisplayName}...";
                await _exportService.ExportDeviceHealthScriptAsync(
                    SelectedDeviceHealthScript, outputPath, migrationTable, cancellationToken);
            }
            else if (IsMacCustomAttributesCategory && SelectedMacCustomAttribute != null)
            {
                StatusText = $"Exporting {SelectedMacCustomAttribute.DisplayName}...";
                await _exportService.ExportMacCustomAttributeAsync(
                    SelectedMacCustomAttribute, outputPath, migrationTable, cancellationToken);
            }
            else if (IsFeatureUpdatesCategory && SelectedFeatureUpdateProfile != null)
            {
                StatusText = $"Exporting {SelectedFeatureUpdateProfile.DisplayName}...";
                await _exportService.ExportFeatureUpdateProfileAsync(
                    SelectedFeatureUpdateProfile, outputPath, migrationTable, cancellationToken);
            }
            else if (IsNamedLocationsCategory && SelectedNamedLocation != null)
            {
                StatusText = $"Exporting {TryReadStringProperty(SelectedNamedLocation, "DisplayName") ?? "named location"}...";
                await _exportService.ExportNamedLocationAsync(
                    SelectedNamedLocation, outputPath, migrationTable, cancellationToken);
            }
            else if (IsAuthenticationStrengthsCategory && SelectedAuthenticationStrengthPolicy != null)
            {
                StatusText = $"Exporting {SelectedAuthenticationStrengthPolicy.DisplayName}...";
                await _exportService.ExportAuthenticationStrengthPolicyAsync(
                    SelectedAuthenticationStrengthPolicy, outputPath, migrationTable, cancellationToken);
            }
            else if (IsAuthenticationContextsCategory && SelectedAuthenticationContextClassReference != null)
            {
                StatusText = $"Exporting {SelectedAuthenticationContextClassReference.DisplayName}...";
                await _exportService.ExportAuthenticationContextAsync(
                    SelectedAuthenticationContextClassReference, outputPath, migrationTable, cancellationToken);
            }
            else if (IsTermsOfUseCategory && SelectedTermsOfUseAgreement != null)
            {
                StatusText = $"Exporting {SelectedTermsOfUseAgreement.DisplayName ?? "terms of use"}...";
                await _exportService.ExportTermsOfUseAgreementAsync(
                    SelectedTermsOfUseAgreement, outputPath, migrationTable, cancellationToken);
            }
            else if (IsDeviceManagementScriptsCategory && SelectedDeviceManagementScript != null)
            {
                StatusText = $"Exporting {SelectedDeviceManagementScript.DisplayName ?? "device management script"}...";
                await _exportService.ExportDeviceManagementScriptAsync(
                    SelectedDeviceManagementScript, outputPath, migrationTable, cancellationToken);
            }
            else if (IsDeviceShellScriptsCategory && SelectedDeviceShellScript != null)
            {
                StatusText = $"Exporting {SelectedDeviceShellScript.DisplayName ?? "device shell script"}...";
                await _exportService.ExportDeviceShellScriptAsync(
                    SelectedDeviceShellScript, outputPath, migrationTable, cancellationToken);
            }
            else if (IsComplianceScriptsCategory && SelectedComplianceScript != null)
            {
                StatusText = $"Exporting {SelectedComplianceScript.DisplayName ?? "compliance script"}...";
                await _exportService.ExportComplianceScriptAsync(
                    SelectedComplianceScript, outputPath, migrationTable, cancellationToken);
            }
            else if (IsQualityUpdatesCategory && SelectedQualityUpdateProfile != null)
            {
                StatusText = $"Exporting {SelectedQualityUpdateProfile.DisplayName ?? "quality update profile"}...";
                await _exportService.ExportQualityUpdateProfileAsync(
                    SelectedQualityUpdateProfile, outputPath, migrationTable, cancellationToken);
            }
            else if (IsDriverUpdatesCategory && SelectedDriverUpdateProfile != null)
            {
                StatusText = $"Exporting {SelectedDriverUpdateProfile.DisplayName ?? "driver update profile"}...";
                await _exportService.ExportDriverUpdateProfileAsync(
                    SelectedDriverUpdateProfile, outputPath, migrationTable, cancellationToken);
            }
            else
            {
                StatusText = "Nothing selected to export";
                return;
            }

            await _exportService.SaveMigrationTableAsync(migrationTable, outputPath, cancellationToken);
            StatusText = $"Exported to {outputPath}";
        }
        catch (Exception ex)
        {
            SetError($"Export failed: {FormatGraphError(ex)}");
            StatusText = "Export failed";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportAllAsync(CancellationToken cancellationToken)
    {
        ClearError();
        IsBusy = true;

        try
        {
            var outputPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "IntuneExport");

            var migrationTable = new MigrationTable();
            var count = 0;

            // Export device configs
            if (DeviceConfigurations.Any())
            {
                StatusText = "Exporting device configurations...";
                foreach (var config in DeviceConfigurations)
                {
                    await _exportService.ExportDeviceConfigurationAsync(config, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            // Export compliance policies with assignments
            if (CompliancePolicies.Any() && _compliancePolicyService != null)
            {
                StatusText = "Exporting compliance policies...";
                foreach (var policy in CompliancePolicies)
                {
                    var assignments = policy.Id != null
                        ? await _compliancePolicyService.GetAssignmentsAsync(policy.Id, cancellationToken)
                        : [];
                    await _exportService.ExportCompliancePolicyAsync(policy, assignments, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            // Export applications with assignments
            if (Applications.Any() && _applicationService != null)
            {
                StatusText = "Exporting applications...";
                foreach (var app in Applications)
                {
                    var assignments = app.Id != null
                        ? await _applicationService.GetAssignmentsAsync(app.Id, cancellationToken)
                        : [];
                    await _exportService.ExportApplicationAsync(app, assignments, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            // Export endpoint security intents with assignments
            if (EndpointSecurityIntents.Any() && _endpointSecurityService != null)
            {
                StatusText = "Exporting endpoint security intents...";
                foreach (var intent in EndpointSecurityIntents)
                {
                    var assignments = intent.Id != null
                        ? await _endpointSecurityService.GetAssignmentsAsync(intent.Id, cancellationToken)
                        : [];
                    await _exportService.ExportEndpointSecurityIntentAsync(intent, assignments, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            // Export administrative templates with assignments
            if (AdministrativeTemplates.Any() && _administrativeTemplateService != null)
            {
                StatusText = "Exporting administrative templates...";
                foreach (var template in AdministrativeTemplates)
                {
                    var assignments = template.Id != null
                        ? await _administrativeTemplateService.GetAssignmentsAsync(template.Id, cancellationToken)
                        : [];
                    await _exportService.ExportAdministrativeTemplateAsync(template, assignments, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            // Export settings catalog policies with settings and assignments (bounded parallelism)
            if (SettingsCatalogPolicies.Any() && _settingsCatalogService != null)
            {
                StatusText = "Exporting settings catalog policies...";
                const int maxDegreeOfParallelism = 4;
                using var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
                var policyExportTasks = new List<Task>();

                foreach (var policy in SettingsCatalogPolicies)
                {
                    var capturedPolicy = policy;
                    policyExportTasks.Add(Task.Run(async () =>
                    {
                        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                        try
                        {
                            var settings = capturedPolicy.Id != null
                                ? await _settingsCatalogService.GetPolicySettingsAsync(capturedPolicy.Id, cancellationToken).ConfigureAwait(false)
                                : [];
                            var assignments = capturedPolicy.Id != null
                                ? await _settingsCatalogService.GetAssignmentsAsync(capturedPolicy.Id, cancellationToken).ConfigureAwait(false)
                                : [];
                            await _exportService.ExportSettingsCatalogPolicyAsync(capturedPolicy, settings, assignments, outputPath, migrationTable, cancellationToken).ConfigureAwait(false);
                            Interlocked.Increment(ref count);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }, cancellationToken));
                }

                await Task.WhenAll(policyExportTasks).ConfigureAwait(false);
            }

            // Export enrollment configurations
            if (EnrollmentConfigurations.Any())
            {
                StatusText = "Exporting enrollment configurations...";
                foreach (var configuration in EnrollmentConfigurations)
                {
                    await _exportService.ExportEnrollmentConfigurationAsync(configuration, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            // Export app protection policies
            if (AppProtectionPolicies.Any())
            {
                StatusText = "Exporting app protection policies...";
                foreach (var policy in AppProtectionPolicies)
                {
                    await _exportService.ExportAppProtectionPolicyAsync(policy, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            // Export managed device app configurations
            if (ManagedDeviceAppConfigurations.Any())
            {
                StatusText = "Exporting managed device app configurations...";
                foreach (var configuration in ManagedDeviceAppConfigurations)
                {
                    await _exportService.ExportManagedDeviceAppConfigurationAsync(configuration, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            // Export targeted managed app configurations
            if (TargetedManagedAppConfigurations.Any())
            {
                StatusText = "Exporting targeted managed app configurations...";
                foreach (var configuration in TargetedManagedAppConfigurations)
                {
                    await _exportService.ExportTargetedManagedAppConfigurationAsync(configuration, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            // Export terms and conditions
            if (TermsAndConditionsCollection.Any())
            {
                StatusText = "Exporting terms and conditions...";
                foreach (var termsAndConditions in TermsAndConditionsCollection)
                {
                    await _exportService.ExportTermsAndConditionsAsync(termsAndConditions, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            // Export scope tags
            if (ScopeTags.Any())
            {
                StatusText = "Exporting scope tags...";
                foreach (var scopeTag in ScopeTags)
                {
                    await _exportService.ExportScopeTagAsync(scopeTag, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            // Export role definitions
            if (RoleDefinitions.Any())
            {
                StatusText = "Exporting role definitions...";
                foreach (var roleDefinition in RoleDefinitions)
                {
                    await _exportService.ExportRoleDefinitionAsync(roleDefinition, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            // Export Intune branding profiles
            if (IntuneBrandingProfiles.Any())
            {
                StatusText = "Exporting Intune branding profiles...";
                foreach (var profile in IntuneBrandingProfiles)
                {
                    await _exportService.ExportIntuneBrandingProfileAsync(profile, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            // Export Azure branding localizations
            if (AzureBrandingLocalizations.Any())
            {
                StatusText = "Exporting Azure branding localizations...";
                foreach (var localization in AzureBrandingLocalizations)
                {
                    await _exportService.ExportAzureBrandingLocalizationAsync(localization, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            // Export autopilot profiles
            if (AutopilotProfiles.Any())
            {
                StatusText = "Exporting autopilot profiles...";
                foreach (var profile in AutopilotProfiles)
                {
                    await _exportService.ExportAutopilotProfileAsync(profile, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            // Export device health scripts
            if (DeviceHealthScripts.Any())
            {
                StatusText = "Exporting device health scripts...";
                foreach (var script in DeviceHealthScripts)
                {
                    await _exportService.ExportDeviceHealthScriptAsync(script, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            // Export mac custom attributes
            if (MacCustomAttributes.Any())
            {
                StatusText = "Exporting mac custom attributes...";
                foreach (var script in MacCustomAttributes)
                {
                    await _exportService.ExportMacCustomAttributeAsync(script, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            // Export feature update profiles
            if (FeatureUpdateProfiles.Any())
            {
                StatusText = "Exporting feature update profiles...";
                foreach (var profile in FeatureUpdateProfiles)
                {
                    await _exportService.ExportFeatureUpdateProfileAsync(profile, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            // Export named locations
            if (NamedLocations.Any())
            {
                StatusText = "Exporting named locations...";
                foreach (var namedLocation in NamedLocations)
                {
                    await _exportService.ExportNamedLocationAsync(namedLocation, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            // Export authentication strength policies
            if (AuthenticationStrengthPolicies.Any())
            {
                StatusText = "Exporting authentication strengths...";
                foreach (var policy in AuthenticationStrengthPolicies)
                {
                    await _exportService.ExportAuthenticationStrengthPolicyAsync(policy, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            // Export authentication contexts
            if (AuthenticationContextClassReferences.Any())
            {
                StatusText = "Exporting authentication contexts...";
                foreach (var contextClassReference in AuthenticationContextClassReferences)
                {
                    await _exportService.ExportAuthenticationContextAsync(contextClassReference, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            // Export terms of use agreements
            if (TermsOfUseAgreements.Any())
            {
                StatusText = "Exporting terms of use agreements...";
                foreach (var agreement in TermsOfUseAgreements)
                {
                    await _exportService.ExportTermsOfUseAgreementAsync(agreement, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            // Export device management scripts
            if (DeviceManagementScripts.Any())
            {
                StatusText = "Exporting device management scripts...";
                foreach (var script in DeviceManagementScripts)
                {
                    await _exportService.ExportDeviceManagementScriptAsync(script, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            // Export device shell scripts
            if (DeviceShellScripts.Any())
            {
                StatusText = "Exporting device shell scripts...";
                foreach (var script in DeviceShellScripts)
                {
                    await _exportService.ExportDeviceShellScriptAsync(script, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            // Export compliance scripts
            if (ComplianceScripts.Any())
            {
                StatusText = "Exporting compliance scripts...";
                foreach (var script in ComplianceScripts)
                {
                    await _exportService.ExportComplianceScriptAsync(script, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            // Export quality update profiles
            if (QualityUpdateProfiles.Any())
            {
                StatusText = "Exporting quality update profiles...";
                foreach (var profile in QualityUpdateProfiles)
                {
                    await _exportService.ExportQualityUpdateProfileAsync(profile, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            // Export driver update profiles
            if (DriverUpdateProfiles.Any())
            {
                StatusText = "Exporting driver update profiles...";
                foreach (var profile in DriverUpdateProfiles)
                {
                    await _exportService.ExportDriverUpdateProfileAsync(profile, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            await _exportService.SaveMigrationTableAsync(migrationTable, outputPath, cancellationToken);
            StatusText = $"Exported {count} item(s) to {outputPath}";
        }
        catch (Exception ex)
        {
            SetError($"Export failed: {FormatGraphError(ex)}");
            StatusText = "Export failed";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // --- Import ---

    [RelayCommand]
    private async Task ImportFromFolderAsync(string folderPath, CancellationToken cancellationToken)
    {
        if (_importService == null) return;

        ClearError();
        IsBusy = true;
        StatusText = "Importing...";

        try
        {
            var migrationTable = await _importService.ReadMigrationTableAsync(folderPath, cancellationToken);
            var imported = 0;

            // Import device configurations
            var configs = await _importService.ReadDeviceConfigurationsFromFolderAsync(folderPath, cancellationToken);
            foreach (var config in configs)
            {
                await _importService.ImportDeviceConfigurationAsync(config, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Import compliance policies
            var policies = await _importService.ReadCompliancePoliciesFromFolderAsync(folderPath, cancellationToken);
            foreach (var export in policies)
            {
                await _importService.ImportCompliancePolicyAsync(export, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Import endpoint security intents
            var endpointIntents = await _importService.ReadEndpointSecurityIntentsFromFolderAsync(folderPath, cancellationToken);
            foreach (var export in endpointIntents)
            {
                await _importService.ImportEndpointSecurityIntentAsync(export, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Import administrative templates
            var templates = await _importService.ReadAdministrativeTemplatesFromFolderAsync(folderPath, cancellationToken);
            foreach (var export in templates)
            {
                await _importService.ImportAdministrativeTemplateAsync(export, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Import enrollment configurations
            var enrollmentConfigs = await _importService.ReadEnrollmentConfigurationsFromFolderAsync(folderPath, cancellationToken);
            foreach (var config in enrollmentConfigs)
            {
                await _importService.ImportEnrollmentConfigurationAsync(config, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Import app protection policies
            var appProtectionPolicies = await _importService.ReadAppProtectionPoliciesFromFolderAsync(folderPath, cancellationToken);
            foreach (var policy in appProtectionPolicies)
            {
                await _importService.ImportAppProtectionPolicyAsync(policy, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Import managed device app configurations
            var managedDeviceAppConfigs = await _importService.ReadManagedDeviceAppConfigurationsFromFolderAsync(folderPath, cancellationToken);
            foreach (var configuration in managedDeviceAppConfigs)
            {
                await _importService.ImportManagedDeviceAppConfigurationAsync(configuration, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Import targeted managed app configurations
            var targetedAppConfigs = await _importService.ReadTargetedManagedAppConfigurationsFromFolderAsync(folderPath, cancellationToken);
            foreach (var configuration in targetedAppConfigs)
            {
                await _importService.ImportTargetedManagedAppConfigurationAsync(configuration, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Import terms and conditions
            var termsCollection = await _importService.ReadTermsAndConditionsFromFolderAsync(folderPath, cancellationToken);
            foreach (var termsAndConditions in termsCollection)
            {
                await _importService.ImportTermsAndConditionsAsync(termsAndConditions, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Import scope tags
            var scopeTags = await _importService.ReadScopeTagsFromFolderAsync(folderPath, cancellationToken);
            foreach (var scopeTag in scopeTags)
            {
                await _importService.ImportScopeTagAsync(scopeTag, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Import role definitions
            var roleDefinitions = await _importService.ReadRoleDefinitionsFromFolderAsync(folderPath, cancellationToken);
            foreach (var roleDefinition in roleDefinitions)
            {
                await _importService.ImportRoleDefinitionAsync(roleDefinition, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Import Intune branding profiles
            var intuneBrandingProfiles = await _importService.ReadIntuneBrandingProfilesFromFolderAsync(folderPath, cancellationToken);
            foreach (var profile in intuneBrandingProfiles)
            {
                await _importService.ImportIntuneBrandingProfileAsync(profile, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Import Azure branding localizations
            var azureBrandingLocalizations = await _importService.ReadAzureBrandingLocalizationsFromFolderAsync(folderPath, cancellationToken);
            foreach (var localization in azureBrandingLocalizations)
            {
                await _importService.ImportAzureBrandingLocalizationAsync(localization, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Import autopilot profiles
            var autopilotProfiles = await _importService.ReadAutopilotProfilesFromFolderAsync(folderPath, cancellationToken);
            foreach (var profile in autopilotProfiles)
            {
                await _importService.ImportAutopilotProfileAsync(profile, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Import device health scripts
            var deviceHealthScripts = await _importService.ReadDeviceHealthScriptsFromFolderAsync(folderPath, cancellationToken);
            foreach (var script in deviceHealthScripts)
            {
                await _importService.ImportDeviceHealthScriptAsync(script, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Import mac custom attributes
            var macCustomAttributes = await _importService.ReadMacCustomAttributesFromFolderAsync(folderPath, cancellationToken);
            foreach (var script in macCustomAttributes)
            {
                await _importService.ImportMacCustomAttributeAsync(script, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Import feature update profiles
            var featureUpdateProfiles = await _importService.ReadFeatureUpdateProfilesFromFolderAsync(folderPath, cancellationToken);
            foreach (var profile in featureUpdateProfiles)
            {
                await _importService.ImportFeatureUpdateProfileAsync(profile, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Import named locations
            var namedLocations = await _importService.ReadNamedLocationsFromFolderAsync(folderPath, cancellationToken);
            foreach (var namedLocation in namedLocations)
            {
                await _importService.ImportNamedLocationAsync(namedLocation, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Import authentication strength policies
            var authenticationStrengthPolicies = await _importService.ReadAuthenticationStrengthPoliciesFromFolderAsync(folderPath, cancellationToken);
            foreach (var policy in authenticationStrengthPolicies)
            {
                await _importService.ImportAuthenticationStrengthPolicyAsync(policy, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Import authentication contexts
            var authenticationContexts = await _importService.ReadAuthenticationContextsFromFolderAsync(folderPath, cancellationToken);
            foreach (var contextClassReference in authenticationContexts)
            {
                await _importService.ImportAuthenticationContextAsync(contextClassReference, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Import terms of use agreements
            var termsOfUseAgreements = await _importService.ReadTermsOfUseAgreementsFromFolderAsync(folderPath, cancellationToken);
            foreach (var agreement in termsOfUseAgreements)
            {
                await _importService.ImportTermsOfUseAgreementAsync(agreement, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Import device management scripts
            var deviceManagementScripts = await _importService.ReadDeviceManagementScriptsFromFolderAsync(folderPath, cancellationToken);
            foreach (var script in deviceManagementScripts)
            {
                await _importService.ImportDeviceManagementScriptAsync(script, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Import device shell scripts
            var deviceShellScripts = await _importService.ReadDeviceShellScriptsFromFolderAsync(folderPath, cancellationToken);
            foreach (var script in deviceShellScripts)
            {
                await _importService.ImportDeviceShellScriptAsync(script, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Import compliance scripts
            var complianceScripts = await _importService.ReadComplianceScriptsFromFolderAsync(folderPath, cancellationToken);
            foreach (var script in complianceScripts)
            {
                await _importService.ImportComplianceScriptAsync(script, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Import quality update profiles
            var qualityUpdateProfiles = await _importService.ReadQualityUpdateProfilesFromFolderAsync(folderPath, cancellationToken);
            foreach (var profile in qualityUpdateProfiles)
            {
                await _importService.ImportQualityUpdateProfileAsync(profile, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Import driver update profiles
            var driverUpdateProfiles = await _importService.ReadDriverUpdateProfilesFromFolderAsync(folderPath, cancellationToken);
            foreach (var profile in driverUpdateProfiles)
            {
                await _importService.ImportDriverUpdateProfileAsync(profile, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Import settings catalog policies
            var settingsCatalogPolicies = await _importService.ReadSettingsCatalogPoliciesFromFolderAsync(folderPath, cancellationToken);
            foreach (var export in settingsCatalogPolicies)
            {
                await _importService.ImportSettingsCatalogPolicyAsync(export, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Save updated migration table
            await _exportService.SaveMigrationTableAsync(migrationTable, folderPath, cancellationToken);
            StatusText = $"Imported {imported} item(s)";

            await RefreshAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            SetError($"Import failed: {FormatGraphError(ex)}");
            StatusText = "Import failed";
        }
        finally
        {
            IsBusy = false;
        }
    }
}

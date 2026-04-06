using System.IO;
using System.Text.Json;
using Intune.Commander.Core.Models;
using Intune.Commander.Core.Services;
using Intune.Commander.DesktopReact.Models;

namespace Intune.Commander.DesktopReact.Services;

public class ExportImportBridgeService
{
    private readonly AuthBridgeService _authBridge;
    private readonly IExportService _exportService;

    // Lazily created Graph services for export (fetch live data)
    private IConfigurationProfileService? _configService;
    private ICompliancePolicyService? _complianceService;
    private IApplicationService? _appService;
    private IEndpointSecurityService? _endpointSecurityService;
    private ISettingsCatalogService? _settingsCatalogService;
    private IScopeTagService? _scopeTagService;
    private IRoleDefinitionService? _roleDefinitionService;
    private IIntuneBrandingService? _intuneBrandingService;
    private IAzureBrandingService? _azureBrandingService;
    private ITermsAndConditionsService? _termsAndConditionsService;
    private ITermsOfUseService? _termsOfUseService;
    private IAppProtectionPolicyService? _appProtectionService;
    private IDeviceHealthScriptService? _deviceHealthScriptService;
    private IEnrollmentConfigurationService? _enrollmentService;
    private IAutopilotService? _autopilotService;
    private IConditionalAccessPolicyService? _conditionalAccessService;
    private INamedLocationService? _namedLocationService;
    private IAuthenticationStrengthService? _authStrengthService;
    private IAdministrativeTemplateService? _adminTemplateService;
    private IManagedAppConfigurationService? _managedAppConfigService;

    /// <summary>
    /// Maps subfolder names (as used in export structure) to human-friendly labels.
    /// </summary>
    private static readonly Dictionary<string, string> ObjectTypeLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["DeviceConfigurations"] = "Device Configurations",
        ["CompliancePolicies"] = "Compliance Policies",
        ["Applications"] = "Applications",
        ["EndpointSecurity"] = "Endpoint Security",
        ["SettingsCatalog"] = "Settings Catalog",
        ["AdministrativeTemplates"] = "Administrative Templates",
        ["EnrollmentConfigurations"] = "Enrollment Configurations",
        ["AppProtectionPolicies"] = "App Protection Policies",
        ["ManagedDeviceAppConfigurations"] = "Managed Device App Configurations",
        ["TargetedManagedAppConfigurations"] = "Targeted Managed App Configurations",
        ["TermsAndConditions"] = "Terms & Conditions",
        ["ScopeTags"] = "Scope Tags",
        ["RoleDefinitions"] = "Role Definitions",
        ["IntuneBrandingProfiles"] = "Intune Branding Profiles",
        ["AzureBrandingLocalizations"] = "Azure Branding Localizations",
        ["AutopilotProfiles"] = "Autopilot Profiles",
        ["DeviceHealthScripts"] = "Device Health Scripts",
        ["ConditionalAccessPolicies"] = "Conditional Access Policies",
        ["NamedLocations"] = "Named Locations",
        ["AuthenticationStrengths"] = "Authentication Strengths",
        ["AuthenticationContexts"] = "Authentication Contexts",
        ["TermsOfUse"] = "Terms of Use",
        ["DeviceManagementScripts"] = "Device Management Scripts",
        ["DeviceShellScripts"] = "Device Shell Scripts",
        ["ComplianceScripts"] = "Compliance Scripts",
        ["FeatureUpdates"] = "Feature Updates",
        ["QualityUpdates"] = "Quality Updates",
        ["DriverUpdates"] = "Driver Updates",
        ["MacCustomAttributes"] = "Mac Custom Attributes",
        ["AdmxFiles"] = "ADMX Files",
        ["ReusablePolicySettings"] = "Reusable Policy Settings",
        ["NotificationTemplates"] = "Notification Templates",
    };

    public ExportImportBridgeService(AuthBridgeService authBridge, IExportService exportService)
    {
        _authBridge = authBridge;
        _exportService = exportService;
    }

    private Microsoft.Graph.Beta.GraphServiceClient GetClient() =>
        _authBridge.GraphClient ?? throw new InvalidOperationException("Not connected — authenticate first");

    public void Reset()
    {
        _configService = null;
        _complianceService = null;
        _appService = null;
        _endpointSecurityService = null;
        _settingsCatalogService = null;
        _scopeTagService = null;
        _roleDefinitionService = null;
        _intuneBrandingService = null;
        _azureBrandingService = null;
        _termsAndConditionsService = null;
        _termsOfUseService = null;
        _appProtectionService = null;
        _deviceHealthScriptService = null;
        _enrollmentService = null;
        _autopilotService = null;
        _conditionalAccessService = null;
        _namedLocationService = null;
        _authStrengthService = null;
        _adminTemplateService = null;
        _managedAppConfigService = null;
    }

    // ── Export ──────────────────────────────────────────────────────────

    public async Task<object> RunExportAsync(JsonElement? payload)
    {
        if (payload is null) throw new ArgumentException("Payload is required");

        var p = payload.Value;
        var outputPath = p.GetProperty("outputPath").GetString()
            ?? throw new ArgumentException("outputPath is required");

        var objectTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (p.TryGetProperty("objectTypes", out var typesProp) && typesProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var t in typesProp.EnumerateArray())
                if (t.GetString() is string s) objectTypes.Add(s);
        }

        var client = GetClient();
        var migrationTable = new MigrationTable();
        int exportedCount = 0;

        // Export each requested type using per-item APIs with shared migration table
        if (ShouldExport("DeviceConfigurations"))
        {
            _configService ??= new ConfigurationProfileService(client);
            var items = await _configService.ListDeviceConfigurationsAsync();
            foreach (var item in items)
            {
                await _exportService.ExportDeviceConfigurationAsync(item, outputPath, migrationTable);
                exportedCount++;
            }
        }

        if (ShouldExport("CompliancePolicies"))
        {
            _complianceService ??= new CompliancePolicyService(client);
            var policies = await _complianceService.ListCompliancePoliciesAsync();
            foreach (var policy in policies)
            {
                var assignments = await _complianceService.GetAssignmentsAsync(policy.Id!);
                await _exportService.ExportCompliancePolicyAsync(policy, assignments, outputPath, migrationTable);
                exportedCount++;
            }
        }

        if (ShouldExport("SettingsCatalog"))
        {
            _settingsCatalogService ??= new SettingsCatalogService(client);
            var policies = await _settingsCatalogService.ListSettingsCatalogPoliciesAsync();
            foreach (var policy in policies)
            {
                var settings = await _settingsCatalogService.GetPolicySettingsAsync(policy.Id!);
                var assignments = await _settingsCatalogService.GetAssignmentsAsync(policy.Id!);
                await _exportService.ExportSettingsCatalogPolicyAsync(policy, settings, assignments, outputPath, migrationTable);
                exportedCount++;
            }
        }

        if (ShouldExport("ScopeTags"))
        {
            _scopeTagService ??= new ScopeTagService(client);
            var items = await _scopeTagService.ListScopeTagsAsync();
            foreach (var item in items)
            {
                await _exportService.ExportScopeTagAsync(item, outputPath, migrationTable);
                exportedCount++;
            }
        }

        if (ShouldExport("RoleDefinitions"))
        {
            _roleDefinitionService ??= new RoleDefinitionService(client);
            var items = await _roleDefinitionService.ListRoleDefinitionsAsync();
            foreach (var item in items)
            {
                await _exportService.ExportRoleDefinitionAsync(item, outputPath, migrationTable);
                exportedCount++;
            }
        }

        if (ShouldExport("TermsAndConditions"))
        {
            _termsAndConditionsService ??= new TermsAndConditionsService(client);
            var items = await _termsAndConditionsService.ListTermsAndConditionsAsync();
            foreach (var item in items)
            {
                await _exportService.ExportTermsAndConditionsAsync(item, outputPath, migrationTable);
                exportedCount++;
            }
        }

        if (ShouldExport("IntuneBrandingProfiles"))
        {
            _intuneBrandingService ??= new IntuneBrandingService(client);
            var items = await _intuneBrandingService.ListIntuneBrandingProfilesAsync();
            foreach (var item in items)
            {
                await _exportService.ExportIntuneBrandingProfileAsync(item, outputPath, migrationTable);
                exportedCount++;
            }
        }

        if (ShouldExport("AzureBrandingLocalizations"))
        {
            _azureBrandingService ??= new AzureBrandingService(client);
            var items = await _azureBrandingService.ListBrandingLocalizationsAsync();
            foreach (var item in items)
            {
                await _exportService.ExportAzureBrandingLocalizationAsync(item, outputPath, migrationTable);
                exportedCount++;
            }
        }

        if (ShouldExport("DeviceHealthScripts"))
        {
            _deviceHealthScriptService ??= new DeviceHealthScriptService(client);
            var items = await _deviceHealthScriptService.ListDeviceHealthScriptsAsync();
            foreach (var item in items)
            {
                await _exportService.ExportDeviceHealthScriptAsync(item, outputPath, migrationTable);
                exportedCount++;
            }
        }

        if (ShouldExport("ConditionalAccessPolicies"))
        {
            _conditionalAccessService ??= new ConditionalAccessPolicyService(client);
            var items = await _conditionalAccessService.ListPoliciesAsync();
            foreach (var item in items)
            {
                await _exportService.ExportConditionalAccessPolicyAsync(item, outputPath, migrationTable);
                exportedCount++;
            }
        }

        await _exportService.SaveMigrationTableAsync(migrationTable, outputPath);

        return new ExportResult(exportedCount, outputPath);

        bool ShouldExport(string type) =>
            objectTypes.Count == 0 || objectTypes.Contains(type);
    }

    // ── Import Preview ─────────────────────────────────────────────────

    public Task<object> PreviewImportAsync(JsonElement? payload)
    {
        if (payload is null) throw new ArgumentException("Payload is required");

        var folderPath = payload.Value.GetProperty("folderPath").GetString()
            ?? throw new ArgumentException("folderPath is required");

        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException($"Import folder not found: {folderPath}");

        var items = new List<ImportPreviewItem>();
        var objectTypes = new HashSet<string>();

        foreach (var subDir in Directory.GetDirectories(folderPath))
        {
            var dirName = Path.GetFileName(subDir);
            var label = ObjectTypeLabels.GetValueOrDefault(dirName, dirName);
            var jsonFiles = Directory.GetFiles(subDir, "*.json");

            foreach (var file in jsonFiles)
            {
                var fileName = Path.GetFileName(file);
                var name = Path.GetFileNameWithoutExtension(file);
                items.Add(new ImportPreviewItem(label, name, fileName));
            }

            if (jsonFiles.Length > 0)
                objectTypes.Add(label);
        }

        var result = new ImportPreview(
            items.OrderBy(i => i.ObjectType).ThenBy(i => i.Name).ToArray(),
            items.Count,
            objectTypes.OrderBy(t => t).ToArray());

        return Task.FromResult<object>(result);
    }

    // ── Import Run ─────────────────────────────────────────────────────

    public async Task<object> RunImportAsync(JsonElement? payload)
    {
        if (payload is null) throw new ArgumentException("Payload is required");

        var p = payload.Value;
        var folderPath = p.GetProperty("folderPath").GetString()
            ?? throw new ArgumentException("folderPath is required");

        var selectedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (p.TryGetProperty("objectTypes", out var typesProp) && typesProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var t in typesProp.EnumerateArray())
                if (t.GetString() is string s) selectedTypes.Add(s);
        }

        var client = GetClient();
        _configService ??= new ConfigurationProfileService(client);

        var importService = new ImportService(
            _configService,
            _complianceService ??= new CompliancePolicyService(client),
            _endpointSecurityService ??= new EndpointSecurityService(client),
            _adminTemplateService ??= new AdministrativeTemplateService(client),
            _enrollmentService ??= new EnrollmentConfigurationService(client),
            _appProtectionService ??= new AppProtectionPolicyService(client),
            _managedAppConfigService ??= new ManagedAppConfigurationService(client),
            _termsAndConditionsService ??= new TermsAndConditionsService(client),
            _scopeTagService ??= new ScopeTagService(client),
            _roleDefinitionService ??= new RoleDefinitionService(client),
            _intuneBrandingService ??= new IntuneBrandingService(client),
            _azureBrandingService ??= new AzureBrandingService(client),
            settingsCatalogService: _settingsCatalogService ??= new SettingsCatalogService(client));

        var migrationTable = await importService.ReadMigrationTableAsync(folderPath);
        var results = new List<ImportResultItem>();

        // Import Device Configurations
        if (ShouldImport("Device Configurations"))
            await ImportTypeAsync("Device Configurations", "DeviceConfigurations",
                async dir => {
                    var items = await importService.ReadDeviceConfigurationsFromFolderAsync(dir);
                    foreach (var item in items)
                    {
                        try { await importService.ImportDeviceConfigurationAsync(item, migrationTable); results.Add(new(item.OdataType ?? "DeviceConfiguration", item.DisplayName ?? "", true, null)); }
                        catch (Exception ex) { results.Add(new("Device Configuration", item.DisplayName ?? "", false, ex.Message)); }
                    }
                });

        if (ShouldImport("Compliance Policies"))
            await ImportTypeAsync("Compliance Policies", "CompliancePolicies",
                async dir => {
                    var items = await importService.ReadCompliancePoliciesFromFolderAsync(dir);
                    foreach (var item in items)
                    {
                        try { await importService.ImportCompliancePolicyAsync(item, migrationTable); results.Add(new("Compliance Policy", item.Policy?.DisplayName ?? "", true, null)); }
                        catch (Exception ex) { results.Add(new("Compliance Policy", item.Policy?.DisplayName ?? "", false, ex.Message)); }
                    }
                });

        if (ShouldImport("Settings Catalog"))
            await ImportTypeAsync("Settings Catalog", "SettingsCatalog",
                async dir => {
                    var items = await importService.ReadSettingsCatalogPoliciesFromFolderAsync(dir);
                    foreach (var item in items)
                    {
                        try { await importService.ImportSettingsCatalogPolicyAsync(item, migrationTable); results.Add(new("Settings Catalog", item.Policy?.Name ?? "", true, null)); }
                        catch (Exception ex) { results.Add(new("Settings Catalog", item.Policy?.Name ?? "", false, ex.Message)); }
                    }
                });

        if (ShouldImport("Scope Tags"))
            await ImportTypeAsync("Scope Tags", "ScopeTags",
                async dir => {
                    var items = await importService.ReadScopeTagsFromFolderAsync(dir);
                    foreach (var item in items)
                    {
                        try { await importService.ImportScopeTagAsync(item, migrationTable); results.Add(new("Scope Tag", item.DisplayName ?? "", true, null)); }
                        catch (Exception ex) { results.Add(new("Scope Tag", item.DisplayName ?? "", false, ex.Message)); }
                    }
                });

        if (ShouldImport("Role Definitions"))
            await ImportTypeAsync("Role Definitions", "RoleDefinitions",
                async dir => {
                    var items = await importService.ReadRoleDefinitionsFromFolderAsync(dir);
                    foreach (var item in items)
                    {
                        try { await importService.ImportRoleDefinitionAsync(item, migrationTable); results.Add(new("Role Definition", item.DisplayName ?? "", true, null)); }
                        catch (Exception ex) { results.Add(new("Role Definition", item.DisplayName ?? "", false, ex.Message)); }
                    }
                });

        if (ShouldImport("Terms & Conditions"))
            await ImportTypeAsync("Terms & Conditions", "TermsAndConditions",
                async dir => {
                    var items = await importService.ReadTermsAndConditionsFromFolderAsync(dir);
                    foreach (var item in items)
                    {
                        try { await importService.ImportTermsAndConditionsAsync(item, migrationTable); results.Add(new("Terms & Conditions", item.DisplayName ?? "", true, null)); }
                        catch (Exception ex) { results.Add(new("Terms & Conditions", item.DisplayName ?? "", false, ex.Message)); }
                    }
                });

        await _exportService.SaveMigrationTableAsync(migrationTable, folderPath);

        return new ImportResult(
            results.ToArray(),
            results.Count(r => r.Success),
            results.Count(r => !r.Success));

        bool ShouldImport(string label) =>
            selectedTypes.Count == 0 || selectedTypes.Contains(label);

        async Task ImportTypeAsync(string label, string subfolder, Func<string, Task> action)
        {
            var dir = Path.Combine(folderPath, subfolder);
            if (Directory.Exists(dir))
                await action(folderPath);
        }
    }
}

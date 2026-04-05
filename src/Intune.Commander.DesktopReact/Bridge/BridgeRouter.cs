using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using Intune.Commander.DesktopReact.Services;
using Microsoft.Web.WebView2.Core;

namespace Intune.Commander.DesktopReact.Bridge;

public class BridgeRouter : IBridgeService
{
    private CoreWebView2? _webView;
    private DevWebSocketServer? _devWs;
    private readonly ProfileBridgeService _profileBridge;
    private readonly AuthBridgeService _authBridge;
    private readonly NavigationBridgeService _navBridge;
    private readonly ShellStateBridgeService _shellBridge;
    private readonly SettingsCatalogBridgeService _settingsCatalogBridge;
    private readonly DeviceHealthScriptBridgeService _healthScriptBridge;
    private readonly DeviceBridgeService _deviceBridge;
    private readonly SearchBridgeService _searchBridge;
    private readonly CacheSyncBridgeService _cacheSyncBridge;
    private readonly DashboardBridgeService _dashboardBridge;
    private readonly ApplicationBridgeService _applicationBridge;
    private readonly ApplicationAssignmentsBridgeService _applicationAssignmentsBridge;
    private readonly BulkAppAssignmentBridgeService _bulkAppAssignmentBridge;
    private readonly AppProtectionPolicyBridgeService _appProtectionPolicyBridge;
    private readonly ManagedDeviceAppConfigurationBridgeService _managedDeviceAppConfigurationBridge;
    private readonly TargetedManagedAppConfigurationBridgeService _targetedManagedAppConfigurationBridge;
    private readonly VppTokenBridgeService _vppTokenBridge;
    private readonly ConditionalAccessBridgeService _conditionalAccessBridge;
    private readonly SecurityPostureBridgeService _securityPostureBridge;
    private readonly AssignmentExplorerBridgeService _assignmentExplorerBridge;
    private readonly ScriptsHubBridgeService _scriptsHubBridge;
    private readonly PolicyComparisonBridgeService _policyComparisonBridge;
    private readonly DeviceConfigBridgeService _deviceConfigBridge;
    private readonly CompliancePolicyBridgeService _compliancePolicyBridge;
    private readonly EndpointSecurityBridgeService _endpointSecurityBridge;
    private readonly EnrollmentBridgeService _enrollmentBridge;
    private readonly DialogBridgeService _dialogBridge;
    private readonly GroupBridgeService _groupBridge;
    private readonly DriftDetectionBridgeService _driftDetectionBridge;
    private readonly ExportImportBridgeService _exportImportBridge;
    private readonly TenantAdminBridgeService _tenantAdminBridge;

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public BridgeRouter(
        ProfileBridgeService profileBridge,
        AuthBridgeService authBridge,
        NavigationBridgeService navBridge,
        ShellStateBridgeService shellBridge,
        SettingsCatalogBridgeService settingsCatalogBridge,
        DeviceHealthScriptBridgeService healthScriptBridge,
        DeviceBridgeService deviceBridge,
        SearchBridgeService searchBridge,
        CacheSyncBridgeService cacheSyncBridge,
        DashboardBridgeService dashboardBridge,
        ApplicationBridgeService applicationBridge,
        ApplicationAssignmentsBridgeService applicationAssignmentsBridge,
        BulkAppAssignmentBridgeService bulkAppAssignmentBridge,
        AppProtectionPolicyBridgeService appProtectionPolicyBridge,
        ManagedDeviceAppConfigurationBridgeService managedDeviceAppConfigurationBridge,
        TargetedManagedAppConfigurationBridgeService targetedManagedAppConfigurationBridge,
        VppTokenBridgeService vppTokenBridge,
        ConditionalAccessBridgeService conditionalAccessBridge,
        SecurityPostureBridgeService securityPostureBridge,
        AssignmentExplorerBridgeService assignmentExplorerBridge,
        ScriptsHubBridgeService scriptsHubBridge,
        PolicyComparisonBridgeService policyComparisonBridge,
        DeviceConfigBridgeService deviceConfigBridge,
        CompliancePolicyBridgeService compliancePolicyBridge,
        EndpointSecurityBridgeService endpointSecurityBridge,
        EnrollmentBridgeService enrollmentBridge,
        DialogBridgeService dialogBridge,
        GroupBridgeService groupBridge,
        DriftDetectionBridgeService driftDetectionBridge,
        ExportImportBridgeService exportImportBridge,
        TenantAdminBridgeService tenantAdminBridge)
    {
        _profileBridge = profileBridge;
        _authBridge = authBridge;
        _navBridge = navBridge;
        _shellBridge = shellBridge;
        _settingsCatalogBridge = settingsCatalogBridge;
        _healthScriptBridge = healthScriptBridge;
        _deviceBridge = deviceBridge;
        _searchBridge = searchBridge;
        _cacheSyncBridge = cacheSyncBridge;
        _dashboardBridge = dashboardBridge;
        _applicationBridge = applicationBridge;
        _applicationAssignmentsBridge = applicationAssignmentsBridge;
        _bulkAppAssignmentBridge = bulkAppAssignmentBridge;
        _appProtectionPolicyBridge = appProtectionPolicyBridge;
        _managedDeviceAppConfigurationBridge = managedDeviceAppConfigurationBridge;
        _targetedManagedAppConfigurationBridge = targetedManagedAppConfigurationBridge;
        _vppTokenBridge = vppTokenBridge;
        _conditionalAccessBridge = conditionalAccessBridge;
        _securityPostureBridge = securityPostureBridge;
        _assignmentExplorerBridge = assignmentExplorerBridge;
        _scriptsHubBridge = scriptsHubBridge;
        _policyComparisonBridge = policyComparisonBridge;
        _deviceConfigBridge = deviceConfigBridge;
        _compliancePolicyBridge = compliancePolicyBridge;
        _endpointSecurityBridge = endpointSecurityBridge;
        _enrollmentBridge = enrollmentBridge;
        _dialogBridge = dialogBridge;
        _groupBridge = groupBridge;
        _driftDetectionBridge = driftDetectionBridge;
        _exportImportBridge = exportImportBridge;
        _tenantAdminBridge = tenantAdminBridge;
    }

    public void Initialize(CoreWebView2 webView)
    {
        _webView = webView;
        _webView.WebMessageReceived += OnWebMessageReceived;

        // Give bridge services access to push events
        _profileBridge.SetBridge(this);
        _authBridge.SetBridge(this);
        _shellBridge.SetBridge(this);
        _cacheSyncBridge.SetBridge(this);
    }

    /// <summary>
    /// Start the dev WebSocket server so the Vite dev server (browser)
    /// can talk to the .NET backend without WebView2.
    /// </summary>
    [System.Diagnostics.Conditional("DEBUG")]
    public void StartDevWebSocket()
    {
        _profileBridge.SetBridge(this);
        _authBridge.SetBridge(this);
        _shellBridge.SetBridge(this);
        _cacheSyncBridge.SetBridge(this);

        _devWs = new DevWebSocketServer(this);
        _devWs.Start();
    }

    /// <summary>
    /// Dispatch a bridge command and return the result.
    /// Used by both the WebView2 message handler and the dev WebSocket server.
    /// </summary>
    public async Task<object?> DispatchCommandAsync(BridgeCommand command)
    {
        return command.Command switch
        {
            "profiles.load" => await _profileBridge.LoadAsync(),
            "profiles.save" => await _profileBridge.SaveAsync(command.Payload),
            "profiles.delete" => await _profileBridge.DeleteAsync(command.Payload),
            "profiles.import" => await _profileBridge.ImportAsync(),
            "auth.connect" => await _authBridge.ConnectAsync(command.Payload),
            "auth.disconnect" => await _authBridge.DisconnectAsync(),
            "nav.getCategories" => _navBridge.GetCategories(),
            "shell.getState" => _shellBridge.GetState(),
            "settingsCatalog.list" => await _settingsCatalogBridge.ListAsync(),
            "settingsCatalog.getDetail" => await _settingsCatalogBridge.GetDetailAsync(command.Payload),
            "healthScripts.list" => await _healthScriptBridge.ListAsync(),
            "healthScripts.getDetail" => await _healthScriptBridge.GetDetailAsync(command.Payload),
            "healthScripts.update" => await _healthScriptBridge.UpdateAsync(command.Payload),
            "healthScripts.deploy" => await _healthScriptBridge.DeployAsync(command.Payload),
            "healthScripts.refreshRunStates" => await _healthScriptBridge.RefreshRunStatesAsync(command.Payload),
            "devices.search" => await _deviceBridge.SearchAsync(command.Payload),
            "search.query" => await _searchBridge.SearchAsync(command.Payload),
            "cache.sync" => await _cacheSyncBridge.SyncAllAsync(command.Payload),
            "cache.status" => await _cacheSyncBridge.GetStatusAsync(command.Payload),
            "cache.invalidate" => await _cacheSyncBridge.InvalidateAsync(command.Payload),
            "dashboard.complianceSummary" => await _dashboardBridge.GetComplianceSummaryAsync(command.Payload),
            "apps.list" => await _applicationBridge.ListAsync(),
            "apps.getDetail" => await _applicationBridge.GetDetailAsync(command.Payload),
            "appAssignments.list" => await _applicationAssignmentsBridge.ListAsync(),
            "appAssignments.getDetail" => await _applicationAssignmentsBridge.GetDetailAsync(command.Payload),
            "bulkAppAssignments.bootstrap" => await _bulkAppAssignmentBridge.GetBootstrapAsync(),
            "bulkAppAssignments.apply" => await _bulkAppAssignmentBridge.ApplyAsync(command.Payload),
            "appProtectionPolicies.list" => await _appProtectionPolicyBridge.ListAsync(),
            "appProtectionPolicies.getDetail" => await _appProtectionPolicyBridge.GetDetailAsync(command.Payload),
            "managedDeviceAppConfigurations.list" => await _managedDeviceAppConfigurationBridge.ListAsync(),
            "managedDeviceAppConfigurations.getDetail" => await _managedDeviceAppConfigurationBridge.GetDetailAsync(command.Payload),
            "targetedManagedAppConfigurations.list" => await _targetedManagedAppConfigurationBridge.ListAsync(),
            "targetedManagedAppConfigurations.getDetail" => await _targetedManagedAppConfigurationBridge.GetDetailAsync(command.Payload),
            "vppTokens.list" => await _vppTokenBridge.ListAsync(),
            "vppTokens.getDetail" => await _vppTokenBridge.GetDetailAsync(command.Payload),
            "conditionalAccess.list" => await _conditionalAccessBridge.ListAsync(),
            "conditionalAccess.getDetail" => await _conditionalAccessBridge.GetDetailAsync(command.Payload),
            "securityPosture.summary" => await _securityPostureBridge.GetSummaryAsync(),
            "securityPosture.detail" => await _securityPostureBridge.GetDetailAsync(),
            "assignments.searchGroups" => await _assignmentExplorerBridge.SearchGroupsAsync(command.Payload),
            "assignments.runReport" => await _assignmentExplorerBridge.RunReportAsync(command.Payload),
            "scripts.listAll" => await _scriptsHubBridge.ListAllAsync(),
            "scripts.getDetail" => await _scriptsHubBridge.GetDetailAsync(command.Payload),
            "policyComparison.list" => await _policyComparisonBridge.ListPoliciesAsync(command.Payload),
            "policyComparison.compare" => await _policyComparisonBridge.CompareAsync(command.Payload),
            "deviceConfig.list" => await _deviceConfigBridge.ListAsync(),
            "deviceConfig.getDetail" => await _deviceConfigBridge.GetDetailAsync(command.Payload),
            "compliance.list" => await _compliancePolicyBridge.ListAsync(),
            "compliance.getDetail" => await _compliancePolicyBridge.GetDetailAsync(command.Payload),
            "endpointSecurity.list" => await _endpointSecurityBridge.ListAsync(),
            "endpointSecurity.getDetail" => await _endpointSecurityBridge.GetDetailAsync(command.Payload),
            "enrollment.list" => await _enrollmentBridge.ListAsync(),
            "enrollment.getDetail" => await _enrollmentBridge.GetDetailAsync(command.Payload),
            "groups.list" => await _groupBridge.ListAsync(),
            "groups.search" => await _groupBridge.SearchAsync(command.Payload),
            "groups.getDetail" => await _groupBridge.GetDetailAsync(command.Payload),
            // Drift Detection
            "drift.compare" => await _driftDetectionBridge.CompareAsync(command.Payload),
            // Export / Import
            "export.run" => await _exportImportBridge.RunExportAsync(command.Payload),
            "import.preview" => await _exportImportBridge.PreviewImportAsync(command.Payload),
            "import.run" => await _exportImportBridge.RunImportAsync(command.Payload),
            // Tenant Admin
            "tenantAdmin.scopeTags.list" => await _tenantAdminBridge.ListScopeTagsAsync(),
            "tenantAdmin.scopeTags.getDetail" => await _tenantAdminBridge.GetScopeTagDetailAsync(command.Payload),
            "tenantAdmin.roles.list" => await _tenantAdminBridge.ListRolesAsync(),
            "tenantAdmin.roles.getDetail" => await _tenantAdminBridge.GetRoleDetailAsync(command.Payload),
            "tenantAdmin.intuneBranding.list" => await _tenantAdminBridge.ListIntuneBrandingAsync(),
            "tenantAdmin.intuneBranding.getDetail" => await _tenantAdminBridge.GetIntuneBrandingDetailAsync(command.Payload),
            "tenantAdmin.azureBranding.list" => await _tenantAdminBridge.ListAzureBrandingAsync(),
            "tenantAdmin.azureBranding.getDetail" => await _tenantAdminBridge.GetAzureBrandingDetailAsync(command.Payload),
            "tenantAdmin.termsAndConditions.list" => await _tenantAdminBridge.ListTermsAndConditionsAsync(),
            "tenantAdmin.termsAndConditions.getDetail" => await _tenantAdminBridge.GetTermsAndConditionsDetailAsync(command.Payload),
            "tenantAdmin.termsOfUse.list" => await _tenantAdminBridge.ListTermsOfUseAsync(),
            "tenantAdmin.termsOfUse.getDetail" => await _tenantAdminBridge.GetTermsOfUseDetailAsync(command.Payload),
            "tenantAdmin.admxFiles.list" => await _tenantAdminBridge.ListAdmxFilesAsync(),
            "tenantAdmin.admxFiles.getDetail" => await _tenantAdminBridge.GetAdmxFileDetailAsync(command.Payload),
            "tenantAdmin.reusableSettings.list" => await _tenantAdminBridge.ListReusableSettingsAsync(),
            "tenantAdmin.reusableSettings.getDetail" => await _tenantAdminBridge.GetReusableSettingDetailAsync(command.Payload),
            "tenantAdmin.notifications.list" => await _tenantAdminBridge.ListNotificationsAsync(),
            "tenantAdmin.notifications.getDetail" => await _tenantAdminBridge.GetNotificationDetailAsync(command.Payload),
            "tenantAdmin.policySets.list" => await _tenantAdminBridge.ListPolicySetsAsync(),
            "tenantAdmin.policySets.getDetail" => await _tenantAdminBridge.GetPolicySetDetailAsync(command.Payload),
            // Dialogs
            "dialog.pickFolder" => await _dialogBridge.PickFolderAsync(),
            "dialog.pickFile" => await _dialogBridge.PickFileAsync(
                GetStringProp(command.Payload, "filter"),
                GetStringProp(command.Payload, "title")),
            "dialog.saveFile" => await _dialogBridge.SaveFileAsync(
                GetStringProp(command.Payload, "filter"),
                GetStringProp(command.Payload, "title"),
                GetStringProp(command.Payload, "defaultFileName")),
            _ => throw new NotSupportedException($"Unknown command: {command.Command}")
        };
    }

    private async void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        BridgeCommand? command;
        try
        {
            var json = e.WebMessageAsJson;
            command = JsonSerializer.Deserialize<BridgeCommand>(json, JsonOptions);
        }
        catch
        {
            return; // Malformed message — ignore
        }

        if (command is null || command.Protocol != "ic/1")
            return;

        try
        {
            var result = await DispatchCommandAsync(command);
            await SendResponseAsync(command.Id, true, result);
        }
        catch (Exception ex)
        {
            await SendResponseAsync(command.Id, false, null, ex.Message);
        }
    }

    public async Task SendEventAsync(string eventName, object payload)
    {
        var evt = BridgeEvent.Create(eventName, payload);
        var json = JsonSerializer.Serialize(evt, JsonOptions);

        // Send to WebView2 if available
        if (_webView is not null)
        {
            Application.Current.Dispatcher.Invoke(() => _webView.PostWebMessageAsJson(json));
        }

        // Also broadcast to any connected dev WebSocket clients
        if (_devWs is not null)
        {
            await _devWs.BroadcastEventAsync(json);
        }
    }

    public Task SendResponseAsync(string id, bool success, object? payload, string? error = null)
    {
        if (_webView is null) return Task.CompletedTask;

        var response = success
            ? BridgeResponse.Ok(id, payload)
            : BridgeResponse.Fail(id, error ?? "Unknown error");

        var json = JsonSerializer.Serialize(response, JsonOptions);

        Application.Current.Dispatcher.Invoke(() => _webView.PostWebMessageAsJson(json));
        return Task.CompletedTask;
    }

    private static string? GetStringProp(JsonElement? payload, string propertyName)
    {
        if (payload is null) return null;
        return payload.Value.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;
    }
}

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
    private readonly ConditionalAccessBridgeService _conditionalAccessBridge;

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
        ConditionalAccessBridgeService conditionalAccessBridge)
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
        _conditionalAccessBridge = conditionalAccessBridge;
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
            "conditionalAccess.list" => await _conditionalAccessBridge.ListAsync(),
            "conditionalAccess.getDetail" => await _conditionalAccessBridge.GetDetailAsync(command.Payload),
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
}

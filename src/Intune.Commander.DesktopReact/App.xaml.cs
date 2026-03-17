using System.Windows;
using Intune.Commander.Core.Extensions;
using Intune.Commander.DesktopReact.Bridge;
using Intune.Commander.DesktopReact.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Intune.Commander.DesktopReact;

public partial class App : Application
{
    public static ServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        services.AddIntuneCommanderCore();

        // Bridge services
        services.AddSingleton<ShellStateBridgeService>();
        services.AddSingleton<ProfileBridgeService>();
        services.AddSingleton<AuthBridgeService>();
        services.AddSingleton<NavigationBridgeService>();
        services.AddSingleton<SettingsCatalogBridgeService>();
        services.AddSingleton<DeviceHealthScriptBridgeService>();
        services.AddSingleton<DeviceBridgeService>();
        services.AddSingleton<SearchBridgeService>();
        services.AddSingleton<CacheSyncBridgeService>();
        services.AddSingleton<DashboardBridgeService>();
        services.AddSingleton<ApplicationBridgeService>();
        services.AddSingleton<ApplicationAssignmentsBridgeService>();
        services.AddSingleton<BulkAppAssignmentBridgeService>();
        services.AddSingleton<AppProtectionPolicyBridgeService>();
        services.AddSingleton<ManagedDeviceAppConfigurationBridgeService>();
        services.AddSingleton<TargetedManagedAppConfigurationBridgeService>();
        services.AddSingleton<VppTokenBridgeService>();
        services.AddSingleton<ConditionalAccessBridgeService>();
        services.AddSingleton<SecurityPostureBridgeService>();
        services.AddSingleton<AssignmentExplorerBridgeService>();
        services.AddSingleton<ScriptsHubBridgeService>();
        services.AddSingleton<PolicyComparisonBridgeService>();
        services.AddSingleton<DeviceConfigBridgeService>();
        services.AddSingleton<CompliancePolicyBridgeService>();
        services.AddSingleton<EndpointSecurityBridgeService>();
        services.AddSingleton<EnrollmentBridgeService>();
        services.AddSingleton<DialogBridgeService>();
        services.AddSingleton<GroupBridgeService>();
        services.AddSingleton<BridgeRouter>();

        Services = services.BuildServiceProvider();

        // In DEBUG builds, start a WebSocket server so the Vite dev server
        // (browser on :5173) can talk to the .NET backend without WebView2.
        Services.GetRequiredService<BridgeRouter>().StartDevWebSocket();
    }
}

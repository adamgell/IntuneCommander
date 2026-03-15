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
        services.AddSingleton<ConditionalAccessBridgeService>();
        services.AddSingleton<SecurityPostureBridgeService>();
        services.AddSingleton<AssignmentExplorerBridgeService>();
        services.AddSingleton<BridgeRouter>();

        Services = services.BuildServiceProvider();

        // In DEBUG builds, start a WebSocket server so the Vite dev server
        // (browser on :5173) can talk to the .NET backend without WebView2.
        Services.GetRequiredService<BridgeRouter>().StartDevWebSocket();
    }
}

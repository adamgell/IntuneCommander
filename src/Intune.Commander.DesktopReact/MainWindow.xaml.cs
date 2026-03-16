using System.Diagnostics;
using System.IO;
using System.Windows;
using Intune.Commander.Core.Services;
using Intune.Commander.DesktopReact.Bridge;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Web.WebView2.Core;

namespace Intune.Commander.DesktopReact;

public partial class MainWindow : Window
{
    private BridgeRouter? _bridge;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Explicit user data folder in %LocalAppData% — required when the exe is in
        // Program Files (no write access next to exe). Without this, WebView2 silently
        // fails to initialize and shows a white box.
        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Intune.Commander", "WebView2");

        var env = await CoreWebView2Environment.CreateAsync(
            browserExecutableFolder: null,
            userDataFolder: userDataFolder);

        await webView.EnsureCoreWebView2Async(env);

        var coreWebView = webView.CoreWebView2;

        // Security: disable context menu and status bar
        coreWebView.Settings.AreDefaultContextMenusEnabled = false;
        coreWebView.Settings.IsStatusBarEnabled = false;

#if !DEBUG
        coreWebView.Settings.AreDevToolsEnabled = false;
#endif

        // Security: block navigation away from app content
        coreWebView.NavigationStarting += OnNavigationStarting;

        // Initialize bridge
        _bridge = App.Services.GetRequiredService<BridgeRouter>();
        _bridge.Initialize(coreWebView);

        // Warn if cache is unavailable — most likely another instance is running
        var cache = App.Services.GetRequiredService<ICacheService>();
        if (!cache.IsAvailable)
        {
            MessageBox.Show(
                "The cache database is locked by another running instance of Intune Commander.\n\n" +
                "The app will continue to work, but caching is disabled for this session.",
                "Cache Unavailable",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

#if DEBUG
        coreWebView.Navigate("http://localhost:5173");
#else
        // Vite ES modules require a real origin — file:// causes null-origin CORS
        // failures for module imports. Virtual host mapping serves as https://.
        var wwwroot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        coreWebView.SetVirtualHostNameToFolderMapping(
            "app.intunecommander.local",
            wwwroot,
            CoreWebView2HostResourceAccessKind.Allow);
        coreWebView.Navigate("https://app.intunecommander.local/index.html");
#endif
    }

    private static void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs args)
    {
        var uri = new Uri(args.Uri);

        // Allow virtual host serving app content
        if (uri.Host == "app.intunecommander.local")
            return;

        // Allow dev server
        if (uri.Host == "localhost" && uri.Port == 5173)
            return;

        // Block everything else — open in system browser
        args.Cancel = true;
        Process.Start(new ProcessStartInfo(args.Uri) { UseShellExecute = true });
    }
}

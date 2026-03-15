using System.Diagnostics;
using System.IO;
using System.Windows;
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
        var userDataFolder = GetWebViewUserDataFolder();
        CoreWebView2Environment environment;

        try
        {
            environment = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
        }
        catch (UnauthorizedAccessException)
        {
            var fallbackFolder = Path.Combine(Path.GetTempPath(), "IntuneCommander", "WebView2");
            Directory.CreateDirectory(fallbackFolder);
            environment = await CoreWebView2Environment.CreateAsync(userDataFolder: fallbackFolder);
        }

        await webView.EnsureCoreWebView2Async(environment);

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

#if DEBUG
        coreWebView.Navigate("http://localhost:5173");
#else
        var appDir = AppContext.BaseDirectory;
        var indexPath = Path.Combine(appDir, "wwwroot", "index.html");
        coreWebView.Navigate(new Uri(indexPath).AbsoluteUri);
#endif
    }

    private static string GetWebViewUserDataFolder()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var folder = Path.Combine(localAppData, "IntuneCommander", "DesktopReact", "WebView2");
        Directory.CreateDirectory(folder);
        return folder;
    }

    private static void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs args)
    {
        var uri = new Uri(args.Uri);

        // Allow local content and dev server
        if (uri.Scheme == "file")
            return;

        if (uri.Host == "localhost" && uri.Port == 5173)
            return;

        // Block everything else — open in system browser
        args.Cancel = true;
        Process.Start(new ProcessStartInfo(args.Uri) { UseShellExecute = true });
    }
}

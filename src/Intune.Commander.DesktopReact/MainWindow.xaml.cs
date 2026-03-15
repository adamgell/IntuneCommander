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
        await webView.EnsureCoreWebView2Async();

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

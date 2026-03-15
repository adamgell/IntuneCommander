using Microsoft.Web.WebView2.Core;

namespace Intune.Commander.DesktopReact.Bridge;

public interface IBridgeService
{
    void Initialize(CoreWebView2 webView);
    Task SendEventAsync(string eventName, object payload);
    Task SendResponseAsync(string id, bool success, object? payload, string? error = null);
}

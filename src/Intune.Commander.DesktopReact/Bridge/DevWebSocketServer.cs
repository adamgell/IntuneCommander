using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Intune.Commander.DesktopReact.Bridge;

/// <summary>
/// Lightweight WebSocket server for dev mode.
/// Lets the Vite dev server (browser) talk to the .NET backend
/// using the same ic/1 bridge protocol that WebView2 uses.
/// Only started in DEBUG builds.
/// </summary>
public sealed class DevWebSocketServer : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly BridgeRouter _router;
    private readonly CancellationTokenSource _cts = new();
    private readonly List<WebSocket> _clients = [];
    private readonly Lock _clientsLock = new();
    private Task? _listenTask;

    private const int Port = 5100;
    private const int MaxMessageSize = 1024 * 1024; // 1 MB

    public DevWebSocketServer(BridgeRouter router)
    {
        _router = router;
    }

    public void Start()
    {
        _listener.Prefixes.Add($"http://localhost:{Port}/ws/");
        _listener.Start();
        _listenTask = AcceptLoopAsync(_cts.Token);
        System.Diagnostics.Debug.WriteLine($"[DevWS] WebSocket bridge listening on ws://localhost:{Port}/ws/");
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var context = await _listener.GetContextAsync();

                if (!context.Request.IsWebSocketRequest)
                {
                    // Return CORS-friendly 400 for non-WS requests
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                    continue;
                }

                var wsContext = await context.AcceptWebSocketAsync(null);
                var ws = wsContext.WebSocket;

                lock (_clientsLock) _clients.Add(ws);

                _ = HandleClientAsync(ws, ct);
            }
            catch (HttpListenerException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DevWS] Accept error: {ex.Message}");
            }
        }
    }

    private async Task HandleClientAsync(WebSocket ws, CancellationToken ct)
    {
        var buffer = new byte[16 * 1024];

        try
        {
            while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var result = await ws.ReceiveAsync(buffer, ct);

                if (result.MessageType == WebSocketMessageType.Close)
                    break;

                if (result.Count > MaxMessageSize)
                {
                    System.Diagnostics.Debug.WriteLine($"[DevWS] Message too large ({result.Count} bytes), skipping");
                    continue;
                }

                var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                await DispatchCommandAsync(ws, json);
            }
        }
        catch (WebSocketException) { }
        catch (OperationCanceledException) { }
        finally
        {
            lock (_clientsLock) _clients.Remove(ws);
            if (ws.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                try { await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None); }
                catch { /* best-effort */ }
            }
            ws.Dispose();
        }
    }

    private async Task DispatchCommandAsync(WebSocket ws, string json)
    {
        BridgeCommand? command;
        try
        {
            command = JsonSerializer.Deserialize<BridgeCommand>(json, BridgeRouter.JsonOptions);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DevWS] Failed to deserialize command: {ex.Message}");
            return;
        }

        if (command is null || command.Protocol != "ic/1")
            return;

        try
        {
            var result = await _router.DispatchCommandAsync(command);
            var response = BridgeResponse.Ok(command.Id, result);
            await SendJsonAsync(ws, JsonSerializer.Serialize(response, BridgeRouter.JsonOptions));
        }
        catch (Exception ex)
        {
            var response = BridgeResponse.Fail(command.Id, ex.Message);
            await SendJsonAsync(ws, JsonSerializer.Serialize(response, BridgeRouter.JsonOptions));
        }
    }

    /// <summary>
    /// Broadcast an event to all connected WebSocket clients.
    /// Called by BridgeRouter when it sends events.
    /// </summary>
    public async Task BroadcastEventAsync(string eventJson)
    {
        List<WebSocket> snapshot;
        lock (_clientsLock) snapshot = [.. _clients];

        foreach (var ws in snapshot)
        {
            if (ws.State == WebSocketState.Open)
            {
                try { await SendJsonAsync(ws, eventJson); }
                catch { /* client may have disconnected */ }
            }
        }
    }

    private static async Task SendJsonAsync(WebSocket ws, string json)
    {
        if (ws.State != WebSocketState.Open) return;
        var bytes = Encoding.UTF8.GetBytes(json);
        await ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _listener.Stop();
        _listener.Close();

        lock (_clientsLock)
        {
            foreach (var ws in _clients)
                ws.Dispose();
            _clients.Clear();
        }

        _cts.Dispose();
    }
}

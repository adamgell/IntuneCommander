using System.Text.Json;
using Intune.Commander.Core.Services;
using Intune.Commander.DesktopReact.Models;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.DesktopReact.Services;

public class DeviceBridgeService
{
    private readonly AuthBridgeService _authBridge;
    private readonly ICacheService _cache;
    private readonly ShellStateBridgeService _shellState;

    private const string CacheKeyDevices = "ManagedDevices";

    private IDeviceService? _service;

    public DeviceBridgeService(
        AuthBridgeService authBridge,
        ICacheService cache,
        ShellStateBridgeService shellState)
    {
        _authBridge = authBridge;
        _cache = cache;
        _shellState = shellState;
    }

    private IDeviceService GetService()
    {
        var client = _authBridge.GraphClient
            ?? throw new InvalidOperationException("Not connected — authenticate first");

        _service ??= new DeviceService(client);
        return _service;
    }

    public void Reset() => _service = null;

    private string? GetTenantId() => _shellState.ActiveProfile?.TenantId;

    public async Task<object> SearchAsync(JsonElement? payload)
    {
        var query = "";
        if (payload is not null && payload.Value.TryGetProperty("query", out var qProp))
            query = qProp.GetString() ?? "";

        var tenantId = GetTenantId();

        // Try cache first — filter cached devices by name if query is provided
        if (tenantId is not null)
        {
            var cached = _cache.Get<ManagedDevice>(tenantId, CacheKeyDevices);
            if (cached is { Count: > 0 })
            {
                var filtered = string.IsNullOrWhiteSpace(query)
                    ? cached
                    : cached.Where(d =>
                        d.DeviceName?.Contains(query, StringComparison.OrdinalIgnoreCase) == true)
                        .ToList();

                return MapDevices(filtered);
            }
        }

        // Cache miss — fetch from Graph
        var service = GetService();
        var devices = await service.SearchDevicesAsync(query);

        // Cache full device list if this was an unfiltered fetch
        if (string.IsNullOrWhiteSpace(query) && tenantId is not null)
            _cache.Set(tenantId, CacheKeyDevices, devices);

        return MapDevices(devices);
    }

    private static DeviceSearchResult[] MapDevices(List<ManagedDevice> devices)
    {
        return devices.Where(d => d.Id is not null).Select(d => new DeviceSearchResult(
            Id: d.Id!,
            DeviceName: d.DeviceName ?? "",
            OperatingSystem: d.OperatingSystem,
            OsVersion: d.OsVersion,
            Model: d.Model,
            Manufacturer: d.Manufacturer,
            LastSyncDateTime: d.LastSyncDateTime?.ToString("o")
        )).ToArray();
    }
}

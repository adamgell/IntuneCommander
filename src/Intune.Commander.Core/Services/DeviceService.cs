using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;
using Microsoft.Kiota.Abstractions;

namespace Intune.Commander.Core.Services;

public class DeviceService(GraphServiceClient graphClient) : IDeviceService
{
    private readonly GraphServiceClient _graphClient = graphClient;

    private static readonly string[] DeviceSelect =
        ["id", "deviceName", "operatingSystem", "osVersion", "lastSyncDateTime", "managementState", "model", "manufacturer"];

    private const string WindowsFilter = "operatingSystem eq 'Windows'";

    public async Task<List<ManagedDevice>> SearchDevicesAsync(string query, CancellationToken cancellationToken = default)
    {
        var result = new List<ManagedDevice>();
        var trimmed = (query ?? "").Trim();

        // Empty query → list all Windows devices
        if (string.IsNullOrEmpty(trimmed))
        {
            var all = await _graphClient.DeviceManagement.ManagedDevices.GetAsync(req =>
            {
                req.QueryParameters.Filter = WindowsFilter;
                req.QueryParameters.Select = DeviceSelect;
                req.QueryParameters.Top = 50;
                req.QueryParameters.Orderby = ["deviceName"];
            }, cancellationToken);

            if (all?.Value != null)
                result.AddRange(all.Value);
            return result;
        }

        // OData injection guard
        if (trimmed.Any(c => c is '\'' or '"' or '$' or '&'))
            throw new ArgumentException("Query contains invalid characters.");

        var escaped = trimmed.Replace("\"", "\\\"");
        try
        {
            var response = await _graphClient.DeviceManagement.ManagedDevices.GetAsync(req =>
            {
                req.QueryParameters.Search = $"\"deviceName:{escaped}\"";
                req.QueryParameters.Filter = WindowsFilter;
                req.QueryParameters.Select = DeviceSelect;
                req.QueryParameters.Top = 50;
                req.QueryParameters.Orderby = ["deviceName"];
                req.Headers.Add("ConsistencyLevel", "eventual");
                req.QueryParameters.Count = true;
            }, cancellationToken);

            if (response?.Value != null)
                result.AddRange(response.Value);
        }
        catch
        {
            // $search may not be available; fall back to contains $filter
            var escapedFilter = trimmed.Replace("'", "''");
            try
            {
                var containsFallback = await _graphClient.DeviceManagement.ManagedDevices.GetAsync(req =>
                {
                    req.QueryParameters.Filter = $"contains(deviceName,'{escapedFilter}') and {WindowsFilter}";
                    req.QueryParameters.Select = DeviceSelect;
                    req.QueryParameters.Top = 50;
                    req.Headers.Add("ConsistencyLevel", "eventual");
                    req.QueryParameters.Count = true;
                }, cancellationToken);

                if (containsFallback?.Value != null)
                    result.AddRange(containsFallback.Value);
            }
            catch
            {
                // contains not supported; last resort startsWith
                var startsWith = await _graphClient.DeviceManagement.ManagedDevices.GetAsync(req =>
                {
                    req.QueryParameters.Filter = $"startsWith(deviceName,'{escapedFilter}') and {WindowsFilter}";
                    req.QueryParameters.Select = DeviceSelect;
                    req.QueryParameters.Top = 50;
                    req.Headers.Add("ConsistencyLevel", "eventual");
                    req.QueryParameters.Count = true;
                }, cancellationToken);

                if (startsWith?.Value != null)
                    result.AddRange(startsWith.Value);
            }
        }

        return result;
    }

    public async Task<ManagedDevice?> GetDeviceAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        return await _graphClient.DeviceManagement.ManagedDevices[deviceId]
            .GetAsync(req => req.QueryParameters.Select = DeviceSelect, cancellationToken);
    }
}

using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Core.Services;

public interface IDeviceHealthScriptService
{
    Task<List<DeviceHealthScript>> ListDeviceHealthScriptsAsync(CancellationToken cancellationToken = default);
    Task<DeviceHealthScript?> GetDeviceHealthScriptAsync(string id, CancellationToken cancellationToken = default);
    Task<DeviceHealthScript> CreateDeviceHealthScriptAsync(DeviceHealthScript script, CancellationToken cancellationToken = default);
    Task<DeviceHealthScript> UpdateDeviceHealthScriptAsync(DeviceHealthScript script, CancellationToken cancellationToken = default);
    Task DeleteDeviceHealthScriptAsync(string id, CancellationToken cancellationToken = default);
}
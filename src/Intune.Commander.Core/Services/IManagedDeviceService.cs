using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Core.Services;

public interface IManagedDeviceService
{
    Task<List<ManagedDevice>> ListManagedDevicesAsync(CancellationToken cancellationToken = default);
}

using Intune.Commander.Core.Services;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.CLI.Helpers;

internal sealed class DryRunConfigurationProfileService : IConfigurationProfileService
{
    public Task<List<DeviceConfiguration>> ListDeviceConfigurationsAsync(CancellationToken cancellationToken = default) =>
        throw CreateDryRunOnlyException();

    public Task<DeviceConfiguration?> GetDeviceConfigurationAsync(string id, CancellationToken cancellationToken = default) =>
        throw CreateDryRunOnlyException();

    public Task<DeviceConfiguration> CreateDeviceConfigurationAsync(DeviceConfiguration config, CancellationToken cancellationToken = default) =>
        throw CreateDryRunOnlyException();

    public Task<DeviceConfiguration> UpdateDeviceConfigurationAsync(DeviceConfiguration config, CancellationToken cancellationToken = default) =>
        throw CreateDryRunOnlyException();

    public Task DeleteDeviceConfigurationAsync(string id, CancellationToken cancellationToken = default) =>
        throw CreateDryRunOnlyException();

    public Task<List<DeviceConfigurationAssignment>> GetAssignmentsAsync(string configId, CancellationToken cancellationToken = default) =>
        throw CreateDryRunOnlyException();

    private static InvalidOperationException CreateDryRunOnlyException() =>
        new("Dry-run validation only supports reading exported files.");
}

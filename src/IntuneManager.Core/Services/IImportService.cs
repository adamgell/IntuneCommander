using IntuneManager.Core.Models;
using Microsoft.Graph.Models;

namespace IntuneManager.Core.Services;

public interface IImportService
{
    Task<DeviceConfiguration?> ReadDeviceConfigurationAsync(string filePath, CancellationToken cancellationToken = default);
    Task<List<DeviceConfiguration>> ReadDeviceConfigurationsFromFolderAsync(string folderPath, CancellationToken cancellationToken = default);
    Task<MigrationTable> ReadMigrationTableAsync(string folderPath, CancellationToken cancellationToken = default);
    Task<DeviceConfiguration> ImportDeviceConfigurationAsync(DeviceConfiguration config, MigrationTable migrationTable, CancellationToken cancellationToken = default);
}

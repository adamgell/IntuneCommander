using IntuneManager.Core.Models;
using Microsoft.Graph.Models;

namespace IntuneManager.Core.Services;

public interface IExportService
{
    Task ExportDeviceConfigurationAsync(DeviceConfiguration config, string outputPath, MigrationTable migrationTable, CancellationToken cancellationToken = default);
    Task ExportDeviceConfigurationsAsync(IEnumerable<DeviceConfiguration> configs, string outputPath, CancellationToken cancellationToken = default);
    Task SaveMigrationTableAsync(MigrationTable table, string outputPath, CancellationToken cancellationToken = default);
}

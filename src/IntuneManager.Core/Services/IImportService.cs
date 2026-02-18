using IntuneManager.Core.Models;
using Microsoft.Graph.Beta.Models;

namespace IntuneManager.Core.Services;

public interface IImportService
{
    Task<DeviceConfiguration?> ReadDeviceConfigurationAsync(string filePath, CancellationToken cancellationToken = default);
    Task<List<DeviceConfiguration>> ReadDeviceConfigurationsFromFolderAsync(string folderPath, CancellationToken cancellationToken = default);
    Task<MigrationTable> ReadMigrationTableAsync(string folderPath, CancellationToken cancellationToken = default);
    Task<DeviceConfiguration> ImportDeviceConfigurationAsync(DeviceConfiguration config, MigrationTable migrationTable, CancellationToken cancellationToken = default);

    Task<CompliancePolicyExport?> ReadCompliancePolicyAsync(string filePath, CancellationToken cancellationToken = default);
    Task<List<CompliancePolicyExport>> ReadCompliancePoliciesFromFolderAsync(string folderPath, CancellationToken cancellationToken = default);
    Task<DeviceCompliancePolicy> ImportCompliancePolicyAsync(CompliancePolicyExport export, MigrationTable migrationTable, CancellationToken cancellationToken = default);
}

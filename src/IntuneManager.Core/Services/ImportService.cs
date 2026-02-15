using System.Text.Json;
using IntuneManager.Core.Models;
using Microsoft.Graph.Models;

namespace IntuneManager.Core.Services;

public class ImportService : IImportService
{
    private readonly IIntuneService _intuneService;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ImportService(IIntuneService intuneService)
    {
        _intuneService = intuneService;
    }

    public async Task<DeviceConfiguration?> ReadDeviceConfigurationAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        return JsonSerializer.Deserialize<DeviceConfiguration>(json, JsonOptions);
    }

    public async Task<List<DeviceConfiguration>> ReadDeviceConfigurationsFromFolderAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        var configs = new List<DeviceConfiguration>();
        var configFolder = Path.Combine(folderPath, "DeviceConfigurations");

        if (!Directory.Exists(configFolder))
            return configs;

        foreach (var file in Directory.GetFiles(configFolder, "*.json"))
        {
            var config = await ReadDeviceConfigurationAsync(file, cancellationToken);
            if (config != null)
                configs.Add(config);
        }

        return configs;
    }

    public async Task<MigrationTable> ReadMigrationTableAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(folderPath, "migration-table.json");

        if (!File.Exists(filePath))
            return new MigrationTable();

        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        return JsonSerializer.Deserialize<MigrationTable>(json, JsonOptions) ?? new MigrationTable();
    }

    public async Task<DeviceConfiguration> ImportDeviceConfigurationAsync(
        DeviceConfiguration config,
        MigrationTable migrationTable,
        CancellationToken cancellationToken = default)
    {
        var originalId = config.Id;

        // Clear the ID so Graph creates a new object
        config.Id = null;
        // Clear read-only properties that can't be set during creation
        config.CreatedDateTime = null;
        config.LastModifiedDateTime = null;
        config.Version = null;

        var created = await _intuneService.CreateDeviceConfigurationAsync(config, cancellationToken);

        // Update migration table with the mapping
        if (originalId != null && created.Id != null)
        {
            migrationTable.AddOrUpdate(new MigrationEntry
            {
                ObjectType = "DeviceConfiguration",
                OriginalId = originalId,
                NewId = created.Id,
                Name = created.DisplayName ?? "Unknown"
            });
        }

        return created;
    }
}

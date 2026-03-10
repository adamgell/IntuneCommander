using Intune.Commander.Core.Models;
using Intune.Commander.Core.Services;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Core.Tests.Services;

public sealed class ExportServiceEdgeCaseTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"ic-export-edge-{Guid.NewGuid():N}");
    private readonly ExportService _sut = new();

    public ExportServiceEdgeCaseTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public async Task ExportDeviceConfiguration_NameAndIdCollision_UsesNumericSuffix()
    {
        var table = new MigrationTable();
        var config = new DeviceConfiguration
        {
            Id = "cfg-1",
            DisplayName = "Same Name"
        };

        await _sut.ExportDeviceConfigurationAsync(config, _tempDir, table);
        await _sut.ExportDeviceConfigurationAsync(config, _tempDir, table);
        await _sut.ExportDeviceConfigurationAsync(config, _tempDir, table);

        var files = Directory.GetFiles(Path.Combine(_tempDir, "DeviceConfigurations"), "*.json")
            .Select(Path.GetFileName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(3, files.Length);
        Assert.Contains("Same Name.json", files);
        Assert.Contains("Same Name_cfg-1.json", files);
        Assert.Contains("Same Name_1.json", files);
    }

    [Fact]
    public async Task ExportDeviceConfigurations_LargeBatch_WritesEveryFileAndMigrationEntry()
    {
        var configs = Enumerable.Range(1, 25)
            .Select(index => new DeviceConfiguration
            {
                Id = $"cfg-{index}",
                DisplayName = $"Config {index:D2}"
            })
            .ToArray();

        await _sut.ExportDeviceConfigurationsAsync(configs, _tempDir);

        var folder = Path.Combine(_tempDir, "DeviceConfigurations");
        Assert.Equal(25, Directory.GetFiles(folder, "*.json").Length);

        var migrationJson = await File.ReadAllTextAsync(Path.Combine(_tempDir, "migration-table.json"));
        Assert.Contains("cfg-25", migrationJson);
    }

    [Fact]
    public async Task ExportDeviceConfiguration_PreexistingFile_UsesNextAvailableName()
    {
        var folder = Path.Combine(_tempDir, "DeviceConfigurations");
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "Same Name.json"), "{ \"displayName\": \"existing\" }");

        var table = new MigrationTable();
        var config = new DeviceConfiguration
        {
            Id = "cfg-2",
            DisplayName = "Same Name"
        };

        await _sut.ExportDeviceConfigurationAsync(config, _tempDir, table);

        var files = Directory.GetFiles(folder, "*.json")
            .Select(Path.GetFileName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(2, files.Length);
        Assert.Contains("Same Name.json", files);
        Assert.Contains("Same Name_cfg-2.json", files);
    }

    [Fact]
    public async Task SaveMigrationTableAsync_ResetsReservedFileNamesAfterDeletedFile()
    {
        var folder = Path.Combine(_tempDir, "DeviceConfigurations");
        var firstTable = new MigrationTable();
        var secondTable = new MigrationTable();

        await _sut.ExportDeviceConfigurationAsync(new DeviceConfiguration
        {
            Id = "cfg-1",
            DisplayName = "Same Name"
        }, _tempDir, firstTable);
        await _sut.SaveMigrationTableAsync(firstTable, _tempDir);

        File.Delete(Path.Combine(folder, "Same Name.json"));

        await _sut.ExportDeviceConfigurationAsync(new DeviceConfiguration
        {
            Id = "cfg-2",
            DisplayName = "Same Name"
        }, _tempDir, secondTable);

        var files = Directory.GetFiles(folder, "*.json")
            .Select(Path.GetFileName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Single(files);
        Assert.Contains("Same Name.json", files);
    }

    [Fact]
    public async Task SaveMigrationTableAsync_RefreshesFolderStateForExternalFiles()
    {
        var folder = Path.Combine(_tempDir, "DeviceConfigurations");
        var firstTable = new MigrationTable();
        var secondTable = new MigrationTable();

        await _sut.ExportDeviceConfigurationAsync(new DeviceConfiguration
        {
            Id = "cfg-1",
            DisplayName = "Alpha"
        }, _tempDir, firstTable);
        await _sut.SaveMigrationTableAsync(firstTable, _tempDir);

        var externalPath = Path.Combine(folder, "Bravo.json");
        await File.WriteAllTextAsync(externalPath, "{ \"displayName\": \"external\" }");

        await _sut.ExportDeviceConfigurationAsync(new DeviceConfiguration
        {
            Id = "cfg-2",
            DisplayName = "Bravo"
        }, _tempDir, secondTable);

        var files = Directory.GetFiles(folder, "*.json")
            .Select(Path.GetFileName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Contains("Bravo.json", files);
        Assert.Contains("Bravo_cfg-2.json", files);
        Assert.Contains("external", await File.ReadAllTextAsync(externalPath));
    }

    [Fact]
    public async Task SaveMigrationTableAsync_OverwritesExistingFile()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "migration-table.json"), "{ \"entries\": [] }");

        var table = new MigrationTable();
        table.AddOrUpdate(new MigrationEntry
        {
            ObjectType = "DeviceConfiguration",
            OriginalId = "cfg-1",
            NewId = "created-1",
            Name = "Config 1"
        });

        await _sut.SaveMigrationTableAsync(table, _tempDir);

        var json = await File.ReadAllTextAsync(Path.Combine(_tempDir, "migration-table.json"));
        Assert.Contains("created-1", json);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}

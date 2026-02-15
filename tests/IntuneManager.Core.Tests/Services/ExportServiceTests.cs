using IntuneManager.Core.Models;
using IntuneManager.Core.Services;
using Microsoft.Graph.Models;

namespace IntuneManager.Core.Tests.Services;

public class ExportServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ExportService _service;

    public ExportServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"intunemanager-export-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _service = new ExportService();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public async Task ExportDeviceConfiguration_CreatesJsonFile()
    {
        var config = new DeviceConfiguration
        {
            Id = "test-id",
            DisplayName = "Test Config"
        };
        var table = new MigrationTable();

        await _service.ExportDeviceConfigurationAsync(config, _tempDir, table);

        var expectedPath = Path.Combine(_tempDir, "DeviceConfigurations", "Test Config.json");
        Assert.True(File.Exists(expectedPath));
    }

    [Fact]
    public async Task ExportDeviceConfiguration_UpdatesMigrationTable()
    {
        var config = new DeviceConfiguration
        {
            Id = "test-id",
            DisplayName = "Test Config"
        };
        var table = new MigrationTable();

        await _service.ExportDeviceConfigurationAsync(config, _tempDir, table);

        Assert.Single(table.Entries);
        Assert.Equal("test-id", table.Entries[0].OriginalId);
        Assert.Equal("DeviceConfiguration", table.Entries[0].ObjectType);
    }

    [Fact]
    public async Task ExportDeviceConfigurations_ExportsMultipleFiles()
    {
        var configs = new[]
        {
            new DeviceConfiguration { Id = "id-1", DisplayName = "Config One" },
            new DeviceConfiguration { Id = "id-2", DisplayName = "Config Two" }
        };

        await _service.ExportDeviceConfigurationsAsync(configs, _tempDir);

        var folder = Path.Combine(_tempDir, "DeviceConfigurations");
        Assert.Equal(2, Directory.GetFiles(folder, "*.json").Length);
    }

    [Fact]
    public async Task ExportDeviceConfigurations_CreatesMigrationTableFile()
    {
        var configs = new[]
        {
            new DeviceConfiguration { Id = "id-1", DisplayName = "Config One" }
        };

        await _service.ExportDeviceConfigurationsAsync(configs, _tempDir);

        var tablePath = Path.Combine(_tempDir, "migration-table.json");
        Assert.True(File.Exists(tablePath));
    }

    [Fact]
    public async Task SaveMigrationTable_WritesValidJson()
    {
        var table = new MigrationTable();
        table.AddOrUpdate(new MigrationEntry
        {
            ObjectType = "DeviceConfiguration",
            OriginalId = "id-1",
            Name = "Policy"
        });

        await _service.SaveMigrationTableAsync(table, _tempDir);

        var json = await File.ReadAllTextAsync(Path.Combine(_tempDir, "migration-table.json"));
        Assert.Contains("DeviceConfiguration", json);
        Assert.Contains("id-1", json);
    }

    [Fact]
    public async Task ExportDeviceConfiguration_SanitizesFileName()
    {
        var config = new DeviceConfiguration
        {
            Id = "test-id",
            DisplayName = "Test/Config:With<Invalid>Chars"
        };
        var table = new MigrationTable();

        await _service.ExportDeviceConfigurationAsync(config, _tempDir, table);

        var folder = Path.Combine(_tempDir, "DeviceConfigurations");
        var files = Directory.GetFiles(folder, "*.json");
        Assert.Single(files);
    }
}

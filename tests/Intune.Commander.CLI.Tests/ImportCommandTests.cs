using System.CommandLine;
using System.Text.Json;
using Intune.Commander.CLI.Commands;
using Intune.Commander.Core.Models;
using Intune.Commander.Core.Services;
using Microsoft.Graph.Beta.Models;
using Microsoft.Kiota.Abstractions;

namespace Intune.Commander.CLI.Tests;

[Collection("Console")]
public sealed class ImportCommandTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"ic-import-cli-{Guid.NewGuid():N}");

    public ImportCommandTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public async Task Build_DryRunWithoutAuthOptions_SucceedsAndIncludesSummary()
    {
        var exportDir = Path.Combine(_tempDir, "export");
        Directory.CreateDirectory(Path.Combine(exportDir, "DeviceConfigurations"));
        Directory.CreateDirectory(Path.Combine(exportDir, "CompliancePolicies"));

        await File.WriteAllTextAsync(
            Path.Combine(exportDir, "DeviceConfigurations", "Config One.json"),
            JsonSerializer.Serialize(new DeviceConfiguration
            {
                Id = "cfg-1",
                DisplayName = "Config One"
            }));

        await File.WriteAllTextAsync(
            Path.Combine(exportDir, "CompliancePolicies", "Policy One.json"),
            JsonSerializer.Serialize(new CompliancePolicyExport
            {
                Policy = new DeviceCompliancePolicy
                {
                    Id = "cp-1",
                    DisplayName = "Policy One"
                },
                Assignments = []
            }));

        var result = await InvokeAsync("import", "--folder", exportDir, "--dry-run");

        Assert.Equal(0, result.ExitCode);
        Assert.True(string.IsNullOrWhiteSpace(result.StdErr));

        using var document = JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;

        Assert.True(root.GetProperty("result").GetProperty("dryRun").GetBoolean());
        Assert.Equal(2, root.GetProperty("result").GetProperty("count").GetInt32());
        Assert.Equal(2, root.GetProperty("summary").GetProperty("total").GetInt32());
        Assert.Equal(0, root.GetProperty("summary").GetProperty("validationErrorCount").GetInt32());
        Assert.Equal(1, root.GetProperty("summary").GetProperty("perTypeCounts").GetProperty("deviceConfigurations").GetInt32());
        Assert.Equal(1, root.GetProperty("summary").GetProperty("perTypeCounts").GetProperty("compliancePolicies").GetInt32());
        Assert.Equal(0, root.GetProperty("summary").GetProperty("perTypeCounts").GetProperty("settingsCatalogPolicies").GetInt32());
        Assert.Equal(0, root.GetProperty("migrationTable").GetProperty("entries").GetArrayLength());
        Assert.Equal(0, root.GetProperty("validationErrors").GetArrayLength());
    }

    [Fact]
    public async Task Build_DryRunMissingFolder_FailsCleanly()
    {
        var missingFolder = Path.Combine(_tempDir, "does-not-exist");

        var result = await InvokeAsync("import", "--folder", missingFolder, "--dry-run");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("was not found", result.StdErr);
        Assert.True(string.IsNullOrWhiteSpace(result.StdOut));
    }

    [Fact]
    public async Task Build_DryRunMalformedJson_ReportsValidationErrorsAndContinues()
    {
        var exportDir = Path.Combine(_tempDir, "malformed-export");
        Directory.CreateDirectory(Path.Combine(exportDir, "DeviceConfigurations"));
        Directory.CreateDirectory(Path.Combine(exportDir, "CompliancePolicies"));

        await File.WriteAllTextAsync(
            Path.Combine(exportDir, "DeviceConfigurations", "Config One.json"),
            JsonSerializer.Serialize(new DeviceConfiguration
            {
                Id = "cfg-1",
                DisplayName = "Config One"
            }));

        await File.WriteAllTextAsync(
            Path.Combine(exportDir, "DeviceConfigurations", "Broken.json"),
            "{");

        await File.WriteAllTextAsync(
            Path.Combine(exportDir, "CompliancePolicies", "Policy One.json"),
            JsonSerializer.Serialize(new CompliancePolicyExport
            {
                Policy = new DeviceCompliancePolicy
                {
                    Id = "cp-1",
                    DisplayName = "Policy One"
                },
                Assignments = []
            }));

        var result = await InvokeAsync("import", "--folder", exportDir, "--dry-run");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Dry-run validation found 1 invalid file", result.StdErr);

        using var document = JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;

        Assert.Equal(2, root.GetProperty("result").GetProperty("count").GetInt32());
        Assert.Equal(2, root.GetProperty("summary").GetProperty("total").GetInt32());
        Assert.Equal(1, root.GetProperty("summary").GetProperty("validationErrorCount").GetInt32());
        Assert.Equal(1, root.GetProperty("summary").GetProperty("perTypeCounts").GetProperty("deviceConfigurations").GetInt32());
        Assert.Equal(1, root.GetProperty("summary").GetProperty("perTypeCounts").GetProperty("compliancePolicies").GetInt32());

        var validationError = Assert.Single(root.GetProperty("validationErrors").EnumerateArray());
        Assert.Equal("deviceConfigurations", validationError.GetProperty("summaryKey").GetString());
        Assert.EndsWith("Broken.json", validationError.GetProperty("relativePath").GetString(), StringComparison.Ordinal);
        Assert.Equal("JsonException", validationError.GetProperty("errorType").GetString());
    }

    [Fact]
    public async Task Build_DryRunNullPayload_ReportsValidationErrorsAndContinues()
    {
        var exportDir = Path.Combine(_tempDir, "null-export");
        Directory.CreateDirectory(Path.Combine(exportDir, "DeviceConfigurations"));
        Directory.CreateDirectory(Path.Combine(exportDir, "CompliancePolicies"));

        await File.WriteAllTextAsync(
            Path.Combine(exportDir, "DeviceConfigurations", "NullConfig.json"),
            "null");

        await File.WriteAllTextAsync(
            Path.Combine(exportDir, "CompliancePolicies", "Policy One.json"),
            JsonSerializer.Serialize(new CompliancePolicyExport
            {
                Policy = new DeviceCompliancePolicy
                {
                    Id = "cp-1",
                    DisplayName = "Policy One"
                },
                Assignments = []
            }));

        var result = await InvokeAsync("import", "--folder", exportDir, "--dry-run");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Dry-run validation found 1 invalid file", result.StdErr);

        using var document = JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;

        Assert.Equal(1, root.GetProperty("result").GetProperty("count").GetInt32());
        Assert.Equal(1, root.GetProperty("summary").GetProperty("total").GetInt32());
        Assert.Equal(1, root.GetProperty("summary").GetProperty("validationErrorCount").GetInt32());
        Assert.Equal(0, root.GetProperty("summary").GetProperty("perTypeCounts").GetProperty("deviceConfigurations").GetInt32());
        Assert.Equal(1, root.GetProperty("summary").GetProperty("perTypeCounts").GetProperty("compliancePolicies").GetInt32());

        var validationError = Assert.Single(root.GetProperty("validationErrors").EnumerateArray());
        Assert.Equal("deviceConfigurations", validationError.GetProperty("summaryKey").GetString());
        Assert.EndsWith("NullConfig.json", validationError.GetProperty("relativePath").GetString(), StringComparison.Ordinal);
        Assert.Equal("NullPayload", validationError.GetProperty("errorType").GetString());
        Assert.Equal("The file deserialized to null.", validationError.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Build_DryRunMalformedMigrationTable_ReportsValidationErrorsAndContinues()
    {
        var exportDir = Path.Combine(_tempDir, "bad-migration-table-export");
        Directory.CreateDirectory(Path.Combine(exportDir, "DeviceConfigurations"));

        await File.WriteAllTextAsync(
            Path.Combine(exportDir, "migration-table.json"),
            "{");

        await File.WriteAllTextAsync(
            Path.Combine(exportDir, "DeviceConfigurations", "Config One.json"),
            JsonSerializer.Serialize(new DeviceConfiguration
            {
                Id = "cfg-1",
                DisplayName = "Config One"
            }));

        var result = await InvokeAsync("import", "--folder", exportDir, "--dry-run");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Dry-run validation found 1 invalid file", result.StdErr);

        using var document = JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;

        Assert.Equal(1, root.GetProperty("result").GetProperty("count").GetInt32());
        Assert.Equal(1, root.GetProperty("summary").GetProperty("total").GetInt32());
        Assert.Equal(1, root.GetProperty("summary").GetProperty("validationErrorCount").GetInt32());
        Assert.Equal(1, root.GetProperty("summary").GetProperty("perTypeCounts").GetProperty("deviceConfigurations").GetInt32());
        Assert.Equal(0, root.GetProperty("migrationTable").GetProperty("entries").GetArrayLength());

        var validationError = Assert.Single(root.GetProperty("validationErrors").EnumerateArray());
        Assert.Equal("migrationTable", validationError.GetProperty("summaryKey").GetString());
        Assert.Equal("migration-table.json", validationError.GetProperty("relativePath").GetString());
        Assert.Equal("JsonException", validationError.GetProperty("errorType").GetString());
    }

    [Fact]
    public async Task ExecuteImportAsync_Success_WritesJsonAndMigrationTable()
    {
        var exportDir = Path.Combine(_tempDir, "live-import-success");
        Directory.CreateDirectory(Path.Combine(exportDir, "DeviceConfigurations"));

        await File.WriteAllTextAsync(
            Path.Combine(exportDir, "DeviceConfigurations", "Config One.json"),
            JsonSerializer.Serialize(new DeviceConfiguration
            {
                Id = "cfg-1",
                DisplayName = "Config One"
            }));

        var importService = new ImportService(new FakeConfigurationProfileService(static config =>
        {
            config.Id = "created-1";
            return Task.FromResult(config);
        }));

        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await ImportCommand.ExecuteImportAsync(
            importService,
            new ExportService(),
            exportDir,
            CancellationToken.None,
            stdout,
            stderr);

        Assert.Equal(0, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(stderr.ToString()));

        using var document = JsonDocument.Parse(stdout.ToString());
        var root = document.RootElement;
        Assert.Equal(1, root.GetProperty("result").GetProperty("count").GetInt32());
        Assert.Equal(1, root.GetProperty("summary").GetProperty("perTypeCounts").GetProperty("deviceConfigurations").GetInt32());
        Assert.Equal(1, root.GetProperty("migrationTable").GetProperty("entries").GetArrayLength());

        var migrationTableJson = await File.ReadAllTextAsync(Path.Combine(exportDir, "migration-table.json"));
        Assert.Contains("created-1", migrationTableJson);
    }

    [Fact]
    public async Task ExecuteImportAsync_ImporterFailure_ReturnsNonZeroAndWritesError()
    {
        var exportDir = Path.Combine(_tempDir, "live-import-failure");
        Directory.CreateDirectory(Path.Combine(exportDir, "DeviceConfigurations"));

        await File.WriteAllTextAsync(
            Path.Combine(exportDir, "DeviceConfigurations", "Config One.json"),
            JsonSerializer.Serialize(new DeviceConfiguration
            {
                Id = "cfg-1",
                DisplayName = "Config One"
            }));

        var importService = new ImportService(new FakeConfigurationProfileService(static _ =>
            throw new ApiException("simulated device create failure.")));

        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await ImportCommand.ExecuteImportAsync(
            importService,
            new ExportService(),
            exportDir,
            CancellationToken.None,
            stdout,
            stderr);

        Assert.Equal(1, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(stdout.ToString()));
        Assert.Contains("Import failed: simulated device create failure.", stderr.ToString());
        Assert.False(File.Exists(Path.Combine(exportDir, "migration-table.json")));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private static async Task<CommandInvocationResult> InvokeAsync(params string[] args)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var originalOut = Console.Out;
        var originalErr = Console.Error;

        try
        {
            Console.SetOut(stdout);
            Console.SetError(stderr);

            var root = new RootCommand("test");
            root.AddCommand(ImportCommand.Build());

            var exitCode = await root.InvokeAsync(args);
            return new CommandInvocationResult(exitCode, stdout.ToString(), stderr.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalErr);
        }
    }

    private sealed record CommandInvocationResult(int ExitCode, string StdOut, string StdErr);

    private sealed class FakeConfigurationProfileService(Func<DeviceConfiguration, Task<DeviceConfiguration>> createAsync) : IConfigurationProfileService
    {
        public Task<List<DeviceConfiguration>> ListDeviceConfigurationsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<DeviceConfiguration>());

        public Task<DeviceConfiguration?> GetDeviceConfigurationAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult<DeviceConfiguration?>(null);

        public Task<DeviceConfiguration> CreateDeviceConfigurationAsync(DeviceConfiguration config, CancellationToken cancellationToken = default) =>
            createAsync(config);

        public Task<DeviceConfiguration> UpdateDeviceConfigurationAsync(DeviceConfiguration config, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteDeviceConfigurationAsync(string id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<List<DeviceConfigurationAssignment>> GetAssignmentsAsync(string configId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<DeviceConfigurationAssignment>());
    }
}

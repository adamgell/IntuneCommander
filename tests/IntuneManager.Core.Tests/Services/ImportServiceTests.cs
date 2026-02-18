using System.Text.Json;
using IntuneManager.Core.Models;
using IntuneManager.Core.Services;
using Microsoft.Graph.Beta.Models;

namespace IntuneManager.Core.Tests.Services;

public class ImportServiceTests : IDisposable
{
    private readonly string _tempDir;

    public ImportServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"intunemanager-import-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public async Task ReadDeviceConfigurationAsync_ReadsValidJson()
    {
        var config = new DeviceConfiguration { Id = "cfg-1", DisplayName = "Config One" };
        var file = Path.Combine(_tempDir, "cfg.json");
        await File.WriteAllTextAsync(file, JsonSerializer.Serialize(config));

        var sut = new ImportService(new StubConfigurationService());
        var read = await sut.ReadDeviceConfigurationAsync(file);

        Assert.NotNull(read);
        Assert.Equal("cfg-1", read.Id);
        Assert.Equal("Config One", read.DisplayName);
    }

    [Fact]
    public async Task ReadDeviceConfigurationsFromFolderAsync_MissingFolder_ReturnsEmpty()
    {
        var sut = new ImportService(new StubConfigurationService());

        var result = await sut.ReadDeviceConfigurationsFromFolderAsync(_tempDir);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ReadDeviceConfigurationsFromFolderAsync_ReadsAllJsonFiles()
    {
        var folder = Path.Combine(_tempDir, "DeviceConfigurations");
        Directory.CreateDirectory(folder);

        await File.WriteAllTextAsync(Path.Combine(folder, "a.json"), JsonSerializer.Serialize(
            new DeviceConfiguration { Id = "a", DisplayName = "A" }));
        await File.WriteAllTextAsync(Path.Combine(folder, "b.json"), JsonSerializer.Serialize(
            new DeviceConfiguration { Id = "b", DisplayName = "B" }));

        var sut = new ImportService(new StubConfigurationService());
        var result = await sut.ReadDeviceConfigurationsFromFolderAsync(_tempDir);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, c => c.Id == "a");
        Assert.Contains(result, c => c.Id == "b");
    }

    [Fact]
    public async Task ReadMigrationTableAsync_MissingFile_ReturnsEmpty()
    {
        var sut = new ImportService(new StubConfigurationService());

        var table = await sut.ReadMigrationTableAsync(_tempDir);

        Assert.Empty(table.Entries);
    }

    [Fact]
    public async Task ReadMigrationTableAsync_ReadsExistingFile()
    {
        var table = new MigrationTable();
        table.AddOrUpdate(new MigrationEntry
        {
            ObjectType = "DeviceConfiguration",
            OriginalId = "old-1",
            NewId = "new-1",
            Name = "One"
        });

        var path = Path.Combine(_tempDir, "migration-table.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(table));

        var sut = new ImportService(new StubConfigurationService());
        var read = await sut.ReadMigrationTableAsync(_tempDir);

        Assert.Single(read.Entries);
        Assert.Equal("old-1", read.Entries[0].OriginalId);
    }

    [Fact]
    public async Task ImportDeviceConfigurationAsync_ClearsReadOnlyFields_AndUpdatesMigration()
    {
        var cfgService = new StubConfigurationService
        {
            CreateResult = new DeviceConfiguration { Id = "new-cfg", DisplayName = "Created" }
        };
        var sut = new ImportService(cfgService);
        var table = new MigrationTable();

        var source = new DeviceConfiguration
        {
            Id = "old-cfg",
            DisplayName = "Source",
            CreatedDateTime = DateTimeOffset.UtcNow,
            LastModifiedDateTime = DateTimeOffset.UtcNow,
            Version = 99
        };

        var created = await sut.ImportDeviceConfigurationAsync(source, table);

        Assert.Equal("new-cfg", created.Id);
        Assert.NotNull(cfgService.LastCreatedConfig);
        Assert.Null(cfgService.LastCreatedConfig!.Id);
        Assert.Null(cfgService.LastCreatedConfig.CreatedDateTime);
        Assert.Null(cfgService.LastCreatedConfig.LastModifiedDateTime);
        Assert.Null(cfgService.LastCreatedConfig.Version);
        Assert.Single(table.Entries);
        Assert.Equal("old-cfg", table.Entries[0].OriginalId);
        Assert.Equal("new-cfg", table.Entries[0].NewId);
    }

    [Fact]
    public async Task ReadCompliancePolicyAsync_ReadsValidJson()
    {
        var export = new CompliancePolicyExport
        {
            Policy = new DeviceCompliancePolicy { Id = "p1", DisplayName = "Policy One" },
            Assignments = []
        };

        var file = Path.Combine(_tempDir, "policy.json");
        await File.WriteAllTextAsync(file, JsonSerializer.Serialize(export));

        var sut = new ImportService(new StubConfigurationService(), new StubComplianceService());
        var read = await sut.ReadCompliancePolicyAsync(file);

        Assert.NotNull(read);
        Assert.Equal("p1", read.Policy.Id);
    }

    [Fact]
    public async Task ReadCompliancePoliciesFromFolderAsync_MissingFolder_ReturnsEmpty()
    {
        var sut = new ImportService(new StubConfigurationService(), new StubComplianceService());

        var result = await sut.ReadCompliancePoliciesFromFolderAsync(_tempDir);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ReadCompliancePoliciesFromFolderAsync_ReadsAllJsonFiles()
    {
        var folder = Path.Combine(_tempDir, "CompliancePolicies");
        Directory.CreateDirectory(folder);

        var p1 = new CompliancePolicyExport { Policy = new DeviceCompliancePolicy { Id = "p1", DisplayName = "P1" } };
        var p2 = new CompliancePolicyExport { Policy = new DeviceCompliancePolicy { Id = "p2", DisplayName = "P2" } };

        await File.WriteAllTextAsync(Path.Combine(folder, "p1.json"), JsonSerializer.Serialize(p1));
        await File.WriteAllTextAsync(Path.Combine(folder, "p2.json"), JsonSerializer.Serialize(p2));

        var sut = new ImportService(new StubConfigurationService(), new StubComplianceService());
        var result = await sut.ReadCompliancePoliciesFromFolderAsync(_tempDir);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ImportCompliancePolicyAsync_WithoutService_Throws()
    {
        var sut = new ImportService(new StubConfigurationService(), null);
        var table = new MigrationTable();
        var export = new CompliancePolicyExport
        {
            Policy = new DeviceCompliancePolicy { Id = "old", DisplayName = "Old" }
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ImportCompliancePolicyAsync(export, table));
    }

    [Fact]
    public async Task ImportCompliancePolicyAsync_AssignsAndUpdatesMigration()
    {
        var complianceService = new StubComplianceService
        {
            CreateResult = new DeviceCompliancePolicy { Id = "new-pol", DisplayName = "Created Policy" }
        };

        var sut = new ImportService(new StubConfigurationService(), complianceService);
        var table = new MigrationTable();

        var export = new CompliancePolicyExport
        {
            Policy = new DeviceCompliancePolicy
            {
                Id = "old-pol",
                DisplayName = "Source Policy",
                CreatedDateTime = DateTimeOffset.UtcNow,
                LastModifiedDateTime = DateTimeOffset.UtcNow,
                Version = 3
            },
            Assignments =
            [
                new DeviceCompliancePolicyAssignment { Id = "assign-1" },
                new DeviceCompliancePolicyAssignment { Id = "assign-2" }
            ]
        };

        var created = await sut.ImportCompliancePolicyAsync(export, table);

        Assert.Equal("new-pol", created.Id);
        Assert.NotNull(complianceService.LastCreatedPolicy);
        Assert.Null(complianceService.LastCreatedPolicy!.Id);
        Assert.Null(complianceService.LastCreatedPolicy.CreatedDateTime);
        Assert.Null(complianceService.LastCreatedPolicy.LastModifiedDateTime);
        Assert.Null(complianceService.LastCreatedPolicy.Version);

        Assert.True(complianceService.AssignCalled);
        Assert.Equal("new-pol", complianceService.AssignedPolicyId);
        Assert.All(complianceService.AssignedAssignments!, a => Assert.Null(a.Id));

        Assert.Single(table.Entries);
        Assert.Equal("old-pol", table.Entries[0].OriginalId);
        Assert.Equal("new-pol", table.Entries[0].NewId);
    }

    private sealed class StubConfigurationService : IConfigurationProfileService
    {
        public DeviceConfiguration? LastCreatedConfig { get; private set; }
        public DeviceConfiguration CreateResult { get; set; } = new() { Id = "created", DisplayName = "Created" };

        public Task<List<DeviceConfiguration>> ListDeviceConfigurationsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new List<DeviceConfiguration>());

        public Task<DeviceConfiguration?> GetDeviceConfigurationAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult<DeviceConfiguration?>(null);

        public Task<DeviceConfiguration> CreateDeviceConfigurationAsync(DeviceConfiguration config, CancellationToken cancellationToken = default)
        {
            LastCreatedConfig = config;
            return Task.FromResult(CreateResult);
        }

        public Task<DeviceConfiguration> UpdateDeviceConfigurationAsync(DeviceConfiguration config, CancellationToken cancellationToken = default)
            => Task.FromResult(config);

        public Task DeleteDeviceConfigurationAsync(string id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<List<DeviceConfigurationAssignment>> GetAssignmentsAsync(string configId, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<DeviceConfigurationAssignment>());
    }

    private sealed class StubComplianceService : ICompliancePolicyService
    {
        public DeviceCompliancePolicy? LastCreatedPolicy { get; private set; }
        public DeviceCompliancePolicy CreateResult { get; set; } = new() { Id = "created-policy", DisplayName = "Created" };

        public bool AssignCalled { get; private set; }
        public string? AssignedPolicyId { get; private set; }
        public List<DeviceCompliancePolicyAssignment>? AssignedAssignments { get; private set; }

        public Task<List<DeviceCompliancePolicy>> ListCompliancePoliciesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new List<DeviceCompliancePolicy>());

        public Task<DeviceCompliancePolicy?> GetCompliancePolicyAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult<DeviceCompliancePolicy?>(null);

        public Task<DeviceCompliancePolicy> CreateCompliancePolicyAsync(DeviceCompliancePolicy policy, CancellationToken cancellationToken = default)
        {
            LastCreatedPolicy = policy;
            return Task.FromResult(CreateResult);
        }

        public Task<DeviceCompliancePolicy> UpdateCompliancePolicyAsync(DeviceCompliancePolicy policy, CancellationToken cancellationToken = default)
            => Task.FromResult(policy);

        public Task DeleteCompliancePolicyAsync(string id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<List<DeviceCompliancePolicyAssignment>> GetAssignmentsAsync(string policyId, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<DeviceCompliancePolicyAssignment>());

        public Task AssignPolicyAsync(string policyId, List<DeviceCompliancePolicyAssignment> assignments, CancellationToken cancellationToken = default)
        {
            AssignCalled = true;
            AssignedPolicyId = policyId;
            AssignedAssignments = assignments;
            return Task.CompletedTask;
        }
    }
}

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

    [Fact]
    public async Task ImportEndpointSecurityIntentAsync_AssignsAndUpdatesMigration()
    {
        var endpointService = new StubEndpointSecurityService
        {
            CreateResult = new DeviceManagementIntent { Id = "new-intent", DisplayName = "Created Intent" }
        };
        var sut = new ImportService(new StubConfigurationService(), null, endpointSecurityService: endpointService);
        var table = new MigrationTable();

        var export = new EndpointSecurityExport
        {
            Intent = new DeviceManagementIntent { Id = "old-intent", DisplayName = "Source Intent" },
            Assignments = [new DeviceManagementIntentAssignment { Id = "ea-1" }]
        };

        var created = await sut.ImportEndpointSecurityIntentAsync(export, table);

        Assert.Equal("new-intent", created.Id);
        Assert.True(endpointService.AssignCalled);
        Assert.Equal("new-intent", endpointService.AssignedIntentId);
        Assert.All(endpointService.AssignedAssignments!, a => Assert.Null(a.Id));
        Assert.Single(table.Entries);
        Assert.Equal("EndpointSecurityIntent", table.Entries[0].ObjectType);
        Assert.Equal("old-intent", table.Entries[0].OriginalId);
        Assert.Equal("new-intent", table.Entries[0].NewId);
    }

    [Fact]
    public async Task ImportAdministrativeTemplateAsync_AssignsAndUpdatesMigration()
    {
        var templateService = new StubAdministrativeTemplateService
        {
            CreateResult = new GroupPolicyConfiguration { Id = "new-template", DisplayName = "Created Template" }
        };
        var sut = new ImportService(new StubConfigurationService(), null, administrativeTemplateService: templateService);
        var table = new MigrationTable();

        var export = new AdministrativeTemplateExport
        {
            Template = new GroupPolicyConfiguration
            {
                Id = "old-template",
                DisplayName = "Source Template",
                CreatedDateTime = DateTimeOffset.UtcNow,
                LastModifiedDateTime = DateTimeOffset.UtcNow
            },
            Assignments = [new GroupPolicyConfigurationAssignment { Id = "ta-1" }]
        };

        var created = await sut.ImportAdministrativeTemplateAsync(export, table);

        Assert.Equal("new-template", created.Id);
        Assert.NotNull(templateService.LastCreatedTemplate);
        Assert.Null(templateService.LastCreatedTemplate!.Id);
        Assert.Null(templateService.LastCreatedTemplate.CreatedDateTime);
        Assert.Null(templateService.LastCreatedTemplate.LastModifiedDateTime);
        Assert.True(templateService.AssignCalled);
        Assert.Equal("new-template", templateService.AssignedTemplateId);
        Assert.All(templateService.AssignedAssignments!, a => Assert.Null(a.Id));
        Assert.Single(table.Entries);
        Assert.Equal("AdministrativeTemplate", table.Entries[0].ObjectType);
        Assert.Equal("old-template", table.Entries[0].OriginalId);
        Assert.Equal("new-template", table.Entries[0].NewId);
    }

    [Fact]
    public async Task ImportEnrollmentConfigurationAsync_UpdatesMigration()
    {
        var enrollmentService = new StubEnrollmentConfigurationService
        {
            CreateResult = new DeviceEnrollmentConfiguration { Id = "new-enroll", DisplayName = "Created Enrollment" }
        };
        var sut = new ImportService(new StubConfigurationService(), null, enrollmentConfigurationService: enrollmentService);
        var table = new MigrationTable();

        var configuration = new DeviceEnrollmentConfiguration
        {
            Id = "old-enroll",
            DisplayName = "Source Enrollment",
            CreatedDateTime = DateTimeOffset.UtcNow,
            LastModifiedDateTime = DateTimeOffset.UtcNow,
            Priority = 10
        };

        var created = await sut.ImportEnrollmentConfigurationAsync(configuration, table);

        Assert.Equal("new-enroll", created.Id);
        Assert.NotNull(enrollmentService.LastCreatedConfiguration);
        Assert.Null(enrollmentService.LastCreatedConfiguration!.Id);
        Assert.Null(enrollmentService.LastCreatedConfiguration.CreatedDateTime);
        Assert.Null(enrollmentService.LastCreatedConfiguration.LastModifiedDateTime);
        Assert.Single(table.Entries);
        Assert.Equal("EnrollmentConfiguration", table.Entries[0].ObjectType);
        Assert.Equal("old-enroll", table.Entries[0].OriginalId);
        Assert.Equal("new-enroll", table.Entries[0].NewId);
    }

    [Fact]
    public async Task ImportAppProtectionPolicyAsync_UpdatesMigration()
    {
        var appProtectionService = new StubAppProtectionPolicyService
        {
            CreateResult = new AndroidManagedAppProtection { Id = "new-app-protect", DisplayName = "Created App Protection" }
        };
        var sut = new ImportService(new StubConfigurationService(), null, appProtectionPolicyService: appProtectionService);
        var table = new MigrationTable();

        var policy = new AndroidManagedAppProtection
        {
            Id = "old-app-protect",
            DisplayName = "Source App Protection"
        };

        var created = await sut.ImportAppProtectionPolicyAsync(policy, table);

        Assert.Equal("new-app-protect", created.Id);
        Assert.NotNull(appProtectionService.LastCreatedPolicy);
        Assert.Null(appProtectionService.LastCreatedPolicy!.Id);
        Assert.Single(table.Entries);
        Assert.Equal("AppProtectionPolicy", table.Entries[0].ObjectType);
        Assert.Equal("old-app-protect", table.Entries[0].OriginalId);
        Assert.Equal("new-app-protect", table.Entries[0].NewId);
    }

    [Fact]
    public async Task ImportManagedDeviceAppConfigurationAsync_UpdatesMigration()
    {
        var managedConfigService = new StubManagedAppConfigurationService
        {
            CreateManagedDeviceResult = new ManagedDeviceMobileAppConfiguration { Id = "new-mdac", DisplayName = "Created MDAC" }
        };
        var sut = new ImportService(new StubConfigurationService(), null, managedAppConfigurationService: managedConfigService);
        var table = new MigrationTable();

        var configuration = new ManagedDeviceMobileAppConfiguration
        {
            Id = "old-mdac",
            DisplayName = "Source MDAC",
            CreatedDateTime = DateTimeOffset.UtcNow,
            LastModifiedDateTime = DateTimeOffset.UtcNow,
            Version = 5
        };

        var created = await sut.ImportManagedDeviceAppConfigurationAsync(configuration, table);

        Assert.Equal("new-mdac", created.Id);
        Assert.NotNull(managedConfigService.LastCreatedManagedDeviceConfiguration);
        Assert.Null(managedConfigService.LastCreatedManagedDeviceConfiguration!.Id);
        Assert.Null(managedConfigService.LastCreatedManagedDeviceConfiguration.CreatedDateTime);
        Assert.Null(managedConfigService.LastCreatedManagedDeviceConfiguration.LastModifiedDateTime);
        Assert.Null(managedConfigService.LastCreatedManagedDeviceConfiguration.Version);
        Assert.Single(table.Entries);
        Assert.Equal("ManagedDeviceAppConfiguration", table.Entries[0].ObjectType);
        Assert.Equal("old-mdac", table.Entries[0].OriginalId);
        Assert.Equal("new-mdac", table.Entries[0].NewId);
    }

    [Fact]
    public async Task ImportTargetedManagedAppConfigurationAsync_UpdatesMigration()
    {
        var managedConfigService = new StubManagedAppConfigurationService
        {
            CreateTargetedResult = new TargetedManagedAppConfiguration { Id = "new-tmac", DisplayName = "Created TMAC" }
        };
        var sut = new ImportService(new StubConfigurationService(), null, managedAppConfigurationService: managedConfigService);
        var table = new MigrationTable();

        var configuration = new TargetedManagedAppConfiguration
        {
            Id = "old-tmac",
            DisplayName = "Source TMAC",
            CreatedDateTime = DateTimeOffset.UtcNow,
            LastModifiedDateTime = DateTimeOffset.UtcNow,
            Version = "7"
        };

        var created = await sut.ImportTargetedManagedAppConfigurationAsync(configuration, table);

        Assert.Equal("new-tmac", created.Id);
        Assert.NotNull(managedConfigService.LastCreatedTargetedConfiguration);
        Assert.Null(managedConfigService.LastCreatedTargetedConfiguration!.Id);
        Assert.Null(managedConfigService.LastCreatedTargetedConfiguration.CreatedDateTime);
        Assert.Null(managedConfigService.LastCreatedTargetedConfiguration.LastModifiedDateTime);
        Assert.Null(managedConfigService.LastCreatedTargetedConfiguration.Version);
        Assert.Single(table.Entries);
        Assert.Equal("TargetedManagedAppConfiguration", table.Entries[0].ObjectType);
        Assert.Equal("old-tmac", table.Entries[0].OriginalId);
        Assert.Equal("new-tmac", table.Entries[0].NewId);
    }

    [Fact]
    public async Task ImportTermsAndConditionsAsync_UpdatesMigration()
    {
        var termsService = new StubTermsAndConditionsService
        {
            CreateResult = new TermsAndConditions { Id = "new-terms", DisplayName = "Created Terms" }
        };
        var sut = new ImportService(new StubConfigurationService(), null, termsAndConditionsService: termsService);
        var table = new MigrationTable();

        var termsAndConditions = new TermsAndConditions
        {
            Id = "old-terms",
            DisplayName = "Source Terms",
            CreatedDateTime = DateTimeOffset.UtcNow,
            LastModifiedDateTime = DateTimeOffset.UtcNow,
            Version = 2
        };

        var created = await sut.ImportTermsAndConditionsAsync(termsAndConditions, table);

        Assert.Equal("new-terms", created.Id);
        Assert.NotNull(termsService.LastCreatedTerms);
        Assert.Null(termsService.LastCreatedTerms!.Id);
        Assert.Null(termsService.LastCreatedTerms.CreatedDateTime);
        Assert.Null(termsService.LastCreatedTerms.LastModifiedDateTime);
        Assert.Null(termsService.LastCreatedTerms.Version);
        Assert.Single(table.Entries);
        Assert.Equal("TermsAndConditions", table.Entries[0].ObjectType);
        Assert.Equal("old-terms", table.Entries[0].OriginalId);
        Assert.Equal("new-terms", table.Entries[0].NewId);
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

    private sealed class StubEndpointSecurityService : IEndpointSecurityService
    {
        public DeviceManagementIntent? LastCreatedIntent { get; private set; }
        public DeviceManagementIntent CreateResult { get; set; } = new() { Id = "created-intent", DisplayName = "Created" };

        public bool AssignCalled { get; private set; }
        public string? AssignedIntentId { get; private set; }
        public List<DeviceManagementIntentAssignment>? AssignedAssignments { get; private set; }

        public Task<List<DeviceManagementIntent>> ListEndpointSecurityIntentsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new List<DeviceManagementIntent>());

        public Task<DeviceManagementIntent?> GetEndpointSecurityIntentAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult<DeviceManagementIntent?>(null);

        public Task<DeviceManagementIntent> CreateEndpointSecurityIntentAsync(DeviceManagementIntent intent, CancellationToken cancellationToken = default)
        {
            LastCreatedIntent = intent;
            return Task.FromResult(CreateResult);
        }

        public Task<DeviceManagementIntent> UpdateEndpointSecurityIntentAsync(DeviceManagementIntent intent, CancellationToken cancellationToken = default)
            => Task.FromResult(intent);

        public Task DeleteEndpointSecurityIntentAsync(string id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<List<DeviceManagementIntentAssignment>> GetAssignmentsAsync(string intentId, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<DeviceManagementIntentAssignment>());

        public Task AssignIntentAsync(string intentId, List<DeviceManagementIntentAssignment> assignments, CancellationToken cancellationToken = default)
        {
            AssignCalled = true;
            AssignedIntentId = intentId;
            AssignedAssignments = assignments;
            return Task.CompletedTask;
        }
    }

    private sealed class StubAdministrativeTemplateService : IAdministrativeTemplateService
    {
        public GroupPolicyConfiguration? LastCreatedTemplate { get; private set; }
        public GroupPolicyConfiguration CreateResult { get; set; } = new() { Id = "created-template", DisplayName = "Created" };

        public bool AssignCalled { get; private set; }
        public string? AssignedTemplateId { get; private set; }
        public List<GroupPolicyConfigurationAssignment>? AssignedAssignments { get; private set; }

        public Task<List<GroupPolicyConfiguration>> ListAdministrativeTemplatesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new List<GroupPolicyConfiguration>());

        public Task<GroupPolicyConfiguration?> GetAdministrativeTemplateAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult<GroupPolicyConfiguration?>(null);

        public Task<GroupPolicyConfiguration> CreateAdministrativeTemplateAsync(GroupPolicyConfiguration template, CancellationToken cancellationToken = default)
        {
            LastCreatedTemplate = template;
            return Task.FromResult(CreateResult);
        }

        public Task<GroupPolicyConfiguration> UpdateAdministrativeTemplateAsync(GroupPolicyConfiguration template, CancellationToken cancellationToken = default)
            => Task.FromResult(template);

        public Task DeleteAdministrativeTemplateAsync(string id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<List<GroupPolicyConfigurationAssignment>> GetAssignmentsAsync(string templateId, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<GroupPolicyConfigurationAssignment>());

        public Task AssignAdministrativeTemplateAsync(string templateId, List<GroupPolicyConfigurationAssignment> assignments, CancellationToken cancellationToken = default)
        {
            AssignCalled = true;
            AssignedTemplateId = templateId;
            AssignedAssignments = assignments;
            return Task.CompletedTask;
        }
    }

    private sealed class StubEnrollmentConfigurationService : IEnrollmentConfigurationService
    {
        public DeviceEnrollmentConfiguration? LastCreatedConfiguration { get; private set; }
        public DeviceEnrollmentConfiguration CreateResult { get; set; } = new() { Id = "created-enroll", DisplayName = "Created" };

        public Task<List<DeviceEnrollmentConfiguration>> ListEnrollmentConfigurationsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new List<DeviceEnrollmentConfiguration>());

        public Task<List<DeviceEnrollmentConfiguration>> ListEnrollmentStatusPagesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new List<DeviceEnrollmentConfiguration>());

        public Task<List<DeviceEnrollmentConfiguration>> ListEnrollmentRestrictionsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new List<DeviceEnrollmentConfiguration>());

        public Task<List<DeviceEnrollmentConfiguration>> ListCoManagementSettingsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new List<DeviceEnrollmentConfiguration>());

        public Task<DeviceEnrollmentConfiguration?> GetEnrollmentConfigurationAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult<DeviceEnrollmentConfiguration?>(null);

        public Task<DeviceEnrollmentConfiguration> CreateEnrollmentConfigurationAsync(DeviceEnrollmentConfiguration configuration, CancellationToken cancellationToken = default)
        {
            LastCreatedConfiguration = configuration;
            return Task.FromResult(CreateResult);
        }

        public Task<DeviceEnrollmentConfiguration> UpdateEnrollmentConfigurationAsync(DeviceEnrollmentConfiguration configuration, CancellationToken cancellationToken = default)
            => Task.FromResult(configuration);

        public Task DeleteEnrollmentConfigurationAsync(string id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class StubAppProtectionPolicyService : IAppProtectionPolicyService
    {
        public ManagedAppPolicy? LastCreatedPolicy { get; private set; }
        public ManagedAppPolicy CreateResult { get; set; } = new AndroidManagedAppProtection { Id = "created-app-protect", DisplayName = "Created" };

        public Task<List<ManagedAppPolicy>> ListAppProtectionPoliciesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new List<ManagedAppPolicy>());

        public Task<ManagedAppPolicy?> GetAppProtectionPolicyAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult<ManagedAppPolicy?>(null);

        public Task<ManagedAppPolicy> CreateAppProtectionPolicyAsync(ManagedAppPolicy policy, CancellationToken cancellationToken = default)
        {
            LastCreatedPolicy = policy;
            return Task.FromResult(CreateResult);
        }

        public Task<ManagedAppPolicy> UpdateAppProtectionPolicyAsync(ManagedAppPolicy policy, CancellationToken cancellationToken = default)
            => Task.FromResult(policy);

        public Task DeleteAppProtectionPolicyAsync(string id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class StubManagedAppConfigurationService : IManagedAppConfigurationService
    {
        public ManagedDeviceMobileAppConfiguration? LastCreatedManagedDeviceConfiguration { get; private set; }
        public TargetedManagedAppConfiguration? LastCreatedTargetedConfiguration { get; private set; }
        public ManagedDeviceMobileAppConfiguration CreateManagedDeviceResult { get; set; } = new() { Id = "created-mdac", DisplayName = "Created" };
        public TargetedManagedAppConfiguration CreateTargetedResult { get; set; } = new() { Id = "created-tmac", DisplayName = "Created" };

        public Task<List<ManagedDeviceMobileAppConfiguration>> ListManagedDeviceAppConfigurationsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new List<ManagedDeviceMobileAppConfiguration>());

        public Task<ManagedDeviceMobileAppConfiguration?> GetManagedDeviceAppConfigurationAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult<ManagedDeviceMobileAppConfiguration?>(null);

        public Task<ManagedDeviceMobileAppConfiguration> CreateManagedDeviceAppConfigurationAsync(ManagedDeviceMobileAppConfiguration configuration, CancellationToken cancellationToken = default)
        {
            LastCreatedManagedDeviceConfiguration = configuration;
            return Task.FromResult(CreateManagedDeviceResult);
        }

        public Task<ManagedDeviceMobileAppConfiguration> UpdateManagedDeviceAppConfigurationAsync(ManagedDeviceMobileAppConfiguration configuration, CancellationToken cancellationToken = default)
            => Task.FromResult(configuration);

        public Task DeleteManagedDeviceAppConfigurationAsync(string id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<List<TargetedManagedAppConfiguration>> ListTargetedManagedAppConfigurationsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new List<TargetedManagedAppConfiguration>());

        public Task<TargetedManagedAppConfiguration?> GetTargetedManagedAppConfigurationAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult<TargetedManagedAppConfiguration?>(null);

        public Task<TargetedManagedAppConfiguration> CreateTargetedManagedAppConfigurationAsync(TargetedManagedAppConfiguration configuration, CancellationToken cancellationToken = default)
        {
            LastCreatedTargetedConfiguration = configuration;
            return Task.FromResult(CreateTargetedResult);
        }

        public Task<TargetedManagedAppConfiguration> UpdateTargetedManagedAppConfigurationAsync(TargetedManagedAppConfiguration configuration, CancellationToken cancellationToken = default)
            => Task.FromResult(configuration);

        public Task DeleteTargetedManagedAppConfigurationAsync(string id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class StubTermsAndConditionsService : ITermsAndConditionsService
    {
        public TermsAndConditions? LastCreatedTerms { get; private set; }
        public TermsAndConditions CreateResult { get; set; } = new() { Id = "created-terms", DisplayName = "Created" };

        public Task<List<TermsAndConditions>> ListTermsAndConditionsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new List<TermsAndConditions>());

        public Task<TermsAndConditions?> GetTermsAndConditionsAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult<TermsAndConditions?>(null);

        public Task<TermsAndConditions> CreateTermsAndConditionsAsync(TermsAndConditions termsAndConditions, CancellationToken cancellationToken = default)
        {
            LastCreatedTerms = termsAndConditions;
            return Task.FromResult(CreateResult);
        }

        public Task<TermsAndConditions> UpdateTermsAndConditionsAsync(TermsAndConditions termsAndConditions, CancellationToken cancellationToken = default)
            => Task.FromResult(termsAndConditions);

        public Task DeleteTermsAndConditionsAsync(string id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}

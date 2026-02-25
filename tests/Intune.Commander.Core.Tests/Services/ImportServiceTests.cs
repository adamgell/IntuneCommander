using System.Text.Json;
using NSubstitute;
using Intune.Commander.Core.Models;
using Intune.Commander.Core.Services;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Core.Tests.Services;

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

        var sut = new ImportService(DefaultCfgSvc());
        var read = await sut.ReadDeviceConfigurationAsync(file);

        Assert.NotNull(read);
        Assert.Equal("cfg-1", read.Id);
        Assert.Equal("Config One", read.DisplayName);
    }

    [Fact]
    public async Task ReadDeviceConfigurationsFromFolderAsync_MissingFolder_ReturnsEmpty()
    {
        var sut = new ImportService(DefaultCfgSvc());

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

        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadDeviceConfigurationsFromFolderAsync(_tempDir);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, c => c.Id == "a");
        Assert.Contains(result, c => c.Id == "b");
    }

    [Fact]
    public async Task ReadMigrationTableAsync_MissingFile_ReturnsEmpty()
    {
        var sut = new ImportService(DefaultCfgSvc());

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

        var sut = new ImportService(DefaultCfgSvc());
        var read = await sut.ReadMigrationTableAsync(_tempDir);

        Assert.Single(read.Entries);
        Assert.Equal("old-1", read.Entries[0].OriginalId);
    }

    [Fact]
    public async Task ImportDeviceConfigurationAsync_ClearsReadOnlyFields_AndUpdatesMigration()
    {
        DeviceConfiguration? capturedConfig = null;
        var cfgService = Substitute.For<IConfigurationProfileService>();
        cfgService.CreateDeviceConfigurationAsync(
            Arg.Do<DeviceConfiguration>(c => capturedConfig = c),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new DeviceConfiguration { Id = "new-cfg", DisplayName = "Created" }));
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
        Assert.NotNull(capturedConfig);
        Assert.Null(capturedConfig!.Id);
        Assert.Null(capturedConfig.CreatedDateTime);
        Assert.Null(capturedConfig.LastModifiedDateTime);
        Assert.Null(capturedConfig.Version);
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

        var sut = new ImportService(DefaultCfgSvc(), DefaultComplianceSvc());
        var read = await sut.ReadCompliancePolicyAsync(file);

        Assert.NotNull(read);
        Assert.Equal("p1", read.Policy.Id);
    }

    [Fact]
    public async Task ReadCompliancePoliciesFromFolderAsync_MissingFolder_ReturnsEmpty()
    {
        var sut = new ImportService(DefaultCfgSvc(), DefaultComplianceSvc());

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

        var sut = new ImportService(DefaultCfgSvc(), DefaultComplianceSvc());
        var result = await sut.ReadCompliancePoliciesFromFolderAsync(_tempDir);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ImportCompliancePolicyAsync_WithoutService_Throws()
    {
        var sut = new ImportService(DefaultCfgSvc(), null);
        var table = new MigrationTable();
        var export = new CompliancePolicyExport
        {
            Policy = new DeviceCompliancePolicy { Id = "old", DisplayName = "Old" }
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ImportCompliancePolicyAsync(export, table));
    }

    [Fact]
    public async Task ImportEndpointSecurityIntentAsync_WithoutService_Throws()
    {
        var sut = new ImportService(DefaultCfgSvc(), null);
        var table = new MigrationTable();
        var export = new EndpointSecurityExport { Intent = new DeviceManagementIntent { Id = "old", DisplayName = "Old" } };

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ImportEndpointSecurityIntentAsync(export, table));
    }

    [Fact]
    public async Task ImportAdministrativeTemplateAsync_WithoutService_Throws()
    {
        var sut = new ImportService(DefaultCfgSvc(), null);
        var table = new MigrationTable();
        var export = new AdministrativeTemplateExport { Template = new GroupPolicyConfiguration { Id = "old", DisplayName = "Old" } };

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ImportAdministrativeTemplateAsync(export, table));
    }

    [Fact]
    public async Task ImportEnrollmentConfigurationAsync_WithoutService_Throws()
    {
        var sut = new ImportService(DefaultCfgSvc(), null);
        var table = new MigrationTable();
        var config = new DeviceEnrollmentConfiguration { Id = "old", DisplayName = "Old" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ImportEnrollmentConfigurationAsync(config, table));
    }

    [Fact]
    public async Task ImportAppProtectionPolicyAsync_WithoutService_Throws()
    {
        var sut = new ImportService(DefaultCfgSvc(), null);
        var table = new MigrationTable();
        var policy = new AndroidManagedAppProtection { Id = "old", DisplayName = "Old" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ImportAppProtectionPolicyAsync(policy, table));
    }

    [Fact]
    public async Task ImportManagedDeviceAppConfigurationAsync_WithoutService_Throws()
    {
        var sut = new ImportService(DefaultCfgSvc(), null);
        var table = new MigrationTable();
        var config = new ManagedDeviceMobileAppConfiguration { Id = "old", DisplayName = "Old" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ImportManagedDeviceAppConfigurationAsync(config, table));
    }

    [Fact]
    public async Task ImportTargetedManagedAppConfigurationAsync_WithoutService_Throws()
    {
        var sut = new ImportService(DefaultCfgSvc(), null);
        var table = new MigrationTable();
        var config = new TargetedManagedAppConfiguration { Id = "old", DisplayName = "Old" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ImportTargetedManagedAppConfigurationAsync(config, table));
    }

    [Fact]
    public async Task ImportTermsAndConditionsAsync_WithoutService_Throws()
    {
        var sut = new ImportService(DefaultCfgSvc(), null);
        var table = new MigrationTable();
        var terms = new TermsAndConditions { Id = "old", DisplayName = "Old" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ImportTermsAndConditionsAsync(terms, table));
    }

    [Fact]
    public async Task ImportScopeTagAsync_WithoutService_Throws()
    {
        var sut = new ImportService(DefaultCfgSvc(), null);
        var table = new MigrationTable();
        var scopeTag = new RoleScopeTag { Id = "old", DisplayName = "Old" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ImportScopeTagAsync(scopeTag, table));
    }

    [Fact]
    public async Task ImportRoleDefinitionAsync_WithoutService_Throws()
    {
        var sut = new ImportService(DefaultCfgSvc(), null);
        var table = new MigrationTable();
        var roleDefinition = new RoleDefinition { Id = "old", DisplayName = "Old" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ImportRoleDefinitionAsync(roleDefinition, table));
    }

    [Fact]
    public async Task ImportIntuneBrandingProfileAsync_WithoutService_Throws()
    {
        var sut = new ImportService(DefaultCfgSvc(), null);
        var table = new MigrationTable();
        var profile = new IntuneBrandingProfile { Id = "old", ProfileName = "Old" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ImportIntuneBrandingProfileAsync(profile, table));
    }

    [Fact]
    public async Task ImportAzureBrandingLocalizationAsync_WithoutService_Throws()
    {
        var sut = new ImportService(DefaultCfgSvc(), null);
        var table = new MigrationTable();
        var localization = new OrganizationalBrandingLocalization { Id = "old" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ImportAzureBrandingLocalizationAsync(localization, table));
    }

    [Fact]
    public async Task ImportAutopilotProfileAsync_WithoutService_Throws()
    {
        var sut = new ImportService(DefaultCfgSvc(), null);
        var table = new MigrationTable();
        var profile = new WindowsAutopilotDeploymentProfile { Id = "old", DisplayName = "Old" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ImportAutopilotProfileAsync(profile, table));
    }

    [Fact]
    public async Task ImportDeviceHealthScriptAsync_WithoutService_Throws()
    {
        var sut = new ImportService(DefaultCfgSvc(), null);
        var table = new MigrationTable();
        var script = new DeviceHealthScript { Id = "old", DisplayName = "Old" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ImportDeviceHealthScriptAsync(script, table));
    }

    [Fact]
    public async Task ImportMacCustomAttributeAsync_WithoutService_Throws()
    {
        var sut = new ImportService(DefaultCfgSvc(), null);
        var table = new MigrationTable();
        var script = new DeviceCustomAttributeShellScript { Id = "old", DisplayName = "Old" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ImportMacCustomAttributeAsync(script, table));
    }

    [Fact]
    public async Task ImportFeatureUpdateProfileAsync_WithoutService_Throws()
    {
        var sut = new ImportService(DefaultCfgSvc(), null);
        var table = new MigrationTable();
        var profile = new WindowsFeatureUpdateProfile { Id = "old", DisplayName = "Old" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ImportFeatureUpdateProfileAsync(profile, table));
    }

    [Fact]
    public async Task ImportNamedLocationAsync_WithoutService_Throws()
    {
        var sut = new ImportService(DefaultCfgSvc(), null);
        var table = new MigrationTable();
        var namedLocation = new NamedLocation { Id = "old" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ImportNamedLocationAsync(namedLocation, table));
    }

    [Fact]
    public async Task ImportAuthenticationStrengthPolicyAsync_WithoutService_Throws()
    {
        var sut = new ImportService(DefaultCfgSvc(), null);
        var table = new MigrationTable();
        var policy = new AuthenticationStrengthPolicy { Id = "old", DisplayName = "Old" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ImportAuthenticationStrengthPolicyAsync(policy, table));
    }

    [Fact]
    public async Task ImportAuthenticationContextAsync_WithoutService_Throws()
    {
        var sut = new ImportService(DefaultCfgSvc(), null);
        var table = new MigrationTable();
        var context = new AuthenticationContextClassReference { Id = "old", DisplayName = "Old" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ImportAuthenticationContextAsync(context, table));
    }

    [Fact]
    public async Task ImportTermsOfUseAgreementAsync_WithoutService_Throws()
    {
        var sut = new ImportService(DefaultCfgSvc(), null);
        var table = new MigrationTable();
        var agreement = new Agreement { Id = "old", DisplayName = "Old" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ImportTermsOfUseAgreementAsync(agreement, table));
    }

    [Fact]
    public async Task ImportCompliancePolicyAsync_AssignsAndUpdatesMigration()
    {
        DeviceCompliancePolicy? capturedPolicy = null;
        string? assignedPolicyId = null;
        List<DeviceCompliancePolicyAssignment>? assignedAssignments = null;
        var complianceService = Substitute.For<ICompliancePolicyService>();
        complianceService.CreateCompliancePolicyAsync(
            Arg.Do<DeviceCompliancePolicy>(p => capturedPolicy = p),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new DeviceCompliancePolicy { Id = "new-pol", DisplayName = "Created Policy" }));
        complianceService.AssignPolicyAsync(
            Arg.Do<string>(id => assignedPolicyId = id),
            Arg.Do<List<DeviceCompliancePolicyAssignment>>(a => assignedAssignments = a),
            Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new ImportService(DefaultCfgSvc(), complianceService);
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
        Assert.NotNull(capturedPolicy);
        Assert.Null(capturedPolicy!.Id);
        Assert.Null(capturedPolicy.CreatedDateTime);
        Assert.Null(capturedPolicy.LastModifiedDateTime);
        Assert.Null(capturedPolicy.Version);

        Assert.NotNull(assignedPolicyId);
        Assert.Equal("new-pol", assignedPolicyId);
        Assert.All(assignedAssignments!, a => Assert.Null(a.Id));

        Assert.Single(table.Entries);
        Assert.Equal("old-pol", table.Entries[0].OriginalId);
        Assert.Equal("new-pol", table.Entries[0].NewId);
    }

    [Fact]
    public async Task ImportEndpointSecurityIntentAsync_AssignsAndUpdatesMigration()
    {
        string? assignedIntentId = null;
        List<DeviceManagementIntentAssignment>? assignedIntentAssignments = null;
        var endpointService = Substitute.For<IEndpointSecurityService>();
        endpointService.CreateEndpointSecurityIntentAsync(
            Arg.Any<DeviceManagementIntent>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new DeviceManagementIntent { Id = "new-intent", DisplayName = "Created Intent" }));
        endpointService.AssignIntentAsync(
            Arg.Do<string>(id => assignedIntentId = id),
            Arg.Do<List<DeviceManagementIntentAssignment>>(a => assignedIntentAssignments = a),
            Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var sut = new ImportService(DefaultCfgSvc(), null, endpointSecurityService: endpointService);
        var table = new MigrationTable();

        var export = new EndpointSecurityExport
        {
            Intent = new DeviceManagementIntent { Id = "old-intent", DisplayName = "Source Intent" },
            Assignments = [new DeviceManagementIntentAssignment { Id = "ea-1" }]
        };

        var created = await sut.ImportEndpointSecurityIntentAsync(export, table);

        Assert.Equal("new-intent", created.Id);
        Assert.NotNull(assignedIntentId);
        Assert.Equal("new-intent", assignedIntentId);
        Assert.All(assignedIntentAssignments!, a => Assert.Null(a.Id));
        Assert.Single(table.Entries);
        Assert.Equal("EndpointSecurityIntent", table.Entries[0].ObjectType);
        Assert.Equal("old-intent", table.Entries[0].OriginalId);
        Assert.Equal("new-intent", table.Entries[0].NewId);
    }

    [Fact]
    public async Task ImportAdministrativeTemplateAsync_AssignsAndUpdatesMigration()
    {
        GroupPolicyConfiguration? capturedTemplate = null;
        string? assignedTemplateId = null;
        List<GroupPolicyConfigurationAssignment>? assignedTemplateAssignments = null;
        var templateService = Substitute.For<IAdministrativeTemplateService>();
        templateService.CreateAdministrativeTemplateAsync(
            Arg.Do<GroupPolicyConfiguration>(t => capturedTemplate = t),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GroupPolicyConfiguration { Id = "new-template", DisplayName = "Created Template" }));
        templateService.AssignAdministrativeTemplateAsync(
            Arg.Do<string>(id => assignedTemplateId = id),
            Arg.Do<List<GroupPolicyConfigurationAssignment>>(a => assignedTemplateAssignments = a),
            Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var sut = new ImportService(DefaultCfgSvc(), null, administrativeTemplateService: templateService);
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
        Assert.NotNull(capturedTemplate);
        Assert.Null(capturedTemplate!.Id);
        Assert.Null(capturedTemplate.CreatedDateTime);
        Assert.Null(capturedTemplate.LastModifiedDateTime);
        Assert.NotNull(assignedTemplateId);
        Assert.Equal("new-template", assignedTemplateId);
        Assert.All(assignedTemplateAssignments!, a => Assert.Null(a.Id));
        Assert.Single(table.Entries);
        Assert.Equal("AdministrativeTemplate", table.Entries[0].ObjectType);
        Assert.Equal("old-template", table.Entries[0].OriginalId);
        Assert.Equal("new-template", table.Entries[0].NewId);
    }

    [Fact]
    public async Task ImportEnrollmentConfigurationAsync_UpdatesMigration()
    {
        DeviceEnrollmentConfiguration? capturedEnrollment = null;
        var enrollmentService = Substitute.For<IEnrollmentConfigurationService>();
        enrollmentService.CreateEnrollmentConfigurationAsync(
            Arg.Do<DeviceEnrollmentConfiguration>(c => capturedEnrollment = c),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new DeviceEnrollmentConfiguration { Id = "new-enroll", DisplayName = "Created Enrollment" }));
        var sut = new ImportService(DefaultCfgSvc(), null, enrollmentConfigurationService: enrollmentService);
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
        Assert.NotNull(capturedEnrollment);
        Assert.Null(capturedEnrollment!.Id);
        Assert.Null(capturedEnrollment.CreatedDateTime);
        Assert.Null(capturedEnrollment.LastModifiedDateTime);
        Assert.Single(table.Entries);
        Assert.Equal("EnrollmentConfiguration", table.Entries[0].ObjectType);
        Assert.Equal("old-enroll", table.Entries[0].OriginalId);
        Assert.Equal("new-enroll", table.Entries[0].NewId);
    }

    [Fact]
    public async Task ImportAppProtectionPolicyAsync_UpdatesMigration()
    {
        ManagedAppPolicy? capturedAppPolicy = null;
        var appProtectionService = Substitute.For<IAppProtectionPolicyService>();
        appProtectionService.CreateAppProtectionPolicyAsync(
            Arg.Do<ManagedAppPolicy>(p => capturedAppPolicy = p),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ManagedAppPolicy>(new AndroidManagedAppProtection { Id = "new-app-protect", DisplayName = "Created App Protection" }));
        var sut = new ImportService(DefaultCfgSvc(), null, appProtectionPolicyService: appProtectionService);
        var table = new MigrationTable();

        var policy = new AndroidManagedAppProtection
        {
            Id = "old-app-protect",
            DisplayName = "Source App Protection"
        };

        var created = await sut.ImportAppProtectionPolicyAsync(policy, table);

        Assert.Equal("new-app-protect", created.Id);
        Assert.NotNull(capturedAppPolicy);
        Assert.Null(capturedAppPolicy!.Id);
        Assert.Single(table.Entries);
        Assert.Equal("AppProtectionPolicy", table.Entries[0].ObjectType);
        Assert.Equal("old-app-protect", table.Entries[0].OriginalId);
        Assert.Equal("new-app-protect", table.Entries[0].NewId);
    }

    [Fact]
    public async Task ImportManagedDeviceAppConfigurationAsync_UpdatesMigration()
    {
        ManagedDeviceMobileAppConfiguration? capturedMdac = null;
        var managedConfigService = Substitute.For<IManagedAppConfigurationService>();
        managedConfigService.CreateManagedDeviceAppConfigurationAsync(
            Arg.Do<ManagedDeviceMobileAppConfiguration>(c => capturedMdac = c),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ManagedDeviceMobileAppConfiguration { Id = "new-mdac", DisplayName = "Created MDAC" }));
        var sut = new ImportService(DefaultCfgSvc(), null, managedAppConfigurationService: managedConfigService);
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
        Assert.NotNull(capturedMdac);
        Assert.Null(capturedMdac!.Id);
        Assert.Null(capturedMdac.CreatedDateTime);
        Assert.Null(capturedMdac.LastModifiedDateTime);
        Assert.Null(capturedMdac.Version);
        Assert.Single(table.Entries);
        Assert.Equal("ManagedDeviceAppConfiguration", table.Entries[0].ObjectType);
        Assert.Equal("old-mdac", table.Entries[0].OriginalId);
        Assert.Equal("new-mdac", table.Entries[0].NewId);
    }

    [Fact]
    public async Task ImportTargetedManagedAppConfigurationAsync_UpdatesMigration()
    {
        TargetedManagedAppConfiguration? capturedTmac = null;
        var managedConfigService = Substitute.For<IManagedAppConfigurationService>();
        managedConfigService.CreateTargetedManagedAppConfigurationAsync(
            Arg.Do<TargetedManagedAppConfiguration>(c => capturedTmac = c),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TargetedManagedAppConfiguration { Id = "new-tmac", DisplayName = "Created TMAC" }));
        var sut = new ImportService(DefaultCfgSvc(), null, managedAppConfigurationService: managedConfigService);
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
        Assert.NotNull(capturedTmac);
        Assert.Null(capturedTmac!.Id);
        Assert.Null(capturedTmac.CreatedDateTime);
        Assert.Null(capturedTmac.LastModifiedDateTime);
        Assert.Null(capturedTmac.Version);
        Assert.Single(table.Entries);
        Assert.Equal("TargetedManagedAppConfiguration", table.Entries[0].ObjectType);
        Assert.Equal("old-tmac", table.Entries[0].OriginalId);
        Assert.Equal("new-tmac", table.Entries[0].NewId);
    }

    [Fact]
    public async Task ImportTermsAndConditionsAsync_UpdatesMigration()
    {
        TermsAndConditions? capturedTerms = null;
        var termsService = Substitute.For<ITermsAndConditionsService>();
        termsService.CreateTermsAndConditionsAsync(
            Arg.Do<TermsAndConditions>(t => capturedTerms = t),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TermsAndConditions { Id = "new-terms", DisplayName = "Created Terms" }));
        var sut = new ImportService(DefaultCfgSvc(), null, termsAndConditionsService: termsService);
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
        Assert.NotNull(capturedTerms);
        Assert.Null(capturedTerms!.Id);
        Assert.Null(capturedTerms.CreatedDateTime);
        Assert.Null(capturedTerms.LastModifiedDateTime);
        Assert.Null(capturedTerms.Version);
        Assert.Single(table.Entries);
        Assert.Equal("TermsAndConditions", table.Entries[0].ObjectType);
        Assert.Equal("old-terms", table.Entries[0].OriginalId);
        Assert.Equal("new-terms", table.Entries[0].NewId);
    }

    [Fact]
    public async Task ImportScopeTagAsync_UpdatesMigration()
    {
        RoleScopeTag? capturedScopeTag = null;
        var scopeTagService = Substitute.For<IScopeTagService>();
        scopeTagService.CreateScopeTagAsync(
            Arg.Do<RoleScopeTag>(s => capturedScopeTag = s),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new RoleScopeTag { Id = "new-scope", DisplayName = "Created Scope" }));
        var sut = new ImportService(DefaultCfgSvc(), null, scopeTagService: scopeTagService);
        var table = new MigrationTable();

        var scopeTag = new RoleScopeTag
        {
            Id = "old-scope",
            DisplayName = "Source Scope"
        };

        var created = await sut.ImportScopeTagAsync(scopeTag, table);

        Assert.Equal("new-scope", created.Id);
        Assert.NotNull(capturedScopeTag);
        Assert.Null(capturedScopeTag!.Id);
        Assert.Single(table.Entries);
        Assert.Equal("ScopeTag", table.Entries[0].ObjectType);
        Assert.Equal("old-scope", table.Entries[0].OriginalId);
        Assert.Equal("new-scope", table.Entries[0].NewId);
    }

    [Fact]
    public async Task ImportRoleDefinitionAsync_UpdatesMigration()
    {
        RoleDefinition? capturedRoleDefinition = null;
        var roleDefinitionService = Substitute.For<IRoleDefinitionService>();
        roleDefinitionService.CreateRoleDefinitionAsync(
            Arg.Do<RoleDefinition>(r => capturedRoleDefinition = r),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new RoleDefinition { Id = "new-role", DisplayName = "Created Role" }));
        var sut = new ImportService(DefaultCfgSvc(), null, roleDefinitionService: roleDefinitionService);
        var table = new MigrationTable();

        var roleDefinition = new RoleDefinition
        {
            Id = "old-role",
            DisplayName = "Source Role"
        };

        var created = await sut.ImportRoleDefinitionAsync(roleDefinition, table);

        Assert.Equal("new-role", created.Id);
        Assert.NotNull(capturedRoleDefinition);
        Assert.Null(capturedRoleDefinition!.Id);
        Assert.Single(table.Entries);
        Assert.Equal("RoleDefinition", table.Entries[0].ObjectType);
        Assert.Equal("old-role", table.Entries[0].OriginalId);
        Assert.Equal("new-role", table.Entries[0].NewId);
    }

    [Fact]
    public async Task ImportIntuneBrandingProfileAsync_UpdatesMigration()
    {
        IntuneBrandingProfile? capturedBrandingProfile = null;
        var brandingService = Substitute.For<IIntuneBrandingService>();
        brandingService.CreateIntuneBrandingProfileAsync(
            Arg.Do<IntuneBrandingProfile>(p => capturedBrandingProfile = p),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new IntuneBrandingProfile { Id = "new-branding", ProfileName = "Created Branding" }));
        var sut = new ImportService(DefaultCfgSvc(), null, intuneBrandingService: brandingService);
        var table = new MigrationTable();

        var profile = new IntuneBrandingProfile
        {
            Id = "old-branding",
            ProfileName = "Source Branding"
        };

        var created = await sut.ImportIntuneBrandingProfileAsync(profile, table);

        Assert.Equal("new-branding", created.Id);
        Assert.NotNull(capturedBrandingProfile);
        Assert.Null(capturedBrandingProfile!.Id);
        Assert.Single(table.Entries);
        Assert.Equal("IntuneBrandingProfile", table.Entries[0].ObjectType);
        Assert.Equal("old-branding", table.Entries[0].OriginalId);
        Assert.Equal("new-branding", table.Entries[0].NewId);
    }

    [Fact]
    public async Task ImportAzureBrandingLocalizationAsync_UpdatesMigration()
    {
        OrganizationalBrandingLocalization? capturedLocalization = null;
        var azureBrandingService = Substitute.For<IAzureBrandingService>();
        azureBrandingService.CreateBrandingLocalizationAsync(
            Arg.Do<OrganizationalBrandingLocalization>(l => capturedLocalization = l),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new OrganizationalBrandingLocalization { Id = "new-locale" }));
        var sut = new ImportService(DefaultCfgSvc(), null, azureBrandingService: azureBrandingService);
        var table = new MigrationTable();

        var localization = new OrganizationalBrandingLocalization
        {
            Id = "old-locale"
        };

        var created = await sut.ImportAzureBrandingLocalizationAsync(localization, table);

        Assert.Equal("new-locale", created.Id);
        Assert.NotNull(capturedLocalization);
        Assert.Null(capturedLocalization!.Id);
        Assert.Single(table.Entries);
        Assert.Equal("AzureBrandingLocalization", table.Entries[0].ObjectType);
        Assert.Equal("old-locale", table.Entries[0].OriginalId);
        Assert.Equal("new-locale", table.Entries[0].NewId);
    }

    [Fact]
    public async Task ImportAutopilotProfileAsync_UpdatesMigration()
    {
        WindowsAutopilotDeploymentProfile? capturedAutopilot = null;
        var autopilotService = Substitute.For<IAutopilotService>();
        autopilotService.CreateAutopilotProfileAsync(
            Arg.Do<WindowsAutopilotDeploymentProfile>(p => capturedAutopilot = p),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new WindowsAutopilotDeploymentProfile { Id = "new-autopilot", DisplayName = "Created Autopilot" }));
        var sut = new ImportService(DefaultCfgSvc(), null, autopilotService: autopilotService);
        var table = new MigrationTable();

        var profile = new WindowsAutopilotDeploymentProfile
        {
            Id = "old-autopilot",
            DisplayName = "Source Autopilot"
        };

        var created = await sut.ImportAutopilotProfileAsync(profile, table);

        Assert.Equal("new-autopilot", created.Id);
        Assert.NotNull(capturedAutopilot);
        Assert.Null(capturedAutopilot!.Id);
        Assert.Single(table.Entries);
        Assert.Equal("AutopilotProfile", table.Entries[0].ObjectType);
        Assert.Equal("old-autopilot", table.Entries[0].OriginalId);
        Assert.Equal("new-autopilot", table.Entries[0].NewId);
    }

    [Fact]
    public async Task ImportTermsOfUseAgreementAsync_UpdatesMigration()
    {
        Agreement? capturedAgreement = null;
        var termsOfUseService = Substitute.For<ITermsOfUseService>();
        termsOfUseService.CreateTermsOfUseAgreementAsync(
            Arg.Do<Agreement>(a => capturedAgreement = a),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new Agreement { Id = "new-tou", DisplayName = "Created Terms" }));
        var sut = new ImportService(DefaultCfgSvc(), null, termsOfUseService: termsOfUseService);
        var table = new MigrationTable();

        var agreement = new Agreement
        {
            Id = "old-tou",
            DisplayName = "Source Terms"
        };

        var created = await sut.ImportTermsOfUseAgreementAsync(agreement, table);

        Assert.Equal("new-tou", created.Id);
        Assert.NotNull(capturedAgreement);
        Assert.Null(capturedAgreement!.Id);
        Assert.Single(table.Entries);
        Assert.Equal("TermsOfUseAgreement", table.Entries[0].ObjectType);
        Assert.Equal("old-tou", table.Entries[0].OriginalId);
        Assert.Equal("new-tou", table.Entries[0].NewId);
    }

    [Fact]
    public async Task ReadDeviceConfigurationAsync_MalformedJson_ThrowsJsonException()
    {
        var file = Path.Combine(_tempDir, "bad.json");
        await File.WriteAllTextAsync(file, "{ this is not valid json }}}");

        var sut = new ImportService(DefaultCfgSvc());

        await Assert.ThrowsAsync<JsonException>(() => sut.ReadDeviceConfigurationAsync(file));
    }

    [Fact]
    public async Task ImportDeviceHealthScriptAsync_UpdatesMigration()
    {
        DeviceHealthScript? capturedHealthScript = null;
        var healthScriptService = Substitute.For<IDeviceHealthScriptService>();
        healthScriptService.CreateDeviceHealthScriptAsync(
            Arg.Do<DeviceHealthScript>(s => capturedHealthScript = s),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new DeviceHealthScript { Id = "new-dhs", DisplayName = "Created Script" }));
        var sut = new ImportService(DefaultCfgSvc(), null, deviceHealthScriptService: healthScriptService);
        var table = new MigrationTable();

        var script = new DeviceHealthScript
        {
            Id = "old-dhs",
            DisplayName = "Source Script",
            CreatedDateTime = DateTimeOffset.UtcNow,
            LastModifiedDateTime = DateTimeOffset.UtcNow
        };

        var created = await sut.ImportDeviceHealthScriptAsync(script, table);

        Assert.Equal("new-dhs", created.Id);
        Assert.NotNull(capturedHealthScript);
        Assert.Null(capturedHealthScript!.Id);
        Assert.Single(table.Entries);
        Assert.Equal("DeviceHealthScript", table.Entries[0].ObjectType);
        Assert.Equal("old-dhs", table.Entries[0].OriginalId);
        Assert.Equal("new-dhs", table.Entries[0].NewId);
    }

    [Fact]
    public async Task ImportMacCustomAttributeAsync_UpdatesMigration()
    {
        DeviceCustomAttributeShellScript? capturedMacAttribute = null;
        var macAttributeService = Substitute.For<IMacCustomAttributeService>();
        macAttributeService.CreateMacCustomAttributeAsync(
            Arg.Do<DeviceCustomAttributeShellScript>(s => capturedMacAttribute = s),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new DeviceCustomAttributeShellScript { Id = "new-mac", DisplayName = "Created Attr" }));
        var sut = new ImportService(DefaultCfgSvc(), null, macCustomAttributeService: macAttributeService);
        var table = new MigrationTable();

        var script = new DeviceCustomAttributeShellScript
        {
            Id = "old-mac",
            DisplayName = "Source Attr",
            CreatedDateTime = DateTimeOffset.UtcNow,
            LastModifiedDateTime = DateTimeOffset.UtcNow
        };

        var created = await sut.ImportMacCustomAttributeAsync(script, table);

        Assert.Equal("new-mac", created.Id);
        Assert.NotNull(capturedMacAttribute);
        Assert.Null(capturedMacAttribute!.Id);
        Assert.Single(table.Entries);
        Assert.Equal("MacCustomAttribute", table.Entries[0].ObjectType);
        Assert.Equal("old-mac", table.Entries[0].OriginalId);
        Assert.Equal("new-mac", table.Entries[0].NewId);
    }

    [Fact]
    public async Task ImportFeatureUpdateProfileAsync_UpdatesMigration()
    {
        WindowsFeatureUpdateProfile? capturedFeatureUpdate = null;
        var featureUpdateService = Substitute.For<IFeatureUpdateProfileService>();
        featureUpdateService.CreateFeatureUpdateProfileAsync(
            Arg.Do<WindowsFeatureUpdateProfile>(p => capturedFeatureUpdate = p),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new WindowsFeatureUpdateProfile { Id = "new-fup", DisplayName = "Created Feature Update" }));
        var sut = new ImportService(DefaultCfgSvc(), null, featureUpdateProfileService: featureUpdateService);
        var table = new MigrationTable();

        var profile = new WindowsFeatureUpdateProfile
        {
            Id = "old-fup",
            DisplayName = "Source Feature Update",
            CreatedDateTime = DateTimeOffset.UtcNow,
            LastModifiedDateTime = DateTimeOffset.UtcNow
        };

        var created = await sut.ImportFeatureUpdateProfileAsync(profile, table);

        Assert.Equal("new-fup", created.Id);
        Assert.NotNull(capturedFeatureUpdate);
        Assert.Null(capturedFeatureUpdate!.Id);
        Assert.Single(table.Entries);
        Assert.Equal("FeatureUpdateProfile", table.Entries[0].ObjectType);
        Assert.Equal("old-fup", table.Entries[0].OriginalId);
        Assert.Equal("new-fup", table.Entries[0].NewId);
    }

    [Fact]
    public async Task ImportNamedLocationAsync_UpdatesMigration()
    {
        NamedLocation? capturedNamedLocation = null;
        var namedLocationService = Substitute.For<INamedLocationService>();
        namedLocationService.CreateNamedLocationAsync(
            Arg.Do<NamedLocation>(l => capturedNamedLocation = l),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new NamedLocation
            {
                Id = "new-nl",
                AdditionalData = new Dictionary<string, object> { ["displayName"] = "Created Location" }
            }));
        var sut = new ImportService(DefaultCfgSvc(), null, namedLocationService: namedLocationService);
        var table = new MigrationTable();

        var namedLocation = new NamedLocation
        {
            Id = "old-nl",
            AdditionalData = new Dictionary<string, object> { ["displayName"] = "Source Location" }
        };

        var created = await sut.ImportNamedLocationAsync(namedLocation, table);

        Assert.Equal("new-nl", created.Id);
        Assert.NotNull(capturedNamedLocation);
        Assert.Null(capturedNamedLocation!.Id);
        Assert.Single(table.Entries);
        Assert.Equal("NamedLocation", table.Entries[0].ObjectType);
        Assert.Equal("old-nl", table.Entries[0].OriginalId);
        Assert.Equal("new-nl", table.Entries[0].NewId);
    }

    [Fact]
    public async Task ImportAuthenticationStrengthPolicyAsync_UpdatesMigration()
    {
        AuthenticationStrengthPolicy? capturedStrengthPolicy = null;
        var authStrengthService = Substitute.For<IAuthenticationStrengthService>();
        authStrengthService.CreateAuthenticationStrengthPolicyAsync(
            Arg.Do<AuthenticationStrengthPolicy>(p => capturedStrengthPolicy = p),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new AuthenticationStrengthPolicy { Id = "new-asp", DisplayName = "Created Strength" }));
        var sut = new ImportService(DefaultCfgSvc(), null, authenticationStrengthService: authStrengthService);
        var table = new MigrationTable();

        var policy = new AuthenticationStrengthPolicy
        {
            Id = "old-asp",
            DisplayName = "Source Strength"
        };

        var created = await sut.ImportAuthenticationStrengthPolicyAsync(policy, table);

        Assert.Equal("new-asp", created.Id);
        Assert.NotNull(capturedStrengthPolicy);
        Assert.Null(capturedStrengthPolicy!.Id);
        Assert.Single(table.Entries);
        Assert.Equal("AuthenticationStrengthPolicy", table.Entries[0].ObjectType);
        Assert.Equal("old-asp", table.Entries[0].OriginalId);
        Assert.Equal("new-asp", table.Entries[0].NewId);
    }

    [Fact]
    public async Task ImportAuthenticationContextAsync_UpdatesMigration()
    {
        AuthenticationContextClassReference? capturedAuthContext = null;
        var authContextService = Substitute.For<IAuthenticationContextService>();
        authContextService.CreateAuthenticationContextAsync(
            Arg.Do<AuthenticationContextClassReference>(c => capturedAuthContext = c),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new AuthenticationContextClassReference { Id = "new-ctx", DisplayName = "Created Context" }));
        var sut = new ImportService(DefaultCfgSvc(), null, authenticationContextService: authContextService);
        var table = new MigrationTable();

        var context = new AuthenticationContextClassReference
        {
            Id = "old-ctx",
            DisplayName = "Source Context"
        };

        var created = await sut.ImportAuthenticationContextAsync(context, table);

        Assert.Equal("new-ctx", created.Id);
        Assert.NotNull(capturedAuthContext);
        Assert.Null(capturedAuthContext!.Id);
        Assert.Single(table.Entries);
        Assert.Equal("AuthenticationContext", table.Entries[0].ObjectType);
        Assert.Equal("old-ctx", table.Entries[0].OriginalId);
        Assert.Equal("new-ctx", table.Entries[0].NewId);
    }

    // ---------- Legacy 12-param constructor ----------

    [Fact]
    public void LegacyConstructor_12Params_IsCallable()
    {
        var sut = new ImportService(
            DefaultCfgSvc(), null, null, null, null, null, null, null, null, null, null, null);
        Assert.NotNull(sut);
    }

    // ---------- ReadFromFolder – missing folder → empty ----------

    [Fact]
    public async Task ReadEndpointSecurityIntentsFromFolderAsync_MissingFolder_ReturnsEmpty()
    {
        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadEndpointSecurityIntentsFromFolderAsync(_tempDir);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ReadAdministrativeTemplatesFromFolderAsync_MissingFolder_ReturnsEmpty()
    {
        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadAdministrativeTemplatesFromFolderAsync(_tempDir);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ReadEnrollmentConfigurationsFromFolderAsync_MissingFolder_ReturnsEmpty()
    {
        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadEnrollmentConfigurationsFromFolderAsync(_tempDir);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ReadAppProtectionPoliciesFromFolderAsync_MissingFolder_ReturnsEmpty()
    {
        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadAppProtectionPoliciesFromFolderAsync(_tempDir);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ReadManagedDeviceAppConfigurationsFromFolderAsync_MissingFolder_ReturnsEmpty()
    {
        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadManagedDeviceAppConfigurationsFromFolderAsync(_tempDir);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ReadTargetedManagedAppConfigurationsFromFolderAsync_MissingFolder_ReturnsEmpty()
    {
        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadTargetedManagedAppConfigurationsFromFolderAsync(_tempDir);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ReadTermsAndConditionsFromFolderAsync_MissingFolder_ReturnsEmpty()
    {
        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadTermsAndConditionsFromFolderAsync(_tempDir);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ReadScopeTagsFromFolderAsync_MissingFolder_ReturnsEmpty()
    {
        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadScopeTagsFromFolderAsync(_tempDir);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ReadRoleDefinitionsFromFolderAsync_MissingFolder_ReturnsEmpty()
    {
        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadRoleDefinitionsFromFolderAsync(_tempDir);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ReadIntuneBrandingProfilesFromFolderAsync_MissingFolder_ReturnsEmpty()
    {
        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadIntuneBrandingProfilesFromFolderAsync(_tempDir);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ReadAzureBrandingLocalizationsFromFolderAsync_MissingFolder_ReturnsEmpty()
    {
        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadAzureBrandingLocalizationsFromFolderAsync(_tempDir);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ReadAutopilotProfilesFromFolderAsync_MissingFolder_ReturnsEmpty()
    {
        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadAutopilotProfilesFromFolderAsync(_tempDir);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ReadDeviceHealthScriptsFromFolderAsync_MissingFolder_ReturnsEmpty()
    {
        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadDeviceHealthScriptsFromFolderAsync(_tempDir);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ReadMacCustomAttributesFromFolderAsync_MissingFolder_ReturnsEmpty()
    {
        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadMacCustomAttributesFromFolderAsync(_tempDir);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ReadFeatureUpdateProfilesFromFolderAsync_MissingFolder_ReturnsEmpty()
    {
        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadFeatureUpdateProfilesFromFolderAsync(_tempDir);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ReadNamedLocationsFromFolderAsync_MissingFolder_ReturnsEmpty()
    {
        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadNamedLocationsFromFolderAsync(_tempDir);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ReadAuthenticationStrengthPoliciesFromFolderAsync_MissingFolder_ReturnsEmpty()
    {
        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadAuthenticationStrengthPoliciesFromFolderAsync(_tempDir);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ReadAuthenticationContextsFromFolderAsync_MissingFolder_ReturnsEmpty()
    {
        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadAuthenticationContextsFromFolderAsync(_tempDir);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ReadTermsOfUseAgreementsFromFolderAsync_MissingFolder_ReturnsEmpty()
    {
        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadTermsOfUseAgreementsFromFolderAsync(_tempDir);
        Assert.Empty(result);
    }

    // ---------- ReadFromFolder – folder with files → reads all ----------

    [Fact]
    public async Task ReadEndpointSecurityIntentsFromFolderAsync_ReadsAllJsonFiles()
    {
        var folder = Path.Combine(_tempDir, "EndpointSecurity");
        Directory.CreateDirectory(folder);

        var e1 = new EndpointSecurityExport { Intent = new DeviceManagementIntent { Id = "e1", DisplayName = "E1" } };
        var e2 = new EndpointSecurityExport { Intent = new DeviceManagementIntent { Id = "e2", DisplayName = "E2" } };
        await File.WriteAllTextAsync(Path.Combine(folder, "e1.json"), System.Text.Json.JsonSerializer.Serialize(e1));
        await File.WriteAllTextAsync(Path.Combine(folder, "e2.json"), System.Text.Json.JsonSerializer.Serialize(e2));

        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadEndpointSecurityIntentsFromFolderAsync(_tempDir);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ReadAdministrativeTemplatesFromFolderAsync_ReadsAllJsonFiles()
    {
        var folder = Path.Combine(_tempDir, "AdministrativeTemplates");
        Directory.CreateDirectory(folder);

        var t1 = new AdministrativeTemplateExport { Template = new GroupPolicyConfiguration { Id = "t1", DisplayName = "T1" } };
        var t2 = new AdministrativeTemplateExport { Template = new GroupPolicyConfiguration { Id = "t2", DisplayName = "T2" } };
        await File.WriteAllTextAsync(Path.Combine(folder, "t1.json"), System.Text.Json.JsonSerializer.Serialize(t1));
        await File.WriteAllTextAsync(Path.Combine(folder, "t2.json"), System.Text.Json.JsonSerializer.Serialize(t2));

        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadAdministrativeTemplatesFromFolderAsync(_tempDir);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ReadEnrollmentConfigurationsFromFolderAsync_ReadsAllJsonFiles()
    {
        var folder = Path.Combine(_tempDir, "EnrollmentConfigurations");
        Directory.CreateDirectory(folder);

        var c1 = new DeviceEnrollmentConfiguration { Id = "ec1", DisplayName = "EC1" };
        var c2 = new DeviceEnrollmentConfiguration { Id = "ec2", DisplayName = "EC2" };
        await File.WriteAllTextAsync(Path.Combine(folder, "ec1.json"), System.Text.Json.JsonSerializer.Serialize(c1));
        await File.WriteAllTextAsync(Path.Combine(folder, "ec2.json"), System.Text.Json.JsonSerializer.Serialize(c2));

        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadEnrollmentConfigurationsFromFolderAsync(_tempDir);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ReadAppProtectionPoliciesFromFolderAsync_ReadsAllJsonFiles()
    {
        var folder = Path.Combine(_tempDir, "AppProtectionPolicies");
        Directory.CreateDirectory(folder);

        var p1 = new AndroidManagedAppProtection { Id = "app1", DisplayName = "App1" };
        var p2 = new AndroidManagedAppProtection { Id = "app2", DisplayName = "App2" };
        await File.WriteAllTextAsync(Path.Combine(folder, "p1.json"), System.Text.Json.JsonSerializer.Serialize(p1));
        await File.WriteAllTextAsync(Path.Combine(folder, "p2.json"), System.Text.Json.JsonSerializer.Serialize(p2));

        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadAppProtectionPoliciesFromFolderAsync(_tempDir);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ReadManagedDeviceAppConfigurationsFromFolderAsync_ReadsAllJsonFiles()
    {
        var folder = Path.Combine(_tempDir, "ManagedDeviceAppConfigurations");
        Directory.CreateDirectory(folder);

        var c1 = new ManagedDeviceMobileAppConfiguration { Id = "mdac1", DisplayName = "MDAC1" };
        var c2 = new ManagedDeviceMobileAppConfiguration { Id = "mdac2", DisplayName = "MDAC2" };
        await File.WriteAllTextAsync(Path.Combine(folder, "c1.json"), System.Text.Json.JsonSerializer.Serialize(c1));
        await File.WriteAllTextAsync(Path.Combine(folder, "c2.json"), System.Text.Json.JsonSerializer.Serialize(c2));

        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadManagedDeviceAppConfigurationsFromFolderAsync(_tempDir);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ReadTargetedManagedAppConfigurationsFromFolderAsync_ReadsAllJsonFiles()
    {
        var folder = Path.Combine(_tempDir, "TargetedManagedAppConfigurations");
        Directory.CreateDirectory(folder);

        var c1 = new TargetedManagedAppConfiguration { Id = "tmac1", DisplayName = "TMAC1" };
        var c2 = new TargetedManagedAppConfiguration { Id = "tmac2", DisplayName = "TMAC2" };
        await File.WriteAllTextAsync(Path.Combine(folder, "c1.json"), System.Text.Json.JsonSerializer.Serialize(c1));
        await File.WriteAllTextAsync(Path.Combine(folder, "c2.json"), System.Text.Json.JsonSerializer.Serialize(c2));

        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadTargetedManagedAppConfigurationsFromFolderAsync(_tempDir);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ReadTermsAndConditionsFromFolderAsync_ReadsAllJsonFiles()
    {
        var folder = Path.Combine(_tempDir, "TermsAndConditions");
        Directory.CreateDirectory(folder);

        var t1 = new TermsAndConditions { Id = "tnc1", DisplayName = "TNC1" };
        var t2 = new TermsAndConditions { Id = "tnc2", DisplayName = "TNC2" };
        await File.WriteAllTextAsync(Path.Combine(folder, "t1.json"), System.Text.Json.JsonSerializer.Serialize(t1));
        await File.WriteAllTextAsync(Path.Combine(folder, "t2.json"), System.Text.Json.JsonSerializer.Serialize(t2));

        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadTermsAndConditionsFromFolderAsync(_tempDir);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ReadScopeTagsFromFolderAsync_ReadsAllJsonFiles()
    {
        var folder = Path.Combine(_tempDir, "ScopeTags");
        Directory.CreateDirectory(folder);

        var s1 = new RoleScopeTag { Id = "st1", DisplayName = "ST1" };
        var s2 = new RoleScopeTag { Id = "st2", DisplayName = "ST2" };
        await File.WriteAllTextAsync(Path.Combine(folder, "s1.json"), System.Text.Json.JsonSerializer.Serialize(s1));
        await File.WriteAllTextAsync(Path.Combine(folder, "s2.json"), System.Text.Json.JsonSerializer.Serialize(s2));

        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadScopeTagsFromFolderAsync(_tempDir);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ReadRoleDefinitionsFromFolderAsync_ReadsAllJsonFiles()
    {
        var folder = Path.Combine(_tempDir, "RoleDefinitions");
        Directory.CreateDirectory(folder);

        var r1 = new RoleDefinition { Id = "rd1", DisplayName = "RD1" };
        var r2 = new RoleDefinition { Id = "rd2", DisplayName = "RD2" };
        await File.WriteAllTextAsync(Path.Combine(folder, "r1.json"), System.Text.Json.JsonSerializer.Serialize(r1));
        await File.WriteAllTextAsync(Path.Combine(folder, "r2.json"), System.Text.Json.JsonSerializer.Serialize(r2));

        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadRoleDefinitionsFromFolderAsync(_tempDir);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ReadIntuneBrandingProfilesFromFolderAsync_ReadsAllJsonFiles()
    {
        var folder = Path.Combine(_tempDir, "IntuneBrandingProfiles");
        Directory.CreateDirectory(folder);

        var b1 = new IntuneBrandingProfile { Id = "ibp1", ProfileName = "IBP1" };
        var b2 = new IntuneBrandingProfile { Id = "ibp2", ProfileName = "IBP2" };
        await File.WriteAllTextAsync(Path.Combine(folder, "b1.json"), System.Text.Json.JsonSerializer.Serialize(b1));
        await File.WriteAllTextAsync(Path.Combine(folder, "b2.json"), System.Text.Json.JsonSerializer.Serialize(b2));

        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadIntuneBrandingProfilesFromFolderAsync(_tempDir);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ReadAzureBrandingLocalizationsFromFolderAsync_ReadsAllJsonFiles()
    {
        var folder = Path.Combine(_tempDir, "AzureBrandingLocalizations");
        Directory.CreateDirectory(folder);

        var l1 = new OrganizationalBrandingLocalization { Id = "loc1" };
        var l2 = new OrganizationalBrandingLocalization { Id = "loc2" };
        await File.WriteAllTextAsync(Path.Combine(folder, "l1.json"), System.Text.Json.JsonSerializer.Serialize(l1));
        await File.WriteAllTextAsync(Path.Combine(folder, "l2.json"), System.Text.Json.JsonSerializer.Serialize(l2));

        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadAzureBrandingLocalizationsFromFolderAsync(_tempDir);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ReadAutopilotProfilesFromFolderAsync_ReadsAllJsonFiles()
    {
        var folder = Path.Combine(_tempDir, "AutopilotProfiles");
        Directory.CreateDirectory(folder);

        var p1 = new WindowsAutopilotDeploymentProfile { Id = "ap1", DisplayName = "AP1" };
        var p2 = new WindowsAutopilotDeploymentProfile { Id = "ap2", DisplayName = "AP2" };
        await File.WriteAllTextAsync(Path.Combine(folder, "p1.json"), System.Text.Json.JsonSerializer.Serialize(p1));
        await File.WriteAllTextAsync(Path.Combine(folder, "p2.json"), System.Text.Json.JsonSerializer.Serialize(p2));

        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadAutopilotProfilesFromFolderAsync(_tempDir);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ReadDeviceHealthScriptsFromFolderAsync_ReadsAllJsonFiles()
    {
        var folder = Path.Combine(_tempDir, "DeviceHealthScripts");
        Directory.CreateDirectory(folder);

        var s1 = new DeviceHealthScript { Id = "dhs1", DisplayName = "DHS1" };
        var s2 = new DeviceHealthScript { Id = "dhs2", DisplayName = "DHS2" };
        await File.WriteAllTextAsync(Path.Combine(folder, "s1.json"), System.Text.Json.JsonSerializer.Serialize(s1));
        await File.WriteAllTextAsync(Path.Combine(folder, "s2.json"), System.Text.Json.JsonSerializer.Serialize(s2));

        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadDeviceHealthScriptsFromFolderAsync(_tempDir);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ReadMacCustomAttributesFromFolderAsync_ReadsAllJsonFiles()
    {
        var folder = Path.Combine(_tempDir, "MacCustomAttributes");
        Directory.CreateDirectory(folder);

        var a1 = new DeviceCustomAttributeShellScript { Id = "mca1", DisplayName = "MCA1" };
        var a2 = new DeviceCustomAttributeShellScript { Id = "mca2", DisplayName = "MCA2" };
        await File.WriteAllTextAsync(Path.Combine(folder, "a1.json"), System.Text.Json.JsonSerializer.Serialize(a1));
        await File.WriteAllTextAsync(Path.Combine(folder, "a2.json"), System.Text.Json.JsonSerializer.Serialize(a2));

        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadMacCustomAttributesFromFolderAsync(_tempDir);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ReadFeatureUpdateProfilesFromFolderAsync_ReadsAllJsonFiles()
    {
        var folder = Path.Combine(_tempDir, "FeatureUpdates");
        Directory.CreateDirectory(folder);

        var p1 = new WindowsFeatureUpdateProfile { Id = "fup1", DisplayName = "FUP1" };
        var p2 = new WindowsFeatureUpdateProfile { Id = "fup2", DisplayName = "FUP2" };
        await File.WriteAllTextAsync(Path.Combine(folder, "p1.json"), System.Text.Json.JsonSerializer.Serialize(p1));
        await File.WriteAllTextAsync(Path.Combine(folder, "p2.json"), System.Text.Json.JsonSerializer.Serialize(p2));

        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadFeatureUpdateProfilesFromFolderAsync(_tempDir);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ReadNamedLocationsFromFolderAsync_ReadsAllJsonFiles()
    {
        var folder = Path.Combine(_tempDir, "NamedLocations");
        Directory.CreateDirectory(folder);

        var n1 = new NamedLocation { Id = "nl1" };
        var n2 = new NamedLocation { Id = "nl2" };
        await File.WriteAllTextAsync(Path.Combine(folder, "n1.json"), System.Text.Json.JsonSerializer.Serialize(n1));
        await File.WriteAllTextAsync(Path.Combine(folder, "n2.json"), System.Text.Json.JsonSerializer.Serialize(n2));

        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadNamedLocationsFromFolderAsync(_tempDir);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ReadAuthenticationStrengthPoliciesFromFolderAsync_ReadsAllJsonFiles()
    {
        var folder = Path.Combine(_tempDir, "AuthenticationStrengths");
        Directory.CreateDirectory(folder);

        var p1 = new AuthenticationStrengthPolicy { Id = "asp1", DisplayName = "ASP1" };
        var p2 = new AuthenticationStrengthPolicy { Id = "asp2", DisplayName = "ASP2" };
        await File.WriteAllTextAsync(Path.Combine(folder, "p1.json"), System.Text.Json.JsonSerializer.Serialize(p1));
        await File.WriteAllTextAsync(Path.Combine(folder, "p2.json"), System.Text.Json.JsonSerializer.Serialize(p2));

        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadAuthenticationStrengthPoliciesFromFolderAsync(_tempDir);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ReadAuthenticationContextsFromFolderAsync_ReadsAllJsonFiles()
    {
        var folder = Path.Combine(_tempDir, "AuthenticationContexts");
        Directory.CreateDirectory(folder);

        var c1 = new AuthenticationContextClassReference { Id = "ctx1", DisplayName = "CTX1" };
        var c2 = new AuthenticationContextClassReference { Id = "ctx2", DisplayName = "CTX2" };
        await File.WriteAllTextAsync(Path.Combine(folder, "c1.json"), System.Text.Json.JsonSerializer.Serialize(c1));
        await File.WriteAllTextAsync(Path.Combine(folder, "c2.json"), System.Text.Json.JsonSerializer.Serialize(c2));

        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadAuthenticationContextsFromFolderAsync(_tempDir);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ReadTermsOfUseAgreementsFromFolderAsync_ReadsAllJsonFiles()
    {
        var folder = Path.Combine(_tempDir, "TermsOfUse");
        Directory.CreateDirectory(folder);

        var a1 = new Agreement { Id = "tou1", DisplayName = "TOU1" };
        var a2 = new Agreement { Id = "tou2", DisplayName = "TOU2" };
        await File.WriteAllTextAsync(Path.Combine(folder, "a1.json"), System.Text.Json.JsonSerializer.Serialize(a1));
        await File.WriteAllTextAsync(Path.Combine(folder, "a2.json"), System.Text.Json.JsonSerializer.Serialize(a2));

        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadTermsOfUseAgreementsFromFolderAsync(_tempDir);

        Assert.Equal(2, result.Count);
    }

    // ---------- ImportNamedLocationAsync edge case: null AdditionalData ----------

    [Fact]
    public async Task ImportNamedLocationAsync_NullAdditionalData_UsesUnknownName()
    {
        var namedLocationService = Substitute.For<INamedLocationService>();
        namedLocationService.CreateNamedLocationAsync(
            Arg.Any<NamedLocation>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new NamedLocation { Id = "created-nl", AdditionalData = null }));
        var sut = new ImportService(DefaultCfgSvc(), null, namedLocationService: namedLocationService);
        var table = new MigrationTable();

        var namedLocation = new NamedLocation { Id = "orig-nl" };
        await sut.ImportNamedLocationAsync(namedLocation, table);

        Assert.Single(table.Entries);
        Assert.Equal("Unknown", table.Entries[0].Name);
    }

    [Fact]
    public async Task ReadDeviceManagementScriptsFromFolderAsync_MissingFolder_ReturnsEmpty()
    {
        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadDeviceManagementScriptsFromFolderAsync(_tempDir);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ReadDeviceManagementScriptsFromFolderAsync_ReadsAllJsonFiles()
    {
        var folder = Path.Combine(_tempDir, "DeviceManagementScripts");
        Directory.CreateDirectory(folder);

        var s1 = new DeviceManagementScript { Id = "dms1", DisplayName = "Script1" };
        var s2 = new DeviceManagementScript { Id = "dms2", DisplayName = "Script2" };
        var e1 = new DeviceManagementScriptExport { Script = s1 };
        var e2 = new DeviceManagementScriptExport { Script = s2 };
        await File.WriteAllTextAsync(Path.Combine(folder, "s1.json"), System.Text.Json.JsonSerializer.Serialize(e1));
        await File.WriteAllTextAsync(Path.Combine(folder, "s2.json"), System.Text.Json.JsonSerializer.Serialize(e2));

        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadDeviceManagementScriptsFromFolderAsync(_tempDir);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ImportDeviceManagementScriptAsync_UpdatesMigration()
    {
        DeviceManagementScript? capturedDmsScript = null;
        var scriptService = Substitute.For<IDeviceManagementScriptService>();
        scriptService.CreateDeviceManagementScriptAsync(
            Arg.Do<DeviceManagementScript>(s => capturedDmsScript = s),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new DeviceManagementScript { Id = "new-dms", DisplayName = "Created Script" }));
        var sut = new ImportService(DefaultCfgSvc(), null, deviceManagementScriptService: scriptService);
        var table = new MigrationTable();

        var script = new DeviceManagementScript
        {
            Id = "old-dms",
            DisplayName = "Source Script",
            CreatedDateTime = DateTimeOffset.UtcNow,
            LastModifiedDateTime = DateTimeOffset.UtcNow
        };

        var export = new DeviceManagementScriptExport
        {
            Script = script,
            Assignments = []
        };

        var created = await sut.ImportDeviceManagementScriptAsync(export.Script, table);

        Assert.Equal("new-dms", created.Id);
        Assert.NotNull(capturedDmsScript);
        Assert.Null(capturedDmsScript!.Id);
        Assert.Single(table.Entries);
        Assert.Equal("DeviceManagementScript", table.Entries[0].ObjectType);
        Assert.Equal("old-dms", table.Entries[0].OriginalId);
        Assert.Equal("new-dms", table.Entries[0].NewId);
    }

    [Fact]
    public async Task ReadDeviceShellScriptsFromFolderAsync_MissingFolder_ReturnsEmpty()
    {
        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadDeviceShellScriptsFromFolderAsync(_tempDir);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ReadDeviceShellScriptsFromFolderAsync_ReadsAllJsonFiles()
    {
        var folder = Path.Combine(_tempDir, "DeviceShellScripts");
        Directory.CreateDirectory(folder);

        var s1 = new DeviceShellScript { Id = "dss1", DisplayName = "Shell1" };
        var s2 = new DeviceShellScript { Id = "dss2", DisplayName = "Shell2" };
        var e1 = new DeviceShellScriptExport { Script = s1, Assignments = [] };
        var e2 = new DeviceShellScriptExport { Script = s2, Assignments = [] };
        await File.WriteAllTextAsync(Path.Combine(folder, "s1.json"), System.Text.Json.JsonSerializer.Serialize(e1));
        await File.WriteAllTextAsync(Path.Combine(folder, "s2.json"), System.Text.Json.JsonSerializer.Serialize(e2));

        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadDeviceShellScriptsFromFolderAsync(_tempDir);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ImportDeviceShellScriptAsync_UpdatesMigration()
    {
        DeviceShellScript? capturedDssScript = null;
        var scriptService = Substitute.For<IDeviceShellScriptService>();
        scriptService.CreateDeviceShellScriptAsync(
            Arg.Do<DeviceShellScript>(s => capturedDssScript = s),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new DeviceShellScript { Id = "new-dss", DisplayName = "Created Shell" }));
        var sut = new ImportService(DefaultCfgSvc(), null, deviceShellScriptService: scriptService);
        var table = new MigrationTable();

        var script = new DeviceShellScript
        {
            Id = "old-dss",
            DisplayName = "Source Shell",
            CreatedDateTime = DateTimeOffset.UtcNow,
            LastModifiedDateTime = DateTimeOffset.UtcNow
        };

        var export = new DeviceShellScriptExport
        {
            Script = script,
            Assignments = []
        };

        var created = await sut.ImportDeviceShellScriptAsync(export.Script, table);

        Assert.Equal("new-dss", created.Id);
        Assert.NotNull(capturedDssScript);
        Assert.Null(capturedDssScript!.Id);
        Assert.Single(table.Entries);
        Assert.Equal("DeviceShellScript", table.Entries[0].ObjectType);
        Assert.Equal("old-dss", table.Entries[0].OriginalId);
        Assert.Equal("new-dss", table.Entries[0].NewId);
    }

    [Fact]
    public async Task ReadComplianceScriptsFromFolderAsync_MissingFolder_ReturnsEmpty()
    {
        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadComplianceScriptsFromFolderAsync(_tempDir);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ReadComplianceScriptsFromFolderAsync_ReadsAllJsonFiles()
    {
        var folder = Path.Combine(_tempDir, "ComplianceScripts");
        Directory.CreateDirectory(folder);

        var s1 = new DeviceComplianceScript { Id = "cs1", DisplayName = "CS1" };
        var s2 = new DeviceComplianceScript { Id = "cs2", DisplayName = "CS2" };
        await File.WriteAllTextAsync(Path.Combine(folder, "s1.json"), System.Text.Json.JsonSerializer.Serialize(s1));
        await File.WriteAllTextAsync(Path.Combine(folder, "s2.json"), System.Text.Json.JsonSerializer.Serialize(s2));

        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadComplianceScriptsFromFolderAsync(_tempDir);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ImportQualityUpdateProfileAsync_WithoutService_Throws()
    {
        var sut = new ImportService(DefaultCfgSvc(), null);
        var table = new MigrationTable();
        var profile = new WindowsQualityUpdateProfile { Id = "old", DisplayName = "Old" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ImportQualityUpdateProfileAsync(profile, table));
    }

    [Fact]
    public async Task ImportQualityUpdateProfileAsync_UpdatesMigration()
    {
        var qualityUpdateService = Substitute.For<IQualityUpdateProfileService>();
        qualityUpdateService.CreateQualityUpdateProfileAsync(
            Arg.Any<WindowsQualityUpdateProfile>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new WindowsQualityUpdateProfile { Id = "new-qup", DisplayName = "Created Quality Update" }));

        var sut = new ImportService(DefaultCfgSvc(),
            qualityUpdateProfileService: qualityUpdateService);

        var table = new MigrationTable();
        var profile = new WindowsQualityUpdateProfile
        {
            Id = "original-qup",
            DisplayName = "My Quality Update"
        };

        var created = await sut.ImportQualityUpdateProfileAsync(profile, table);

        Assert.Equal("new-qup", created.Id);
        Assert.Single(table.Entries);
        Assert.Equal("QualityUpdateProfile", table.Entries[0].ObjectType);
        Assert.Equal("original-qup", table.Entries[0].OriginalId);
        Assert.Equal("new-qup", table.Entries[0].NewId);
    }

    [Fact]
    public async Task ReadQualityUpdateProfilesFromFolderAsync_MissingFolder_ReturnsEmpty()
    {
        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadQualityUpdateProfilesFromFolderAsync(_tempDir);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ReadSettingsCatalogPoliciesFromFolderAsync_MissingFolder_ReturnsEmpty()
    {
        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadSettingsCatalogPoliciesFromFolderAsync(_tempDir);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ReadQualityUpdateProfilesFromFolderAsync_ReadsAllJsonFiles()
    {
        var folder = Path.Combine(_tempDir, "QualityUpdates");
        Directory.CreateDirectory(folder);

        var p1 = new WindowsQualityUpdateProfile { Id = "qup1", DisplayName = "QUP1" };
        var p2 = new WindowsQualityUpdateProfile { Id = "qup2", DisplayName = "QUP2" };
        await File.WriteAllTextAsync(Path.Combine(folder, "p1.json"), System.Text.Json.JsonSerializer.Serialize(p1));
        await File.WriteAllTextAsync(Path.Combine(folder, "p2.json"), System.Text.Json.JsonSerializer.Serialize(p2));

        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadQualityUpdateProfilesFromFolderAsync(_tempDir);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ReadSettingsCatalogPoliciesFromFolderAsync_ReadsAllJsonFiles()
    {
        var folder = Path.Combine(_tempDir, "SettingsCatalog");
        Directory.CreateDirectory(folder);

        var export1 = new SettingsCatalogExport { Policy = new DeviceManagementConfigurationPolicy { Id = "sc1", Name = "Policy1" } };
        var export2 = new SettingsCatalogExport { Policy = new DeviceManagementConfigurationPolicy { Id = "sc2", Name = "Policy2" } };
        await File.WriteAllTextAsync(Path.Combine(folder, "p1.json"), System.Text.Json.JsonSerializer.Serialize(export1, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }));
        await File.WriteAllTextAsync(Path.Combine(folder, "p2.json"), System.Text.Json.JsonSerializer.Serialize(export2, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }));

        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadSettingsCatalogPoliciesFromFolderAsync(_tempDir);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ImportDriverUpdateProfileAsync_WithoutService_Throws()
    {
        var sut = new ImportService(DefaultCfgSvc(), null);
        var table = new MigrationTable();
        var profile = new WindowsDriverUpdateProfile { Id = "old", DisplayName = "Old" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ImportDriverUpdateProfileAsync(profile, table));
    }

    [Fact]
    public async Task ImportDriverUpdateProfileAsync_UpdatesMigration()
    {
        var driverUpdateService = Substitute.For<IDriverUpdateProfileService>();
        driverUpdateService.CreateDriverUpdateProfileAsync(
            Arg.Any<WindowsDriverUpdateProfile>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new WindowsDriverUpdateProfile { Id = "new-dup", DisplayName = "Created Driver Update" }));

        var sut = new ImportService(DefaultCfgSvc(),
            driverUpdateProfileService: driverUpdateService);

        var table = new MigrationTable();
        var profile = new WindowsDriverUpdateProfile
        {
            Id = "original-dup",
            DisplayName = "My Driver Update"
        };

        var created = await sut.ImportDriverUpdateProfileAsync(profile, table);

        Assert.Equal("new-dup", created.Id);
        Assert.Single(table.Entries);
        Assert.Equal("DriverUpdateProfile", table.Entries[0].ObjectType);
        Assert.Equal("original-dup", table.Entries[0].OriginalId);
        Assert.Equal("new-dup", table.Entries[0].NewId);
    }

    [Fact]
    public async Task ReadDriverUpdateProfilesFromFolderAsync_MissingFolder_ReturnsEmpty()
    {
        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadDriverUpdateProfilesFromFolderAsync(_tempDir);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ReadDriverUpdateProfilesFromFolderAsync_ReadsAllJsonFiles()
    {
        var folder = Path.Combine(_tempDir, "DriverUpdates");
        Directory.CreateDirectory(folder);

        var p1 = new WindowsDriverUpdateProfile { Id = "dup1", DisplayName = "DUP1" };
        var p2 = new WindowsDriverUpdateProfile { Id = "dup2", DisplayName = "DUP2" };
        await File.WriteAllTextAsync(Path.Combine(folder, "p1.json"), System.Text.Json.JsonSerializer.Serialize(p1));
        await File.WriteAllTextAsync(Path.Combine(folder, "p2.json"), System.Text.Json.JsonSerializer.Serialize(p2));

        var sut = new ImportService(DefaultCfgSvc());
        var result = await sut.ReadDriverUpdateProfilesFromFolderAsync(_tempDir);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ImportSettingsCatalogPolicyAsync_WithoutService_Throws()
    {
        var sut = new ImportService(DefaultCfgSvc());
        var export = new SettingsCatalogExport { Policy = new DeviceManagementConfigurationPolicy { Id = "sc-id", Name = "Policy" } };
        var table = new MigrationTable();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ImportSettingsCatalogPolicyAsync(export, table));
    }

    [Fact]
    public async Task ImportSettingsCatalogPolicyAsync_UpdatesMigration()
    {
        DeviceManagementConfigurationPolicy? capturedScPolicy = null;
        var stubSvc = Substitute.For<ISettingsCatalogService>();
        stubSvc.CreateSettingsCatalogPolicyAsync(
            Arg.Do<DeviceManagementConfigurationPolicy>(p => capturedScPolicy = p),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new DeviceManagementConfigurationPolicy { Id = "created-sc", Name = "Created" }));
        stubSvc.AssignSettingsCatalogPolicyAsync(
            Arg.Any<string>(), Arg.Any<List<DeviceManagementConfigurationPolicyAssignment>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var sut = new ImportService(DefaultCfgSvc(), settingsCatalogService: stubSvc);
        var export = new SettingsCatalogExport
        {
            Policy = new DeviceManagementConfigurationPolicy { Id = "orig-id", Name = "My Policy" }
        };
        var table = new MigrationTable();

        var created = await sut.ImportSettingsCatalogPolicyAsync(export, table);

        Assert.Equal("created-sc", created.Id);
        Assert.Null(capturedScPolicy!.Id);
        Assert.Contains(table.Entries, e => e.ObjectType == "SettingsCatalog" && e.OriginalId == "orig-id" && e.NewId == "created-sc");
    }

    [Fact]
    public async Task ImportSettingsCatalogPolicyAsync_WithAssignments_CallsAssign()
    {
        string? lastAssignedPolicyId = null;
        List<DeviceManagementConfigurationPolicyAssignment>? lastAssignments = null;
        var stubSvc = Substitute.For<ISettingsCatalogService>();
        stubSvc.CreateSettingsCatalogPolicyAsync(
            Arg.Any<DeviceManagementConfigurationPolicy>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new DeviceManagementConfigurationPolicy { Id = "created-sc", Name = "Created" }));
        stubSvc.AssignSettingsCatalogPolicyAsync(
            Arg.Do<string>(id => lastAssignedPolicyId = id),
            Arg.Do<List<DeviceManagementConfigurationPolicyAssignment>>(a => lastAssignments = a),
            Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var sut = new ImportService(DefaultCfgSvc(), settingsCatalogService: stubSvc);
        var export = new SettingsCatalogExport
        {
            Policy = new DeviceManagementConfigurationPolicy { Id = "orig-id", Name = "My Policy" },
            Assignments = [new DeviceManagementConfigurationPolicyAssignment { Id = "assign-1" }]
        };
        var table = new MigrationTable();

        await sut.ImportSettingsCatalogPolicyAsync(export, table);

        Assert.Equal("created-sc", lastAssignedPolicyId);
        Assert.NotNull(lastAssignments);
        Assert.Null(lastAssignments![0].Id);
    }

    private static IConfigurationProfileService DefaultCfgSvc()
    {
        var svc = Substitute.For<IConfigurationProfileService>();
        svc.CreateDeviceConfigurationAsync(Arg.Any<DeviceConfiguration>(), Arg.Any<CancellationToken>())
           .Returns(Task.FromResult(new DeviceConfiguration { Id = "created", DisplayName = "Created" }));
        return svc;
    }

    private static ICompliancePolicyService DefaultComplianceSvc()
    {
        var svc = Substitute.For<ICompliancePolicyService>();
        svc.CreateCompliancePolicyAsync(Arg.Any<DeviceCompliancePolicy>(), Arg.Any<CancellationToken>())
           .Returns(Task.FromResult(new DeviceCompliancePolicy { Id = "created-policy", DisplayName = "Created" }));
        svc.AssignPolicyAsync(Arg.Any<string>(), Arg.Any<List<DeviceCompliancePolicyAssignment>>(), Arg.Any<CancellationToken>())
           .Returns(Task.CompletedTask);
        return svc;
    }
}

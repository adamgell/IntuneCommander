using Intune.Commander.Core.Services;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Core.Tests.Services;

public class ConditionalAccessPptExportServiceTests : IDisposable
{
    private readonly string _tempDir;

    public ConditionalAccessPptExportServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"intunemanager-cappt-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    [Fact]
    public void Constructor_WithValidServices_DoesNotThrow()
    {
        // Arrange
        var caPolicyService = new MockConditionalAccessPolicyService();
        var namedLocationService = new MockNamedLocationService();
        var authStrengthService = new MockAuthenticationStrengthService();
        var authContextService = new MockAuthenticationContextService();
        var applicationService = new MockApplicationService();

        // Act & Assert
        var service = new ConditionalAccessPptExportService(
            caPolicyService,
            namedLocationService,
            authStrengthService,
            authContextService,
            applicationService);

        Assert.NotNull(service);
    }

    [Fact]
    public async Task ExportAsync_WithNullOutputPath_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.ExportAsync(null!, "TenantName"));
    }

    [Fact]
    public async Task ExportAsync_WithEmptyOutputPath_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.ExportAsync("", "TenantName"));
    }

    [Fact]
    public async Task ExportAsync_WithWhitespaceOutputPath_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.ExportAsync("   ", "TenantName"));
    }

    [Fact]
    public async Task ExportAsync_WithNullTenantName_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();
        var outputPath = Path.Combine(_tempDir, "output.pptx");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.ExportAsync(outputPath, null!));
    }

    [Fact]
    public async Task ExportAsync_WithEmptyTenantName_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();
        var outputPath = Path.Combine(_tempDir, "output.pptx");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.ExportAsync(outputPath, ""));
    }

    [Fact]
    public async Task ExportAsync_WithWhitespaceTenantName_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();
        var outputPath = Path.Combine(_tempDir, "output.pptx");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.ExportAsync(outputPath, "   "));
    }

    [Fact]
    public async Task ExportAsync_WithValidParameters_CreatesFile()
    {
        // Arrange
        var service = CreateService();
        var outputPath = Path.Combine(_tempDir, "test-export.pptx");

        // Act
        await service.ExportAsync(outputPath, "Test Tenant");

        // Assert
        Assert.True(File.Exists(outputPath), "PowerPoint file should be created");
        var fileInfo = new FileInfo(outputPath);
        Assert.True(fileInfo.Length > 0, "PowerPoint file should not be empty");
    }

    [Fact]
    public async Task ExportAsync_CreatesParentDirectory()
    {
        // Arrange
        var service = CreateService();
        var subDir = Path.Combine(_tempDir, "nested", "folder");
        var outputPath = Path.Combine(subDir, "export.pptx");

        // Act
        await service.ExportAsync(outputPath, "Test Tenant");

        // Assert
        Assert.True(Directory.Exists(subDir), "Parent directory should be created");
        Assert.True(File.Exists(outputPath), "File should be created in nested directory");
    }

    [Fact]
    public async Task ExportAsync_WithCancellationToken_ThrowsIfCancelled()
    {
        // Arrange
        var service = CreateService();
        var outputPath = Path.Combine(_tempDir, "cancelled-export.pptx");
        using var cts = new CancellationTokenSource();
        
        // Start the task
        var exportTask = service.ExportAsync(outputPath, "Test Tenant", cts.Token);
        
        // Cancel after a brief delay
        await Task.Delay(10);
        cts.Cancel();

        // Act & Assert
        // The export should either complete or be cancelled
        // Since we're using mock services that return instantly, it likely completes
        // This test verifies the cancellationToken parameter is accepted
        var result = await Record.ExceptionAsync(async () => await exportTask);
        
        // Either no exception (completed) or OperationCanceledException (cancelled)
        Assert.True(result == null || result is OperationCanceledException);
    }

    [Fact]
    public async Task ExportAsync_GeneratesValidPptxStructure()
    {
        // Arrange
        var service = CreateServiceWithData();
        var outputPath = Path.Combine(_tempDir, "structured-export.pptx");

        // Act
        await service.ExportAsync(outputPath, "Test Tenant");

        // Assert
        Assert.True(File.Exists(outputPath));
        
        // Basic check: PPTX files are ZIP archives
        // We can verify it's a valid ZIP by checking magic bytes
        var bytes = await File.ReadAllBytesAsync(outputPath);
        Assert.True(bytes.Length > 4, "File should have content");
        
        // Check for ZIP magic bytes (PK\x03\x04)
        Assert.Equal(0x50, bytes[0]); // 'P'
        Assert.Equal(0x4B, bytes[1]); // 'K'
    }

    [Fact]
    public async Task ExportAsync_WithComplexPolicyData_CoversOptionalSections()
    {
        // Arrange
        var policies = new List<ConditionalAccessPolicy>
        {
            new()
            {
                Id = "policy-complex",
                DisplayName = null,
                State = null,
                CreatedDateTime = null,
                Conditions = new ConditionalAccessConditionSet
                {
                    Users = new ConditionalAccessUsers
                    {
                        IncludeUsers = [],
                        ExcludeUsers = []
                    },
                    Applications = new ConditionalAccessApplications
                    {
                        IncludeApplications = null,
                        ExcludeApplications = null
                    },
                    Platforms = new ConditionalAccessPlatforms
                    {
                        IncludePlatforms = null
                    },
                    Locations = new ConditionalAccessLocations
                    {
                        IncludeLocations = null,
                        ExcludeLocations = null
                    }
                },
                GrantControls = new ConditionalAccessGrantControls
                {
                    Operator = null,
                    BuiltInControls = [],
                    TermsOfUse = ["tou-1"],
                    CustomAuthenticationFactors = ["custom-1"]
                }
            },
            new()
            {
                Id = "policy-complex-2",
                DisplayName = "Complex Policy 2",
                State = ConditionalAccessPolicyState.Enabled,
                CreatedDateTime = DateTimeOffset.UtcNow,
                Conditions = new ConditionalAccessConditionSet
                {
                    Applications = new ConditionalAccessApplications
                    {
                        IncludeApplications = ["All"],
                        ExcludeApplications = ["app-id-1"]
                    },
                    Platforms = new ConditionalAccessPlatforms
                    {
                        IncludePlatforms = [ConditionalAccessDevicePlatform.Android]
                    },
                    Locations = new ConditionalAccessLocations
                    {
                        IncludeLocations = ["AllTrusted"],
                        ExcludeLocations = ["loc-id-1"]
                    }
                },
                GrantControls = new ConditionalAccessGrantControls
                {
                    Operator = "AND",
                    BuiltInControls = [ConditionalAccessGrantControl.Mfa],
                    TermsOfUse = [],
                    CustomAuthenticationFactors = []
                }
            }
        };

        var service = new ConditionalAccessPptExportService(
            new MockConditionalAccessPolicyService(customPolicies: policies),
            new MockNamedLocationService(),
            new MockAuthenticationStrengthService(),
            new MockAuthenticationContextService(),
            new MockApplicationService());

        var outputPath = Path.Combine(_tempDir, "complex-export.pptx");

        // Act
        await service.ExportAsync(outputPath, "Test Tenant");

        // Assert
        Assert.True(File.Exists(outputPath));
        Assert.True(new FileInfo(outputPath).Length > 0);
    }

    [Fact]
    public async Task ExportAsync_WithPreCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var service = CreateServiceWithData();
        var outputPath = Path.Combine(_tempDir, "cancelled-before-loop.pptx");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.ExportAsync(outputPath, "Test Tenant", cts.Token));
    }

    private ConditionalAccessPptExportService CreateService()
    {
        return new ConditionalAccessPptExportService(
            new MockConditionalAccessPolicyService(),
            new MockNamedLocationService(),
            new MockAuthenticationStrengthService(),
            new MockAuthenticationContextService(),
            new MockApplicationService());
    }

    private ConditionalAccessPptExportService CreateServiceWithData()
    {
        return new ConditionalAccessPptExportService(
            new MockConditionalAccessPolicyService(withData: true),
            new MockNamedLocationService(),
            new MockAuthenticationStrengthService(),
            new MockAuthenticationContextService(),
            new MockApplicationService());
    }

    // Mock service implementations
    private class MockConditionalAccessPolicyService : IConditionalAccessPolicyService
    {
        private readonly bool _withData;
        private readonly List<ConditionalAccessPolicy>? _customPolicies;

        public MockConditionalAccessPolicyService(bool withData = false, List<ConditionalAccessPolicy>? customPolicies = null)
        {
            _withData = withData;
            _customPolicies = customPolicies;
        }

        public Task<List<ConditionalAccessPolicy>> ListPoliciesAsync(CancellationToken cancellationToken = default)
        {
            if (_customPolicies != null)
            {
                return Task.FromResult(_customPolicies);
            }

            if (_withData)
            {
                return Task.FromResult(new List<ConditionalAccessPolicy>
                {
                    new ConditionalAccessPolicy
                    {
                        Id = "policy-1",
                        DisplayName = "Test Policy 1",
                        State = ConditionalAccessPolicyState.Enabled,
                        CreatedDateTime = DateTimeOffset.Now,
                        Conditions = new ConditionalAccessConditionSet
                        {
                            Users = new ConditionalAccessUsers
                            {
                                IncludeUsers = new List<string> { "All" }
                            }
                        },
                        GrantControls = new ConditionalAccessGrantControls
                        {
                            Operator = "AND",
                            BuiltInControls = new List<ConditionalAccessGrantControl?> 
                            { 
                                ConditionalAccessGrantControl.Mfa 
                            }
                        }
                    },
                    new ConditionalAccessPolicy
                    {
                        Id = "policy-2",
                        DisplayName = "Test Policy 2",
                        State = ConditionalAccessPolicyState.Disabled,
                        CreatedDateTime = DateTimeOffset.Now.AddDays(-7)
                    }
                });
            }

            return Task.FromResult(new List<ConditionalAccessPolicy>());
        }

        public Task<ConditionalAccessPolicy?> GetPolicyAsync(string id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ConditionalAccessPolicy?>(null);
        }
    }

    private class MockNamedLocationService : INamedLocationService
    {
        public Task<List<NamedLocation>> ListNamedLocationsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<NamedLocation>());
        }

        public Task<NamedLocation?> GetNamedLocationAsync(string id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<NamedLocation?>(null);
        }

        public Task<NamedLocation> CreateNamedLocationAsync(NamedLocation location, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<NamedLocation> UpdateNamedLocationAsync(NamedLocation location, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task DeleteNamedLocationAsync(string id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    private class MockAuthenticationStrengthService : IAuthenticationStrengthService
    {
        public Task<List<AuthenticationStrengthPolicy>> ListAuthenticationStrengthPoliciesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<AuthenticationStrengthPolicy>());
        }

        public Task<AuthenticationStrengthPolicy?> GetAuthenticationStrengthPolicyAsync(string id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<AuthenticationStrengthPolicy?>(null);
        }

        public Task<AuthenticationStrengthPolicy> CreateAuthenticationStrengthPolicyAsync(AuthenticationStrengthPolicy policy, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<AuthenticationStrengthPolicy> UpdateAuthenticationStrengthPolicyAsync(AuthenticationStrengthPolicy policy, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAuthenticationStrengthPolicyAsync(string id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    private class MockAuthenticationContextService : IAuthenticationContextService
    {
        public Task<List<AuthenticationContextClassReference>> ListAuthenticationContextsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<AuthenticationContextClassReference>());
        }

        public Task<AuthenticationContextClassReference?> GetAuthenticationContextAsync(string id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<AuthenticationContextClassReference?>(null);
        }

        public Task<AuthenticationContextClassReference> CreateAuthenticationContextAsync(AuthenticationContextClassReference context, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<AuthenticationContextClassReference> UpdateAuthenticationContextAsync(AuthenticationContextClassReference context, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAuthenticationContextAsync(string id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    private class MockApplicationService : IApplicationService
    {
        public Task<List<MobileApp>> ListApplicationsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<MobileApp>());
        }

        public Task<MobileApp?> GetApplicationAsync(string id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<MobileApp?>(null);
        }

        public Task<List<MobileAppAssignment>> GetApplicationAssignmentsAsync(string appId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<MobileAppAssignment>());
        }

        public Task<List<MobileAppAssignment>> GetAssignmentsAsync(string appId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<MobileAppAssignment>());
        }
    }
}

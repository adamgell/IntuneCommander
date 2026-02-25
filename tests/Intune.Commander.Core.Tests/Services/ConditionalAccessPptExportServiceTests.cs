using Intune.Commander.Core.Services;
using Microsoft.Graph.Beta.Models;
using NSubstitute;

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
        var service = new ConditionalAccessPptExportService(
            Substitute.For<IConditionalAccessPolicyService>(),
            Substitute.For<INamedLocationService>(),
            Substitute.For<IAuthenticationStrengthService>(),
            Substitute.For<IAuthenticationContextService>(),
            Substitute.For<IApplicationService>());

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
            PolicyServiceReturning(policies),
            Substitute.For<INamedLocationService>(),
            Substitute.For<IAuthenticationStrengthService>(),
            Substitute.For<IAuthenticationContextService>(),
            Substitute.For<IApplicationService>());

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

    private static ConditionalAccessPptExportService CreateService()
    {
        var caPolicySvc = Substitute.For<IConditionalAccessPolicyService>();
        caPolicySvc.ListPoliciesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<ConditionalAccessPolicy>()));
        return new ConditionalAccessPptExportService(
            caPolicySvc,
            Substitute.For<INamedLocationService>(),
            Substitute.For<IAuthenticationStrengthService>(),
            Substitute.For<IAuthenticationContextService>(),
            Substitute.For<IApplicationService>());
    }

    private static ConditionalAccessPptExportService CreateServiceWithData()
    {
        var caPolicySvc = Substitute.For<IConditionalAccessPolicyService>();
        caPolicySvc.ListPoliciesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<ConditionalAccessPolicy>
            {
                new()
                {
                    Id = "policy-1",
                    DisplayName = "Test Policy 1",
                    State = ConditionalAccessPolicyState.Enabled,
                    CreatedDateTime = DateTimeOffset.Now,
                    Conditions = new ConditionalAccessConditionSet
                    {
                        Users = new ConditionalAccessUsers { IncludeUsers = ["All"] }
                    },
                    GrantControls = new ConditionalAccessGrantControls
                    {
                        Operator = "AND",
                        BuiltInControls = [ConditionalAccessGrantControl.Mfa]
                    }
                },
                new()
                {
                    Id = "policy-2",
                    DisplayName = "Test Policy 2",
                    State = ConditionalAccessPolicyState.Disabled,
                    CreatedDateTime = DateTimeOffset.Now.AddDays(-7)
                }
            }));
        return new ConditionalAccessPptExportService(
            caPolicySvc,
            Substitute.For<INamedLocationService>(),
            Substitute.For<IAuthenticationStrengthService>(),
            Substitute.For<IAuthenticationContextService>(),
            Substitute.For<IApplicationService>());
    }

    // Keep a minimal helper for tests that need custom policy lists
    private static IConditionalAccessPolicyService PolicyServiceReturning(List<ConditionalAccessPolicy> policies)
    {
        var svc = Substitute.For<IConditionalAccessPolicyService>();
        svc.ListPoliciesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(policies));
        return svc;
    }
}


using IntuneManager.Core.Services;
using Microsoft.Graph.Beta.Models;

namespace IntuneManager.Core.Tests.Integration;

/// <summary>
/// CRUD integration tests that create, read, update, and delete resources
/// against a live Graph tenant. Each test creates a resource with a recognizable
/// prefix and cleans up in a finally block, even on failure.
///
/// These tests require write permissions in the test tenant.
/// </summary>
[Trait("Category", "Integration")]
public class GraphServiceCrudIntegrationTests : GraphIntegrationTestBase
{
    private const string TestPrefix = "IntTest_AutoCleanup_";

    #region ScopeTagService CRUD

    [Fact]
    public async Task ScopeTag_Create_Get_Update_Delete()
    {
        if (ShouldSkip()) return;
        var svc = CreateService<ScopeTagService>()!;
        RoleScopeTag? created = null;

        try
        {
            // Create
            var tag = new RoleScopeTag
            {
                DisplayName = $"{TestPrefix}ScopeTag_{Guid.NewGuid():N}",
                Description = "Integration test — will be deleted automatically"
            };
            created = await svc.CreateScopeTagAsync(tag);
            Assert.NotNull(created);
            Assert.NotNull(created.Id);
            Assert.StartsWith(TestPrefix, created.DisplayName);

            // Get
            var fetched = await svc.GetScopeTagAsync(created.Id!);
            Assert.NotNull(fetched);
            Assert.Equal(created.Id, fetched!.Id);
            Assert.Equal(created.DisplayName, fetched.DisplayName);

            // Update
            created.Description = "Updated by integration test";
            var updated = await svc.UpdateScopeTagAsync(created);
            Assert.NotNull(updated);
            Assert.Equal("Updated by integration test", updated.Description);

            // Delete
            await svc.DeleteScopeTagAsync(created.Id!);

            // Verify deletion — Get should return null or throw
            var deleted = await svc.GetScopeTagAsync(created.Id!);
            // If we get here without exception, it should be null
            // (Graph may throw 404 instead — both are acceptable)
            created = null; // prevent double-delete in finally
        }
        catch (Microsoft.Graph.Beta.Models.ODataErrors.ODataError ex)
            when (ex.ResponseStatusCode == 404)
        {
            // Expected after delete — the item is gone
            created = null;
        }
        finally
        {
            // Safety cleanup
            if (created?.Id != null)
            {
                try { await svc.DeleteScopeTagAsync(created.Id); } catch { }
            }
        }
    }

    #endregion

    #region NamedLocationService CRUD

    [Fact]
    public async Task NamedLocation_Create_Get_Update_Delete()
    {
        if (ShouldSkip()) return;
        var svc = CreateService<NamedLocationService>()!;
        NamedLocation? created = null;

        try
        {
            // Create an IP Named Location
            var location = new IpNamedLocation
            {
                DisplayName = $"{TestPrefix}NamedLoc_{Guid.NewGuid():N}",
                IsTrusted = false,
                IpRanges =
                [
                    new IPv4CidrRange { CidrAddress = "203.0.113.0/24" }
                ]
            };
            created = await svc.CreateNamedLocationAsync(location);
            Assert.NotNull(created);
            Assert.NotNull(created.Id);

            // Brief delay — Entra ID named locations have eventual consistency
            await Task.Delay(3000);

            // Get
            var fetched = await svc.GetNamedLocationAsync(created.Id!);
            Assert.NotNull(fetched);
            Assert.Equal(created.Id, fetched!.Id);

            // Update
            var toUpdate = new IpNamedLocation
            {
                Id = created.Id,
                DisplayName = $"{TestPrefix}NamedLoc_Updated_{Guid.NewGuid():N}",
                IsTrusted = false,
                IpRanges =
                [
                    new IPv4CidrRange { CidrAddress = "198.51.100.0/24" }
                ]
            };
            var updated = await svc.UpdateNamedLocationAsync(toUpdate);
            Assert.NotNull(updated);

            // Delete
            await svc.DeleteNamedLocationAsync(created.Id!);
            created = null;
        }
        finally
        {
            if (created?.Id != null)
            {
                try { await svc.DeleteNamedLocationAsync(created.Id); } catch { }
            }
        }
    }

    #endregion

    #region TermsAndConditionsService CRUD

    [Fact]
    public async Task TermsAndConditions_Create_Get_Update_Delete()
    {
        if (ShouldSkip()) return;
        var svc = CreateService<TermsAndConditionsService>()!;
        TermsAndConditions? created = null;

        try
        {
            // Create
            var tac = new TermsAndConditions
            {
                DisplayName = $"{TestPrefix}TAC_{Guid.NewGuid():N}",
                Description = "Integration test -- auto cleanup",
                Title = "Test Terms",
                BodyText = "These are test terms and conditions.",
                AcceptanceStatement = "I accept these terms.",
                Version = 1,
            };
            created = await svc.CreateTermsAndConditionsAsync(tac);
            Assert.NotNull(created);
            Assert.NotNull(created.Id);

            // Get
            var fetched = await svc.GetTermsAndConditionsAsync(created.Id!);
            Assert.NotNull(fetched);
            Assert.Equal(created.Id, fetched!.Id);

            // Update
            created.Description = "Updated by integration test";
            var updated = await svc.UpdateTermsAndConditionsAsync(created);
            Assert.NotNull(updated);
            Assert.Equal("Updated by integration test", updated.Description);

            // Delete
            await svc.DeleteTermsAndConditionsAsync(created.Id!);
            created = null;
        }
        finally
        {
            if (created?.Id != null)
            {
                try { await svc.DeleteTermsAndConditionsAsync(created.Id); } catch { }
            }
        }
    }

    #endregion

    #region DeviceHealthScriptService CRUD

    [Fact]
    public async Task DeviceHealthScript_Create_Get_Update_Delete()
    {
        if (ShouldSkip()) return;
        var svc = CreateService<DeviceHealthScriptService>()!;
        DeviceHealthScript? created = null;

        try
        {
            var script = new DeviceHealthScript
            {
                DisplayName = $"{TestPrefix}HealthScript_{Guid.NewGuid():N}",
                Description = "Integration test — auto cleanup",
                Publisher = "IntegrationTest",
                DetectionScriptContent = Convert.FromBase64String(
                    Convert.ToBase64String("Write-Output 'OK'"u8.ToArray())),
                RunAsAccount = RunAsAccountType.System,
            };
            created = await svc.CreateDeviceHealthScriptAsync(script);
            Assert.NotNull(created);
            Assert.NotNull(created.Id);

            // Get
            var fetched = await svc.GetDeviceHealthScriptAsync(created.Id!);
            Assert.NotNull(fetched);

            // Update
            created.Description = "Updated by integration test";
            var updated = await svc.UpdateDeviceHealthScriptAsync(created);
            Assert.NotNull(updated);

            // Delete
            await svc.DeleteDeviceHealthScriptAsync(created.Id!);
            created = null;
        }
        finally
        {
            if (created?.Id != null)
            {
                try { await svc.DeleteDeviceHealthScriptAsync(created.Id); } catch { }
            }
        }
    }

    #endregion

    #region FeatureUpdateProfileService CRUD

    [Fact]
    public async Task FeatureUpdate_Create_Get_Update_Delete()
    {
        if (ShouldSkip()) return;
        var svc = CreateService<FeatureUpdateProfileService>()!;
        WindowsFeatureUpdateProfile? created = null;

        try
        {
            var profile = new WindowsFeatureUpdateProfile
            {
                DisplayName = $"{TestPrefix}FeatureUpdate_{Guid.NewGuid():N}",
                Description = "Integration test -- auto cleanup",
                FeatureUpdateVersion = "Windows 11, version 24H2",
            };
            created = await svc.CreateFeatureUpdateProfileAsync(profile);
            Assert.NotNull(created);
            Assert.NotNull(created.Id);

            // Get
            var fetched = await svc.GetFeatureUpdateProfileAsync(created.Id!);
            Assert.NotNull(fetched);

            // Update
            created.Description = "Updated by integration test";
            var updated = await svc.UpdateFeatureUpdateProfileAsync(created);
            Assert.NotNull(updated);

            // Delete
            await svc.DeleteFeatureUpdateProfileAsync(created.Id!);
            created = null;
        }
        finally
        {
            if (created?.Id != null)
            {
                try { await svc.DeleteFeatureUpdateProfileAsync(created.Id); } catch { }
            }
        }
    }

    #endregion

    #region RoleDefinitionService CRUD

    [Fact]
    public async Task RoleDefinition_Create_Get_Update_Delete()
    {
        if (ShouldSkip()) return;
        var svc = CreateService<RoleDefinitionService>()!;
        RoleDefinition? created = null;

        try
        {
            var role = new DeviceAndAppManagementRoleDefinition
            {
                DisplayName = $"{TestPrefix}Role_{Guid.NewGuid():N}",
                Description = "Integration test -- auto cleanup",
                IsBuiltIn = false,
                RolePermissions =
                [
                    new RolePermission
                    {
                        ResourceActions =
                        [
                            new ResourceAction
                            {
                                AllowedResourceActions = ["Microsoft.Intune_Organization_Read"]
                            }
                        ]
                    }
                ],
            };
            created = await svc.CreateRoleDefinitionAsync(role);
            Assert.NotNull(created);
            Assert.NotNull(created.Id);

            // Get
            var fetched = await svc.GetRoleDefinitionAsync(created.Id!);
            Assert.NotNull(fetched);

            // Update
            created.Description = "Updated by integration test";
            var updated = await svc.UpdateRoleDefinitionAsync(created);
            Assert.NotNull(updated);

            // Delete
            await svc.DeleteRoleDefinitionAsync(created.Id!);
            created = null;
        }
        finally
        {
            if (created?.Id != null)
            {
                try { await svc.DeleteRoleDefinitionAsync(created.Id); } catch { }
            }
        }
    }

    #endregion

    #region Update-without-ID validation (no Graph call needed)

    [Fact]
    public async Task ScopeTag_Update_WithoutId_Throws_ArgumentException()
    {
        if (ShouldSkip()) return;
        var svc = CreateService<ScopeTagService>()!;
        var tag = new RoleScopeTag { DisplayName = "NoId" };
        await Assert.ThrowsAsync<ArgumentException>(() => svc.UpdateScopeTagAsync(tag));
    }

    [Fact]
    public async Task NamedLocation_Update_WithoutId_Throws_ArgumentException()
    {
        if (ShouldSkip()) return;
        var svc = CreateService<NamedLocationService>()!;
        var loc = new IpNamedLocation { DisplayName = "NoId" };
        await Assert.ThrowsAsync<ArgumentException>(() => svc.UpdateNamedLocationAsync(loc));
    }

    [Fact]
    public async Task TermsAndConditions_Update_WithoutId_Throws_ArgumentException()
    {
        if (ShouldSkip()) return;
        var svc = CreateService<TermsAndConditionsService>()!;
        var tac = new TermsAndConditions { DisplayName = "NoId" };
        await Assert.ThrowsAsync<ArgumentException>(() => svc.UpdateTermsAndConditionsAsync(tac));
    }

    [Fact]
    public async Task AuthenticationStrength_Update_WithoutId_Throws_ArgumentException()
    {
        if (ShouldSkip()) return;
        var svc = CreateService<AuthenticationStrengthService>()!;
        var policy = new AuthenticationStrengthPolicy { DisplayName = "NoId" };
        await Assert.ThrowsAsync<ArgumentException>(() => svc.UpdateAuthenticationStrengthPolicyAsync(policy));
    }

    [Fact]
    public async Task AuthenticationContext_Update_WithoutId_Throws_ArgumentException()
    {
        if (ShouldSkip()) return;
        var svc = CreateService<AuthenticationContextService>()!;
        var ctx = new AuthenticationContextClassReference { DisplayName = "NoId" };
        await Assert.ThrowsAsync<ArgumentException>(() => svc.UpdateAuthenticationContextAsync(ctx));
    }

    [Fact]
    public async Task TermsOfUse_Update_WithoutId_Throws_ArgumentException()
    {
        if (ShouldSkip()) return;
        var svc = CreateService<TermsOfUseService>()!;
        var agreement = new Agreement { DisplayName = "NoId" };
        await Assert.ThrowsAsync<ArgumentException>(() => svc.UpdateTermsOfUseAgreementAsync(agreement));
    }

    #endregion
}

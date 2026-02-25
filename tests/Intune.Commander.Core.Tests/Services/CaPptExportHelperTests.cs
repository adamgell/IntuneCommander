using Intune.Commander.Core.Services.CaPptExport;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Core.Tests.Services;

// =====================================================================
// AssignedCloudAppAction
// =====================================================================

public class AssignedCloudAppActionTests
{
    [Fact]
    public void Constructor_NullConditions_HasNoData()
    {
        var result = new AssignedCloudAppAction(new ConditionalAccessPolicy());
        Assert.Null(result.Name);
        Assert.Null(result.IncludeExclude);
        Assert.False(result.HasData);
    }

    [Fact]
    public void Constructor_NullApplications_HasNoData()
    {
        var policy = new ConditionalAccessPolicy
        {
            Conditions = new ConditionalAccessConditionSet()
        };
        var result = new AssignedCloudAppAction(policy);
        Assert.Null(result.Name);
        Assert.False(result.HasData);
    }

    [Fact]
    public void IncludeNone_AccessTypeAppsNone_NameIsMicrosoftEntra()
    {
        var result = new AssignedCloudAppAction(PolicyWithApps(include: ["None"]));
        Assert.Equal(AppAccessType.AppsNone, result.AccessType);
        Assert.Equal("Microsoft Entra", result.Name);
        Assert.False(result.HasData);
    }

    [Fact]
    public void IncludeAll_AccessTypeAppsAll_NameIsAllCloudApps()
    {
        var result = new AssignedCloudAppAction(PolicyWithApps(include: ["All"]));
        Assert.Equal(AppAccessType.AppsAll, result.AccessType);
        Assert.Equal("All cloud apps", result.Name);
        Assert.True(result.HasData);
    }

    [Fact]
    public void IncludeAllSpecialValue_IncludeExcludeContainsAllCloudApps()
    {
        var result = new AssignedCloudAppAction(PolicyWithApps(include: ["All"]));
        Assert.Contains("All cloud apps", result.IncludeExclude);
    }

    [Fact]
    public void IncludeSpecificApp_AccessTypeAppsSelected()
    {
        var result = new AssignedCloudAppAction(PolicyWithApps(include: ["app-id-123"]));
        Assert.Equal(AppAccessType.AppsSelected, result.AccessType);
        Assert.Equal("Selected cloud apps", result.Name);
        Assert.Contains("app-id-123", result.IncludeExclude);
    }

    [Fact]
    public void IncludeUserActionsRegSecInfo_CorrectAccessType()
    {
        var policy = new ConditionalAccessPolicy
        {
            Conditions = new ConditionalAccessConditionSet
            {
                Applications = new ConditionalAccessApplications
                {
                    IncludeUserActions = ["urn:user:registersecurityinfo"]
                }
            }
        };
        var result = new AssignedCloudAppAction(policy);
        Assert.Equal(AppAccessType.UserActionsRegSecInfo, result.AccessType);
        Assert.Equal("Register security information", result.Name);
        Assert.False(result.HasData);
    }

    [Fact]
    public void IncludeUserActionsRegDevice_CorrectAccessType()
    {
        var policy = new ConditionalAccessPolicy
        {
            Conditions = new ConditionalAccessConditionSet
            {
                Applications = new ConditionalAccessApplications
                {
                    IncludeUserActions = ["urn:user:registerdevice"]
                }
            }
        };
        var result = new AssignedCloudAppAction(policy);
        Assert.Equal(AppAccessType.UserActionsRegDevice, result.AccessType);
        Assert.Equal("Register or join devices", result.Name);
    }

    [Fact]
    public void IncludeAuthContext_CorrectAccessTypeAndIncludeExclude()
    {
        var policy = new ConditionalAccessPolicy
        {
            Conditions = new ConditionalAccessConditionSet
            {
                Applications = new ConditionalAccessApplications
                {
                    IncludeAuthenticationContextClassReferences = ["ctx-1", "ctx-2"]
                }
            }
        };
        var result = new AssignedCloudAppAction(policy);
        Assert.Equal(AppAccessType.AuthenticationContext, result.AccessType);
        Assert.Equal("Authentication context", result.Name);
        Assert.Contains("ctx-1", result.IncludeExclude);
        Assert.Contains("ctx-2", result.IncludeExclude);
    }

    [Fact]
    public void EmptyApplications_AccessTypeIsUnknown()
    {
        var policy = new ConditionalAccessPolicy
        {
            Conditions = new ConditionalAccessConditionSet
            {
                Applications = new ConditionalAccessApplications()
            }
        };
        var result = new AssignedCloudAppAction(policy);
        Assert.Equal(AppAccessType.Unknown, result.AccessType);
        Assert.Equal("Unknown", result.Name);
        Assert.True(result.HasData);
    }

    [Fact]
    public void Office365SingleApp_SetsO365OnlyFlagAndUpdatesName()
    {
        var result = new AssignedCloudAppAction(PolicyWithApps(include: ["Office365"]));
        Assert.True(result.IsSelectedAppO365Only);
        Assert.Equal("Office 365", result.Name);
    }

    [Fact]
    public void Office365WithOtherApps_DoesNotSetO365Flag_AppendedToText()
    {
        var result = new AssignedCloudAppAction(PolicyWithApps(include: ["Office365", "other-app"]));
        Assert.False(result.IsSelectedAppO365Only);
        Assert.Contains("Office 365", result.IncludeExclude);
    }

    [Fact]
    public void MicrosoftAdminPortalsSingle_SetsAdminPortalsFlag()
    {
        var result = new AssignedCloudAppAction(PolicyWithApps(include: ["MicrosoftAdminPortals"]));
        Assert.True(result.IsSelectedMicrosoftAdminPortalsOnly);
        Assert.Equal("Microsoft Admin Portals", result.Name);
    }

    [Fact]
    public void MicrosoftAdminPortalsWithOtherApps_DoesNotSetFlag_AppendedToText()
    {
        var result = new AssignedCloudAppAction(PolicyWithApps(include: ["MicrosoftAdminPortals", "other"]));
        Assert.False(result.IsSelectedMicrosoftAdminPortalsOnly);
        Assert.Contains("Microsoft Admin Portals", result.IncludeExclude);
    }

    [Fact]
    public void ExcludeApplications_AppendedToText()
    {
        var result = new AssignedCloudAppAction(PolicyWithApps(
            include: ["All"],
            exclude: ["excluded-app", "Office365", "MicrosoftAdminPortals"]));
        Assert.Contains("excluded-app", result.IncludeExclude);
        Assert.Contains("Office 365", result.IncludeExclude);
        Assert.Contains("Microsoft Admin Portals", result.IncludeExclude);
    }

    private static ConditionalAccessPolicy PolicyWithApps(
        List<string>? include = null,
        List<string>? exclude = null) =>
        new()
        {
            Conditions = new ConditionalAccessConditionSet
            {
                Applications = new ConditionalAccessApplications
                {
                    IncludeApplications = include,
                    ExcludeApplications = exclude
                }
            }
        };
}

// =====================================================================
// AssignedUserWorkload
// =====================================================================

public class AssignedUserWorkloadTests
{
    [Fact]
    public void Constructor_NullConditions_HasNoData()
    {
        var result = new AssignedUserWorkload(new ConditionalAccessPolicy());
        Assert.Null(result.Name);
        Assert.False(result.HasData);
    }

    [Fact]
    public void WorkloadIdentity_ServicePrincipalsInMyTenant_FormatsCorrectly()
    {
        var policy = new ConditionalAccessPolicy
        {
            Conditions = new ConditionalAccessConditionSet
            {
                ClientApplications = new ConditionalAccessClientApplications
                {
                    IncludeServicePrincipals = ["ServicePrincipalsInMyTenant"]
                }
            }
        };
        var result = new AssignedUserWorkload(policy);
        Assert.True(result.IsWorkload);
        Assert.Equal("Workload identity", result.Name);
        Assert.Contains("All owned service principals", result.IncludeExclude);
    }

    [Fact]
    public void WorkloadIdentity_SpecificServicePrincipal_ListsId()
    {
        var policy = new ConditionalAccessPolicy
        {
            Conditions = new ConditionalAccessConditionSet
            {
                ClientApplications = new ConditionalAccessClientApplications
                {
                    IncludeServicePrincipals = ["sp-id-123"]
                }
            }
        };
        var result = new AssignedUserWorkload(policy);
        Assert.True(result.IsWorkload);
        Assert.Contains("sp-id-123", result.IncludeExclude);
    }

    [Fact]
    public void WorkloadIdentity_WithExclude_AppendedToText()
    {
        var policy = new ConditionalAccessPolicy
        {
            Conditions = new ConditionalAccessConditionSet
            {
                ClientApplications = new ConditionalAccessClientApplications
                {
                    IncludeServicePrincipals = ["sp-include"],
                    ExcludeServicePrincipals = ["sp-exclude"]
                }
            }
        };
        var result = new AssignedUserWorkload(policy);
        Assert.Contains("sp-include", result.IncludeExclude);
        Assert.Contains("sp-exclude", result.IncludeExclude);
    }

    [Fact]
    public void UserAssignment_NullUsers_HasDataFalse()
    {
        var policy = new ConditionalAccessPolicy
        {
            Conditions = new ConditionalAccessConditionSet()
        };
        var result = new AssignedUserWorkload(policy);
        Assert.False(result.IsWorkload);
        Assert.Equal("Users", result.Name);
        Assert.False(result.HasData);
    }

    [Fact]
    public void UserAssignment_AllUsers_FormattedAsAllUsers()
    {
        var result = new AssignedUserWorkload(PolicyWithUsers(includeUsers: ["All"]));
        Assert.False(result.IsWorkload);
        Assert.Equal("Users", result.Name);
        Assert.Contains("All users", result.IncludeExclude);
    }

    [Fact]
    public void UserAssignment_NoneUsers_FormattedAsNone()
    {
        var result = new AssignedUserWorkload(PolicyWithUsers(includeUsers: ["None"]));
        Assert.Contains("None", result.IncludeExclude);
    }

    [Fact]
    public void UserAssignment_GuestsOrExternalUsers_FormattedCorrectly()
    {
        var result = new AssignedUserWorkload(PolicyWithUsers(includeUsers: ["GuestsOrExternalUsers"]));
        Assert.Contains("Guest or external users", result.IncludeExclude);
    }

    [Fact]
    public void UserAssignment_ArbitraryUserId_ShowsIdDirectly()
    {
        var result = new AssignedUserWorkload(PolicyWithUsers(includeUsers: ["user-guid-abc"]));
        Assert.Contains("user-guid-abc", result.IncludeExclude);
    }

    [Fact]
    public void UserAssignment_WithRoles_HasIncludeRolesTrueAndListsRole()
    {
        var policy = new ConditionalAccessPolicy
        {
            Conditions = new ConditionalAccessConditionSet
            {
                Users = new ConditionalAccessUsers
                {
                    IncludeRoles = ["role-id-1"]
                }
            }
        };
        var result = new AssignedUserWorkload(policy);
        Assert.True(result.HasIncludeRoles);
        Assert.Contains("role-id-1", result.IncludeExclude);
        Assert.Contains("Directory roles", result.IncludeExclude);
    }

    [Fact]
    public void UserAssignment_WithExternalUser_HasIncludeExternalUserTrue()
    {
        var policy = new ConditionalAccessPolicy
        {
            Conditions = new ConditionalAccessConditionSet
            {
                Users = new ConditionalAccessUsers
                {
                    IncludeGuestsOrExternalUsers = new ConditionalAccessGuestsOrExternalUsers()
                }
            }
        };
        var result = new AssignedUserWorkload(policy);
        Assert.True(result.HasIncludeExternalUser);
        Assert.Contains("Guest or external users", result.IncludeExclude);
    }

    [Fact]
    public void UserAssignment_WithExternalTenant_HasIncludeExternalTenantTrue()
    {
        var policy = new ConditionalAccessPolicy
        {
            Conditions = new ConditionalAccessConditionSet
            {
                Users = new ConditionalAccessUsers
                {
                    IncludeGuestsOrExternalUsers = new ConditionalAccessGuestsOrExternalUsers
                    {
                        ExternalTenants = new ConditionalAccessAllExternalTenants()
                    }
                }
            }
        };
        var result = new AssignedUserWorkload(policy);
        Assert.True(result.HasIncludeExternalTenant);
    }

    [Fact]
    public void UserAssignment_WithIncludeGroups_AppendedToText()
    {
        var result = new AssignedUserWorkload(PolicyWithUsers(includeGroups: ["grp-1"]));
        Assert.Contains("grp-1", result.IncludeExclude);
        Assert.Contains("Groups", result.IncludeExclude);
    }

    [Fact]
    public void UserAssignment_WithExcludeItems_AppendedToText()
    {
        var policy = new ConditionalAccessPolicy
        {
            Conditions = new ConditionalAccessConditionSet
            {
                Users = new ConditionalAccessUsers
                {
                    ExcludeUsers = ["ex-user"],
                    ExcludeGroups = ["ex-grp"],
                    ExcludeRoles = ["ex-role"],
                    ExcludeGuestsOrExternalUsers = new ConditionalAccessGuestsOrExternalUsers()
                }
            }
        };
        var result = new AssignedUserWorkload(policy);
        Assert.Contains("ex-user", result.IncludeExclude);
        Assert.Contains("ex-grp", result.IncludeExclude);
        Assert.Contains("ex-role", result.IncludeExclude);
        Assert.Contains("Guest or external users", result.IncludeExclude);
    }

    private static ConditionalAccessPolicy PolicyWithUsers(
        List<string>? includeUsers = null,
        List<string>? includeGroups = null) =>
        new()
        {
            Conditions = new ConditionalAccessConditionSet
            {
                Users = new ConditionalAccessUsers
                {
                    IncludeUsers = includeUsers,
                    IncludeGroups = includeGroups
                }
            }
        };
}

// =====================================================================
// ConditionPlatforms
// =====================================================================

public class ConditionPlatformsTests
{
    [Fact]
    public void NullPlatforms_HasDataFalse()
    {
        var result = new ConditionPlatforms(new ConditionalAccessPolicy());
        Assert.False(result.HasData);
        Assert.Null(result.IncludeExclude);
    }

    [Fact]
    public void IncludePlatforms_AllKnownValues_AppearInText()
    {
        var policy = new ConditionalAccessPolicy
        {
            Conditions = new ConditionalAccessConditionSet
            {
                Platforms = new ConditionalAccessPlatforms
                {
                    IncludePlatforms =
                    [
                        ConditionalAccessDevicePlatform.All,
                        ConditionalAccessDevicePlatform.Android,
                        ConditionalAccessDevicePlatform.IOS,
                        ConditionalAccessDevicePlatform.Linux,
                        ConditionalAccessDevicePlatform.MacOS,
                        ConditionalAccessDevicePlatform.Windows,
                        ConditionalAccessDevicePlatform.WindowsPhone
                    ]
                }
            }
        };
        var result = new ConditionPlatforms(policy);
        Assert.True(result.HasData);
        Assert.Contains("All", result.IncludeExclude);
        Assert.Contains("Android", result.IncludeExclude);
        Assert.Contains("iOS", result.IncludeExclude);
        Assert.Contains("Linux", result.IncludeExclude);
        Assert.Contains("macOS", result.IncludeExclude);
        Assert.Contains("Windows", result.IncludeExclude);
        Assert.Contains("Windows Phone", result.IncludeExclude);
    }

    [Fact]
    public void ExcludePlatforms_AppendedAfterInclude()
    {
        var policy = new ConditionalAccessPolicy
        {
            Conditions = new ConditionalAccessConditionSet
            {
                Platforms = new ConditionalAccessPlatforms
                {
                    IncludePlatforms = [ConditionalAccessDevicePlatform.All],
                    ExcludePlatforms = [ConditionalAccessDevicePlatform.Android]
                }
            }
        };
        var result = new ConditionPlatforms(policy);
        Assert.True(result.HasData);
        Assert.Contains("Include", result.IncludeExclude);
        Assert.Contains("Exclude", result.IncludeExclude);
        Assert.Contains("Android", result.IncludeExclude);
    }

    [Fact]
    public void OnlyExcludePlatforms_HasData()
    {
        var policy = new ConditionalAccessPolicy
        {
            Conditions = new ConditionalAccessConditionSet
            {
                Platforms = new ConditionalAccessPlatforms
                {
                    ExcludePlatforms = [ConditionalAccessDevicePlatform.Windows]
                }
            }
        };
        var result = new ConditionPlatforms(policy);
        Assert.True(result.HasData);
        Assert.Contains("Windows", result.IncludeExclude);
    }
}

// =====================================================================
// ConditionClientAppTypes
// =====================================================================

public class ConditionClientAppTypesTests
{
    [Fact]
    public void NullClientAppTypes_HasDataFalse()
    {
        var result = new ConditionClientAppTypes(new ConditionalAccessPolicy());
        Assert.False(result.HasData);
    }

    [Fact]
    public void AllClientAppType_ReturnsEmpty_HasDataFalse()
    {
        var policy = new ConditionalAccessPolicy
        {
            Conditions = new ConditionalAccessConditionSet
            {
                ClientAppTypes = [ConditionalAccessClientApp.All]
            }
        };
        var result = new ConditionClientAppTypes(policy);
        Assert.False(result.HasData);
    }

    [Fact]
    public void BrowserClientApp_HasDataWithBrowserText()
    {
        var policy = new ConditionalAccessPolicy
        {
            Conditions = new ConditionalAccessConditionSet
            {
                ClientAppTypes = [ConditionalAccessClientApp.Browser]
            }
        };
        var result = new ConditionClientAppTypes(policy);
        Assert.True(result.HasData);
        Assert.Contains("Browser", result.IncludeExclude);
    }

    [Fact]
    public void MobileAppsAndDesktopClients_HasData()
    {
        var policy = new ConditionalAccessPolicy
        {
            Conditions = new ConditionalAccessConditionSet
            {
                ClientAppTypes = [ConditionalAccessClientApp.MobileAppsAndDesktopClients]
            }
        };
        var result = new ConditionClientAppTypes(policy);
        Assert.True(result.HasData);
        Assert.Contains("Mobile app and desktop clients", result.IncludeExclude);
    }

    [Fact]
    public void ExchangeActiveSync_HasData()
    {
        var policy = new ConditionalAccessPolicy
        {
            Conditions = new ConditionalAccessConditionSet
            {
                ClientAppTypes = [ConditionalAccessClientApp.ExchangeActiveSync]
            }
        };
        var result = new ConditionClientAppTypes(policy);
        Assert.True(result.HasData);
        Assert.Contains("Exchange ActiveSync clients", result.IncludeExclude);
    }

    [Fact]
    public void EasSupported_HasData()
    {
        var policy = new ConditionalAccessPolicy
        {
            Conditions = new ConditionalAccessConditionSet
            {
                ClientAppTypes = [ConditionalAccessClientApp.EasSupported]
            }
        };
        var result = new ConditionClientAppTypes(policy);
        Assert.True(result.HasData);
        Assert.Contains("Exchange ActiveSync clients", result.IncludeExclude);
    }

    [Fact]
    public void OtherLegacyClients_HasData()
    {
        var policy = new ConditionalAccessPolicy
        {
            Conditions = new ConditionalAccessConditionSet
            {
                ClientAppTypes = [ConditionalAccessClientApp.Other]
            }
        };
        var result = new ConditionClientAppTypes(policy);
        Assert.True(result.HasData);
        Assert.Contains("Other legacy clients", result.IncludeExclude);
    }

    [Fact]
    public void MultipleClientAppTypes_AllAppearInText()
    {
        var policy = new ConditionalAccessPolicy
        {
            Conditions = new ConditionalAccessConditionSet
            {
                ClientAppTypes =
                [
                    ConditionalAccessClientApp.Browser,
                    ConditionalAccessClientApp.MobileAppsAndDesktopClients,
                    ConditionalAccessClientApp.Other
                ]
            }
        };
        var result = new ConditionClientAppTypes(policy);
        Assert.True(result.HasData);
        Assert.Contains("Browser", result.IncludeExclude);
        Assert.Contains("Mobile app and desktop clients", result.IncludeExclude);
        Assert.Contains("Other legacy clients", result.IncludeExclude);
    }
}

// =====================================================================
// ConditionLocations
// =====================================================================

public class ConditionLocationsTests
{
    [Fact]
    public void NullLocations_HasDataFalse()
    {
        var result = new ConditionLocations(new ConditionalAccessPolicy());
        Assert.False(result.HasData);
    }

    [Fact]
    public void IncludeAll_MapsToAnyLocation()
    {
        var result = new ConditionLocations(PolicyWithInclude(["All"]));
        Assert.True(result.HasData);
        Assert.Contains("Any location", result.IncludeExclude);
    }

    [Fact]
    public void IncludeAllTrusted_MapsToAllTrustedLocations()
    {
        var result = new ConditionLocations(PolicyWithInclude(["AllTrusted"]));
        Assert.Contains("All trusted locations", result.IncludeExclude);
    }

    [Fact]
    public void IncludeMfaTrustedIpsGuid_MapsMfaTrustedIPs()
    {
        var result = new ConditionLocations(PolicyWithInclude(["00000000-0000-0000-0000-000000000000"]));
        Assert.Contains("MFA Trusted IPs", result.IncludeExclude);
    }

    [Fact]
    public void IncludeCustomLocationId_ShowsIdDirectly()
    {
        var result = new ConditionLocations(PolicyWithInclude(["loc-id-abc"]));
        Assert.Contains("loc-id-abc", result.IncludeExclude);
    }

    [Fact]
    public void ExcludeLocations_AppendedAfterInclude()
    {
        var policy = new ConditionalAccessPolicy
        {
            Conditions = new ConditionalAccessConditionSet
            {
                Locations = new ConditionalAccessLocations
                {
                    IncludeLocations = ["All"],
                    ExcludeLocations = ["AllTrusted"]
                }
            }
        };
        var result = new ConditionLocations(policy);
        Assert.Contains("Any location", result.IncludeExclude);
        Assert.Contains("All trusted locations", result.IncludeExclude);
        Assert.Contains("Include", result.IncludeExclude);
        Assert.Contains("Exclude", result.IncludeExclude);
    }

    [Fact]
    public void EmptyLocationLists_HasDataFalse()
    {
        var policy = new ConditionalAccessPolicy
        {
            Conditions = new ConditionalAccessConditionSet
            {
                Locations = new ConditionalAccessLocations()
            }
        };
        var result = new ConditionLocations(policy);
        Assert.False(result.HasData);
    }

    private static ConditionalAccessPolicy PolicyWithInclude(List<string> include) =>
        new()
        {
            Conditions = new ConditionalAccessConditionSet
            {
                Locations = new ConditionalAccessLocations
                {
                    IncludeLocations = include
                }
            }
        };
}

// =====================================================================
// ConditionRisks
// =====================================================================

public class ConditionRisksTests
{
    [Fact]
    public void NullConditions_HasDataFalse()
    {
        var result = new ConditionRisks(new ConditionalAccessPolicy());
        Assert.False(result.HasData);
    }

    [Fact]
    public void EmptyConditions_HasDataFalse()
    {
        var policy = new ConditionalAccessPolicy
        {
            Conditions = new ConditionalAccessConditionSet()
        };
        var result = new ConditionRisks(policy);
        Assert.False(result.HasData);
    }

    [Fact]
    public void UserRisk_AllKnownLevels_AppearInText()
    {
        var policy = new ConditionalAccessPolicy
        {
            Conditions = new ConditionalAccessConditionSet
            {
                UserRiskLevels =
                [
                    RiskLevel.Hidden,
                    RiskLevel.High,
                    RiskLevel.Low,
                    RiskLevel.Medium,
                    RiskLevel.None
                ]
            }
        };
        var result = new ConditionRisks(policy);
        Assert.True(result.HasData);
        Assert.Contains("User risk:", result.IncludeExclude);
        Assert.Contains("Hidden", result.IncludeExclude);
        Assert.Contains("High", result.IncludeExclude);
        Assert.Contains("Low", result.IncludeExclude);
        Assert.Contains("Medium", result.IncludeExclude);
        Assert.Contains("No risk", result.IncludeExclude);
    }

    [Fact]
    public void SignInRisk_HasData()
    {
        var policy = new ConditionalAccessPolicy
        {
            Conditions = new ConditionalAccessConditionSet
            {
                SignInRiskLevels = [RiskLevel.High]
            }
        };
        var result = new ConditionRisks(policy);
        Assert.True(result.HasData);
        Assert.Contains("Sign-in risk:", result.IncludeExclude);
        Assert.Contains("High", result.IncludeExclude);
    }

    [Fact]
    public void ServicePrincipalRisk_HasData()
    {
        var policy = new ConditionalAccessPolicy
        {
            Conditions = new ConditionalAccessConditionSet
            {
                ServicePrincipalRiskLevels = [RiskLevel.Medium]
            }
        };
        var result = new ConditionRisks(policy);
        Assert.True(result.HasData);
        Assert.Contains("Service principal risk:", result.IncludeExclude);
        Assert.Contains("Medium", result.IncludeExclude);
    }

    [Fact]
    public void AllRiskTypes_AllAppearInText()
    {
        var policy = new ConditionalAccessPolicy
        {
            Conditions = new ConditionalAccessConditionSet
            {
                UserRiskLevels = [RiskLevel.High],
                SignInRiskLevels = [RiskLevel.Low],
                ServicePrincipalRiskLevels = [RiskLevel.Medium]
            }
        };
        var result = new ConditionRisks(policy);
        Assert.Contains("User risk:", result.IncludeExclude);
        Assert.Contains("Sign-in risk:", result.IncludeExclude);
        Assert.Contains("Service principal risk:", result.IncludeExclude);
    }
}

// =====================================================================
// ConditionDeviceFilters
// =====================================================================

public class ConditionDeviceFiltersTests
{
    [Fact]
    public void NullDevices_HasDataFalse()
    {
        var result = new ConditionDeviceFilters(new ConditionalAccessPolicy());
        Assert.False(result.HasData);
    }

    [Fact]
    public void NullDeviceFilter_HasDataFalse()
    {
        var policy = new ConditionalAccessPolicy
        {
            Conditions = new ConditionalAccessConditionSet
            {
                Devices = new ConditionalAccessDevices()
            }
        };
        var result = new ConditionDeviceFilters(policy);
        Assert.False(result.HasData);
    }

    [Fact]
    public void IncludeMode_ProducesIncludeWhenText()
    {
        var policy = new ConditionalAccessPolicy
        {
            Conditions = new ConditionalAccessConditionSet
            {
                Devices = new ConditionalAccessDevices
                {
                    DeviceFilter = new ConditionalAccessFilter
                    {
                        Mode = FilterMode.Include,
                        Rule = "device.isCompliant -eq True"
                    }
                }
            }
        };
        var result = new ConditionDeviceFilters(policy);
        Assert.True(result.HasData);
        Assert.Contains("Include when", result.IncludeExclude);
        Assert.Contains("device.isCompliant -eq True", result.IncludeExclude);
    }

    [Fact]
    public void ExcludeMode_ProducesExcludeWhenText()
    {
        var policy = new ConditionalAccessPolicy
        {
            Conditions = new ConditionalAccessConditionSet
            {
                Devices = new ConditionalAccessDevices
                {
                    DeviceFilter = new ConditionalAccessFilter
                    {
                        Mode = FilterMode.Exclude,
                        Rule = "device.isManaged -eq True"
                    }
                }
            }
        };
        var result = new ConditionDeviceFilters(policy);
        Assert.True(result.HasData);
        Assert.Contains("Exclude when", result.IncludeExclude);
        Assert.Contains("device.isManaged -eq True", result.IncludeExclude);
    }
}

// =====================================================================
// ControlGrantBlock
// =====================================================================

public class ControlGrantBlockTests
{
    [Fact]
    public void NullGrantControls_IsGrantFalseAndCountZero()
    {
        var result = new ControlGrantBlock(new ConditionalAccessPolicy());
        Assert.False(result.IsGrant);
        Assert.Equal(0, result.GrantControlsCount);
    }

    [Fact]
    public void NullBuiltInControls_IsGrantTrue()
    {
        var policy = new ConditionalAccessPolicy
        {
            GrantControls = new ConditionalAccessGrantControls()
        };
        var result = new ControlGrantBlock(policy);
        Assert.True(result.IsGrant);
    }

    [Fact]
    public void EmptyBuiltInControls_IsGrantTrue()
    {
        var policy = new ConditionalAccessPolicy
        {
            GrantControls = new ConditionalAccessGrantControls
            {
                BuiltInControls = []
            }
        };
        var result = new ControlGrantBlock(policy);
        Assert.True(result.IsGrant);
        Assert.Equal(0, result.GrantControlsCount);
    }

    [Fact]
    public void BlockControl_IsGrantFalse()
    {
        var policy = new ConditionalAccessPolicy
        {
            GrantControls = new ConditionalAccessGrantControls
            {
                BuiltInControls = [ConditionalAccessGrantControl.Block]
            }
        };
        var result = new ControlGrantBlock(policy);
        Assert.False(result.IsGrant);
    }

    [Fact]
    public void MfaControl_SetsMfaTrueAndIncrementsCount()
    {
        var result = new ControlGrantBlock(PolicyWithControls([ConditionalAccessGrantControl.Mfa]));
        Assert.True(result.Mfa);
        Assert.Equal(1, result.GrantControlsCount);
        Assert.Contains("MFA", result.Name);
    }

    [Fact]
    public void CompliantDeviceControl_SetsCompliantDevice()
    {
        var result = new ControlGrantBlock(PolicyWithControls([ConditionalAccessGrantControl.CompliantDevice]));
        Assert.True(result.CompliantDevice);
        Assert.Contains("Compliant Device", result.Name);
    }

    [Fact]
    public void DomainJoinedDeviceControl_SetsDomainJoined()
    {
        var result = new ControlGrantBlock(PolicyWithControls([ConditionalAccessGrantControl.DomainJoinedDevice]));
        Assert.True(result.DomainJoinedDevice);
        Assert.Contains("HAADJ", result.Name);
    }

    [Fact]
    public void ApprovedApplicationControl_SetsApprovedApplication()
    {
        var result = new ControlGrantBlock(PolicyWithControls([ConditionalAccessGrantControl.ApprovedApplication]));
        Assert.True(result.ApprovedApplication);
        Assert.Contains("Approved App", result.Name);
    }

    [Fact]
    public void CompliantApplicationControl_SetsCompliantApplication()
    {
        var result = new ControlGrantBlock(PolicyWithControls([ConditionalAccessGrantControl.CompliantApplication]));
        Assert.True(result.CompliantApplication);
        Assert.Contains("Compliant App", result.Name);
    }

    [Fact]
    public void PasswordChangeControl_SetsPasswordChange()
    {
        var result = new ControlGrantBlock(PolicyWithControls([ConditionalAccessGrantControl.PasswordChange]));
        Assert.True(result.PasswordChange);
        Assert.Contains("Password Change", result.Name);
    }

    [Fact]
    public void MultipleControls_CountMatchesNumberOfControls()
    {
        var result = new ControlGrantBlock(PolicyWithControls(
        [
            ConditionalAccessGrantControl.Mfa,
            ConditionalAccessGrantControl.CompliantDevice
        ]));
        Assert.Equal(2, result.GrantControlsCount);
        Assert.True(result.Mfa);
        Assert.True(result.CompliantDevice);
    }

    [Fact]
    public void OperatorAnd_IsGrantRequireAllTrue()
    {
        var policy = new ConditionalAccessPolicy
        {
            GrantControls = new ConditionalAccessGrantControls
            {
                Operator = "AND",
                BuiltInControls = [ConditionalAccessGrantControl.Mfa]
            }
        };
        var result = new ControlGrantBlock(policy);
        Assert.True(result.IsGrantRequireAll);
        Assert.False(result.IsGrantRequireOne);
    }

    [Fact]
    public void OperatorOr_IsGrantRequireOneTrue()
    {
        var policy = new ConditionalAccessPolicy
        {
            GrantControls = new ConditionalAccessGrantControls
            {
                Operator = "OR",
                BuiltInControls = [ConditionalAccessGrantControl.Mfa]
            }
        };
        var result = new ControlGrantBlock(policy);
        Assert.False(result.IsGrantRequireAll);
        Assert.True(result.IsGrantRequireOne);
    }

    [Fact]
    public void CustomAuthenticationFactors_SetsPropertiesAndName()
    {
        var policy = new ConditionalAccessPolicy
        {
            GrantControls = new ConditionalAccessGrantControls
            {
                BuiltInControls = [],
                CustomAuthenticationFactors = ["factor-1", "factor-2"]
            }
        };
        var result = new ControlGrantBlock(policy);
        Assert.True(result.CustomAuthenticationFactor);
        Assert.Equal("factor-1, factor-2", result.CustomAuthenticationFactorName);
        Assert.Contains("3PMFA", result.Name);
        Assert.Equal(2, result.GrantControlsCount);
    }

    [Fact]
    public void TermsOfUse_SetsPropertiesAndName()
    {
        var policy = new ConditionalAccessPolicy
        {
            GrantControls = new ConditionalAccessGrantControls
            {
                BuiltInControls = [],
                TermsOfUse = ["tou-id-1", "tou-id-2"]
            }
        };
        var result = new ControlGrantBlock(policy);
        Assert.True(result.TermsOfUse);
        Assert.Equal("tou-id-1, tou-id-2", result.TermsOfUseName);
        Assert.Contains("ToU", result.Name);
        Assert.Equal(2, result.GrantControlsCount);
    }

    [Fact]
    public void AuthenticationStrength_SetsPropertiesAndName()
    {
        var policy = new ConditionalAccessPolicy
        {
            GrantControls = new ConditionalAccessGrantControls
            {
                BuiltInControls = [],
                AuthenticationStrength = new AuthenticationStrengthPolicy
                {
                    DisplayName = "Phishing-resistant MFA"
                }
            }
        };
        var result = new ControlGrantBlock(policy);
        Assert.True(result.AuthenticationStrength);
        Assert.Contains("MFA Strength", result.Name);
        Assert.Contains("Phishing-resistant MFA", result.AuthenticationStrengthName);
    }

    private static ConditionalAccessPolicy PolicyWithControls(
        List<ConditionalAccessGrantControl?> controls) =>
        new()
        {
            GrantControls = new ConditionalAccessGrantControls
            {
                Operator = "OR",
                BuiltInControls = controls
            }
        };
}

// =====================================================================
// ControlSession
// =====================================================================

public class ControlSessionTests
{
    [Fact]
    public void NullSession_AllPropertiesFalseOrNull()
    {
        var result = new ControlSession(new ConditionalAccessPolicy());
        Assert.False(result.UseAppEnforcedRestrictions);
        Assert.False(result.UseConditionalAccessAppControl);
        Assert.False(result.SignInFrequency);
        Assert.False(result.PersistentBrowserSession);
        Assert.False(result.ContinuousAccessEvaluation);
        Assert.False(result.DisableResilienceDefaults);
        Assert.False(result.SecureSignInSession);
        Assert.Null(result.SignInFrequencyIntervalLabel);
        Assert.Null(result.CloudAppSecurityType);
        Assert.Null(result.PersistentBrowserSessionModeLabel);
        Assert.Null(result.ContinuousAccessEvaluationModeLabel);
    }

    [Fact]
    public void AppEnforcedRestrictions_Enabled_True()
    {
        var policy = new ConditionalAccessPolicy
        {
            SessionControls = new ConditionalAccessSessionControls
            {
                ApplicationEnforcedRestrictions = new ApplicationEnforcedRestrictionsSessionControl
                {
                    IsEnabled = true
                }
            }
        };
        var result = new ControlSession(policy);
        Assert.True(result.UseAppEnforcedRestrictions);
    }

    [Fact]
    public void AppEnforcedRestrictions_NotEnabled_False()
    {
        var policy = new ConditionalAccessPolicy
        {
            SessionControls = new ConditionalAccessSessionControls
            {
                ApplicationEnforcedRestrictions = new ApplicationEnforcedRestrictionsSessionControl
                {
                    IsEnabled = false
                }
            }
        };
        var result = new ControlSession(policy);
        Assert.False(result.UseAppEnforcedRestrictions);
    }

    [Fact]
    public void CloudAppSecurity_Enabled_SetsTypeLabel()
    {
        var policy = new ConditionalAccessPolicy
        {
            SessionControls = new ConditionalAccessSessionControls
            {
                CloudAppSecurity = new CloudAppSecuritySessionControl
                {
                    IsEnabled = true,
                    CloudAppSecurityType = CloudAppSecuritySessionControlType.MonitorOnly
                }
            }
        };
        var result = new ControlSession(policy);
        Assert.True(result.UseConditionalAccessAppControl);
        Assert.NotNull(result.CloudAppSecurityType);
    }

    [Fact]
    public void CloudAppSecurity_NotEnabled_False()
    {
        var policy = new ConditionalAccessPolicy
        {
            SessionControls = new ConditionalAccessSessionControls
            {
                CloudAppSecurity = new CloudAppSecuritySessionControl
                {
                    IsEnabled = false
                }
            }
        };
        var result = new ControlSession(policy);
        Assert.False(result.UseConditionalAccessAppControl);
        Assert.Null(result.CloudAppSecurityType);
    }

    [Fact]
    public void SignInFrequency_EveryTime_SetsEveryTimeLabel()
    {
        var policy = new ConditionalAccessPolicy
        {
            SessionControls = new ConditionalAccessSessionControls
            {
                SignInFrequency = new SignInFrequencySessionControl
                {
                    IsEnabled = true,
                    FrequencyInterval = SignInFrequencyInterval.EveryTime
                }
            }
        };
        var result = new ControlSession(policy);
        Assert.True(result.SignInFrequency);
        Assert.Equal("Every time", result.SignInFrequencyIntervalLabel);
    }

    [Fact]
    public void SignInFrequency_TimeBasedPluralHours_SetsCorrectLabel()
    {
        var policy = new ConditionalAccessPolicy
        {
            SessionControls = new ConditionalAccessSessionControls
            {
                SignInFrequency = new SignInFrequencySessionControl
                {
                    IsEnabled = true,
                    FrequencyInterval = SignInFrequencyInterval.TimeBased,
                    Type = SigninFrequencyType.Hours,
                    Value = 2
                }
            }
        };
        var result = new ControlSession(policy);
        Assert.True(result.SignInFrequency);
        Assert.Contains("2", result.SignInFrequencyIntervalLabel);
        Assert.Contains("hours", result.SignInFrequencyIntervalLabel);
    }

    [Fact]
    public void SignInFrequency_TimeBasedSingularHour_RemovesTrailingS()
    {
        var policy = new ConditionalAccessPolicy
        {
            SessionControls = new ConditionalAccessSessionControls
            {
                SignInFrequency = new SignInFrequencySessionControl
                {
                    IsEnabled = true,
                    FrequencyInterval = SignInFrequencyInterval.TimeBased,
                    Type = SigninFrequencyType.Hours,
                    Value = 1
                }
            }
        };
        var result = new ControlSession(policy);
        Assert.True(result.SignInFrequency);
        Assert.Equal("1 hour", result.SignInFrequencyIntervalLabel);
    }

    [Fact]
    public void SignInFrequency_TimeBasedDays_SetsLabel()
    {
        var policy = new ConditionalAccessPolicy
        {
            SessionControls = new ConditionalAccessSessionControls
            {
                SignInFrequency = new SignInFrequencySessionControl
                {
                    IsEnabled = true,
                    FrequencyInterval = SignInFrequencyInterval.TimeBased,
                    Type = SigninFrequencyType.Days,
                    Value = 7
                }
            }
        };
        var result = new ControlSession(policy);
        Assert.True(result.SignInFrequency);
        Assert.Contains("7", result.SignInFrequencyIntervalLabel);
        Assert.Contains("days", result.SignInFrequencyIntervalLabel);
    }

    [Fact]
    public void SignInFrequency_NotEnabled_False()
    {
        var policy = new ConditionalAccessPolicy
        {
            SessionControls = new ConditionalAccessSessionControls
            {
                SignInFrequency = new SignInFrequencySessionControl
                {
                    IsEnabled = false
                }
            }
        };
        var result = new ControlSession(policy);
        Assert.False(result.SignInFrequency);
        Assert.Null(result.SignInFrequencyIntervalLabel);
    }

    [Fact]
    public void PersistentBrowser_Enabled_SetsModeLabel()
    {
        var policy = new ConditionalAccessPolicy
        {
            SessionControls = new ConditionalAccessSessionControls
            {
                PersistentBrowser = new PersistentBrowserSessionControl
                {
                    IsEnabled = true,
                    Mode = PersistentBrowserSessionMode.Always
                }
            }
        };
        var result = new ControlSession(policy);
        Assert.True(result.PersistentBrowserSession);
        Assert.NotNull(result.PersistentBrowserSessionModeLabel);
    }

    [Fact]
    public void PersistentBrowser_NotEnabled_False()
    {
        var policy = new ConditionalAccessPolicy
        {
            SessionControls = new ConditionalAccessSessionControls
            {
                PersistentBrowser = new PersistentBrowserSessionControl
                {
                    IsEnabled = false
                }
            }
        };
        var result = new ControlSession(policy);
        Assert.False(result.PersistentBrowserSession);
        Assert.Null(result.PersistentBrowserSessionModeLabel);
    }

    [Fact]
    public void ContinuousAccessEvaluation_WhenSet_IsTrue()
    {
        var policy = new ConditionalAccessPolicy
        {
            SessionControls = new ConditionalAccessSessionControls
            {
                ContinuousAccessEvaluation = new ContinuousAccessEvaluationSessionControl()
            }
        };
        var result = new ControlSession(policy);
        Assert.True(result.ContinuousAccessEvaluation);
    }

    [Fact]
    public void DisableResilienceDefaults_True_SetsProperty()
    {
        var policy = new ConditionalAccessPolicy
        {
            SessionControls = new ConditionalAccessSessionControls
            {
                DisableResilienceDefaults = true
            }
        };
        var result = new ControlSession(policy);
        Assert.True(result.DisableResilienceDefaults);
    }

    [Fact]
    public void DisableResilienceDefaults_False_RemainsFalse()
    {
        var policy = new ConditionalAccessPolicy
        {
            SessionControls = new ConditionalAccessSessionControls
            {
                DisableResilienceDefaults = false
            }
        };
        var result = new ControlSession(policy);
        Assert.False(result.DisableResilienceDefaults);
    }

    [Fact]
    public void SecureSignInSession_Enabled_True()
    {
        var policy = new ConditionalAccessPolicy
        {
            SessionControls = new ConditionalAccessSessionControls
            {
                SecureSignInSession = new SecureSignInSessionControl
                {
                    IsEnabled = true
                }
            }
        };
        var result = new ControlSession(policy);
        Assert.True(result.SecureSignInSession);
    }

    [Fact]
    public void SecureSignInSession_NotEnabled_False()
    {
        var policy = new ConditionalAccessPolicy
        {
            SessionControls = new ConditionalAccessSessionControls
            {
                SecureSignInSession = new SecureSignInSessionControl
                {
                    IsEnabled = false
                }
            }
        };
        var result = new ControlSession(policy);
        Assert.False(result.SecureSignInSession);
    }
}

using System;

using System.Linq;

using System.Text;

using System.Text.Json;

using CommunityToolkit.Mvvm.Input;

using Microsoft.Graph.Beta.Models;



namespace Intune.Commander.Desktop.ViewModels;



public partial class MainWindowViewModel : ViewModelBase

{

    /// <summary>

    /// Creates a <see cref="GroupLookupViewModel"/> wired to the current Graph services.

    /// Returns null if not connected.

    /// </summary>

    public GroupLookupViewModel? CreateGroupLookupViewModel()

    {

        if (_groupService == null || _configProfileService == null ||

            _compliancePolicyService == null || _applicationService == null)

            return null;



        return new GroupLookupViewModel(

            _groupService, _configProfileService,

            _compliancePolicyService, _applicationService);

    }



    /// <summary>

    /// Creates an <see cref="AssignmentReportViewModel"/> wired to the current Graph services.

    /// Returns null if not connected.

    /// </summary>

    public AssignmentReportViewModel? CreateAssignmentReportViewModel()

    {

        if (_graphClient == null || _groupService == null) return null;



        return new AssignmentReportViewModel(

            new Intune.Commander.Core.Services.AssignmentCheckerService(_graphClient, _cacheService, ActiveProfile?.TenantId),

            _groupService,

            new Intune.Commander.Core.Services.UserService(_graphClient));

    }



    [RelayCommand]

    private void CopyDetailsToClipboard()

    {

        var text = GetDetailText();

        if (!string.IsNullOrEmpty(text))

            CopyDetailsRequested?.Invoke(text);

    }



    [RelayCommand]

    private void ViewRawJson()

    {

        object? item = SelectedConfiguration as object

            ?? SelectedCompliancePolicy as object

            ?? SelectedSettingsCatalogPolicy as object

            ?? SelectedApplication as object

            ?? SelectedEndpointSecurityIntent as object

            ?? SelectedAdministrativeTemplate as object

            ?? SelectedEnrollmentConfiguration as object

            ?? SelectedAppProtectionPolicy as object

            ?? SelectedManagedDeviceAppConfiguration as object

            ?? SelectedTargetedManagedAppConfiguration as object

            ?? SelectedTermsAndConditions as object

            ?? SelectedScopeTag as object

            ?? SelectedRoleDefinition as object

            ?? SelectedIntuneBrandingProfile as object

            ?? SelectedAzureBrandingLocalization as object

            ?? SelectedAutopilotProfile as object

            ?? SelectedDeviceHealthScript as object

            ?? SelectedMacCustomAttribute as object

            ?? SelectedFeatureUpdateProfile as object

            ?? SelectedNamedLocation as object

            ?? SelectedAuthenticationStrengthPolicy as object

            ?? SelectedAuthenticationContextClassReference as object

            ?? SelectedTermsOfUseAgreement as object

            ?? SelectedConditionalAccessPolicy as object

            ?? SelectedAssignmentFilter as object

            ?? SelectedPolicySet as object

            ?? SelectedDeviceManagementScript as object

            ?? SelectedDeviceShellScript as object

            ?? SelectedComplianceScript as object;



        if (item == null) return;



        var title = item switch

        {

            DeviceConfiguration cfg => cfg.DisplayName ?? "Device Configuration",

            DeviceCompliancePolicy pol => pol.DisplayName ?? "Compliance Policy",

            DeviceManagementConfigurationPolicy sc => sc.Name ?? "Settings Catalog Policy",

            MobileApp app => app.DisplayName ?? "Application",

            DeviceManagementIntent esi => esi.DisplayName ?? "Endpoint Security",

            GroupPolicyConfiguration at => at.DisplayName ?? "Administrative Template",

            DeviceEnrollmentConfiguration ec => ec.DisplayName ?? "Enrollment Configuration",

            ManagedDeviceMobileAppConfiguration managedConfig => managedConfig.DisplayName ?? "Managed Device App Configuration",

            TargetedManagedAppConfiguration targetedConfig => targetedConfig.DisplayName ?? "Targeted Managed App Configuration",

            ManagedAppPolicy appProtection => appProtection.DisplayName ?? "App Protection Policy",

            TermsAndConditions terms => terms.DisplayName ?? "Terms and Conditions",

            RoleScopeTag scopeTag => scopeTag.DisplayName ?? "Scope Tag",

            RoleDefinition roleDefinition => roleDefinition.DisplayName ?? "Role Definition",

            IntuneBrandingProfile brandingProfile => brandingProfile.ProfileName ?? "Intune Branding",

            OrganizationalBrandingLocalization azureBranding => azureBranding.Id ?? "Azure Branding",

            WindowsAutopilotDeploymentProfile autopilot => TryReadStringProperty(autopilot, "DisplayName") ?? "Autopilot Profile",

            DeviceHealthScript deviceHealthScript => TryReadStringProperty(deviceHealthScript, "DisplayName") ?? "Device Health Script",

            DeviceCustomAttributeShellScript macCustomAttribute => TryReadStringProperty(macCustomAttribute, "DisplayName") ?? "Mac Custom Attribute",

            WindowsFeatureUpdateProfile featureUpdateProfile => TryReadStringProperty(featureUpdateProfile, "DisplayName") ?? "Feature Update Profile",

            NamedLocation namedLocation => TryReadStringProperty(namedLocation, "DisplayName") ?? "Named Location",

            AuthenticationStrengthPolicy authStrength => TryReadStringProperty(authStrength, "DisplayName") ?? "Authentication Strength",

            AuthenticationContextClassReference authContext => TryReadStringProperty(authContext, "DisplayName") ?? "Authentication Context",

            Agreement termsOfUse => TryReadStringProperty(termsOfUse, "DisplayName") ?? "Terms of Use",

            ConditionalAccessPolicy cap => cap.DisplayName ?? "Conditional Access Policy",

            DeviceAndAppManagementAssignmentFilter af => af.DisplayName ?? "Assignment Filter",

            PolicySet ps => ps.DisplayName ?? "Policy Set",

            DeviceManagementScript dms => dms.DisplayName ?? "Device Management Script",

            DeviceShellScript dss => dss.DisplayName ?? "Device Shell Script",

            DeviceComplianceScript cs => cs.DisplayName ?? "Compliance Script",

            _ => "Item"

        };



        try

        {

            var json = JsonSerializer.Serialize(item, item.GetType(), new JsonSerializerOptions

            {

                WriteIndented = true,

                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull

            });

            ViewRawJsonRequested?.Invoke(title, json);

        }

        catch (Exception ex)

        {

            DebugLog.LogError($"Failed to serialize item to JSON: {ex.Message}", ex);

            SetError("Failed to serialize item to JSON");

        }

    }



    /// <summary>

    /// Builds a plain-text representation of whichever item is currently selected in the detail pane.

    /// </summary>

    public string GetDetailText()

    {

        var sb = new StringBuilder();



        if (SelectedConfiguration is { } cfg)

        {

            sb.AppendLine("=== Device Configuration ===");

            Append(sb, "Name", cfg.DisplayName);

            Append(sb, "Description", cfg.Description);

            Append(sb, "Platform / Type", SelectedItemTypeName);

            Append(sb, "ID", cfg.Id);

            Append(sb, "Created", cfg.CreatedDateTime?.ToString("g"));

            Append(sb, "Last Modified", cfg.LastModifiedDateTime?.ToString("g"));

            Append(sb, "Version", cfg.Version?.ToString());

            AppendAssignments(sb);

        }

        else if (SelectedCompliancePolicy is { } pol)

        {

            sb.AppendLine("=== Compliance Policy ===");

            Append(sb, "Name", pol.DisplayName);

            Append(sb, "Description", pol.Description);

            Append(sb, "Platform / Type", SelectedItemTypeName);

            Append(sb, "ID", pol.Id);

            Append(sb, "Created", pol.CreatedDateTime?.ToString("g"));

            Append(sb, "Last Modified", pol.LastModifiedDateTime?.ToString("g"));

            Append(sb, "Version", pol.Version?.ToString());

            AppendAssignments(sb);

        }

        else if (SelectedSettingsCatalogPolicy is { } sc)

        {

            sb.AppendLine("=== Settings Catalog Policy ===");

            Append(sb, "Name", sc.Name);

            Append(sb, "Description", sc.Description);

            Append(sb, "Platforms", sc.Platforms?.ToString());

            Append(sb, "Technologies", sc.Technologies?.ToString());

            Append(sb, "ID", sc.Id);

            Append(sb, "Is Assigned", sc.IsAssigned?.ToString());

            Append(sb, "Role Scope Tags", sc.RoleScopeTagIds != null ? string.Join(", ", sc.RoleScopeTagIds) : "");

            Append(sb, "Created", sc.CreatedDateTime?.ToString("g"));

            Append(sb, "Last Modified", sc.LastModifiedDateTime?.ToString("g"));

            AppendAssignments(sb);

        }

        else if (SelectedApplication is { } app)

        {

            sb.AppendLine("=== Application ===");

            Append(sb, "Name", app.DisplayName);

            Append(sb, "Description", app.Description);

            Append(sb, "App Type", SelectedItemTypeName);

            Append(sb, "Platform", SelectedItemPlatform);

            Append(sb, "ID", app.Id);

            Append(sb, "Publisher", app.Publisher);

            Append(sb, "Developer", app.Developer);

            Append(sb, "Owner", app.Owner);

            Append(sb, "Featured", app.IsFeatured?.ToString());

            Append(sb, "Notes", app.Notes);

            Append(sb, "Information URL", app.InformationUrl);

            Append(sb, "Privacy URL", app.PrivacyInformationUrl);

            Append(sb, "Created", app.CreatedDateTime?.ToString("g"));

            Append(sb, "Last Modified", app.LastModifiedDateTime?.ToString("g"));

            Append(sb, "Publishing State", app.PublishingState?.ToString());

            AppendAssignments(sb);

        }

        else if (SelectedEndpointSecurityIntent is { } endpointSecurity)

        {

            sb.AppendLine("=== Endpoint Security ===");

            Append(sb, "Name", endpointSecurity.DisplayName);

            Append(sb, "Description", endpointSecurity.Description);

            Append(sb, "Type", SelectedItemTypeName);

            Append(sb, "ID", endpointSecurity.Id);

            Append(sb, "Is Assigned", endpointSecurity.IsAssigned?.ToString());

            Append(sb, "Last Modified", endpointSecurity.LastModifiedDateTime?.ToString("g"));

            AppendAssignments(sb);

        }

        else if (SelectedAdministrativeTemplate is { } adminTemplate)

        {

            sb.AppendLine("=== Administrative Template ===");

            Append(sb, "Name", adminTemplate.DisplayName);

            Append(sb, "Description", adminTemplate.Description);

            Append(sb, "Type", SelectedItemTypeName);

            Append(sb, "ID", adminTemplate.Id);

            Append(sb, "Ingestion Type", adminTemplate.PolicyConfigurationIngestionType?.ToString());

            Append(sb, "Last Modified", adminTemplate.LastModifiedDateTime?.ToString("g"));

            AppendAssignments(sb);

        }

        else if (SelectedEnrollmentConfiguration is { } enrollment)

        {

            sb.AppendLine("=== Enrollment Configuration ===");

            Append(sb, "Name", enrollment.DisplayName);

            Append(sb, "Description", enrollment.Description);

            Append(sb, "Type", FriendlyODataType(enrollment.OdataType));

            Append(sb, "ID", enrollment.Id);

            Append(sb, "Priority", enrollment.Priority?.ToString());

        }

        else if (SelectedAppProtectionPolicy is { } appProtection)

        {

            sb.AppendLine("=== App Protection Policy ===");

            Append(sb, "Name", appProtection.DisplayName);

            Append(sb, "Description", appProtection.Description);

            Append(sb, "Type", FriendlyODataType(appProtection.OdataType));

            Append(sb, "ID", appProtection.Id);

            Append(sb, "Version", appProtection.Version?.ToString());

            Append(sb, "Last Modified", appProtection.LastModifiedDateTime?.ToString("g"));

            if (SelectedItemMinAppVersion is { Length: > 0 })

                Append(sb, "Min App Version", SelectedItemMinAppVersion);

            if (SelectedItemMinOSVersion is { Length: > 0 })

                Append(sb, "Min OS Version", SelectedItemMinOSVersion);

        }

        else if (SelectedManagedDeviceAppConfiguration is { } managedConfig)

        {

            sb.AppendLine("=== Managed Device App Configuration ===");

            Append(sb, "Name", managedConfig.DisplayName);

            Append(sb, "Description", managedConfig.Description);

            Append(sb, "ID", managedConfig.Id);

            Append(sb, "Version", managedConfig.Version?.ToString());

            Append(sb, "Last Modified", managedConfig.LastModifiedDateTime?.ToString("g"));

        }

        else if (SelectedTargetedManagedAppConfiguration is { } targetedConfig)

        {

            sb.AppendLine("=== Targeted Managed App Configuration ===");

            Append(sb, "Name", targetedConfig.DisplayName);

            Append(sb, "Description", targetedConfig.Description);

            Append(sb, "ID", targetedConfig.Id);

            Append(sb, "Version", targetedConfig.Version?.ToString());

            Append(sb, "Last Modified", targetedConfig.LastModifiedDateTime?.ToString("g"));

        }

        else if (SelectedTermsAndConditions is { } terms)

        {

            sb.AppendLine("=== Terms and Conditions ===");

            Append(sb, "Name", terms.DisplayName);

            Append(sb, "Description", terms.Description);

            Append(sb, "ID", terms.Id);

            Append(sb, "Version", terms.Version?.ToString());

            Append(sb, "Created", terms.CreatedDateTime?.ToString("g"));

            Append(sb, "Last Modified", terms.LastModifiedDateTime?.ToString("g"));

        }

        else if (SelectedScopeTag is { } scopeTag)

        {

            sb.AppendLine("=== Scope Tag ===");

            Append(sb, "Name", scopeTag.DisplayName);

            Append(sb, "Description", scopeTag.Description);

            Append(sb, "ID", scopeTag.Id);

            Append(sb, "Is Built In", scopeTag.IsBuiltIn?.ToString());

        }

        else if (SelectedRoleDefinition is { } roleDefinition)

        {

            sb.AppendLine("=== Role Definition ===");

            Append(sb, "Name", roleDefinition.DisplayName);

            Append(sb, "Description", roleDefinition.Description);

            Append(sb, "ID", roleDefinition.Id);

            Append(sb, "Is Built In", roleDefinition.IsBuiltIn?.ToString());

            Append(sb, "Is Built In Role Definition", roleDefinition.IsBuiltInRoleDefinition?.ToString());

        }

        else if (SelectedIntuneBrandingProfile is { } brandingProfile)

        {

            sb.AppendLine("=== Intune Branding Profile ===");

            Append(sb, "Display Name", brandingProfile.DisplayName);

            Append(sb, "Profile Name", brandingProfile.ProfileName);

            Append(sb, "ID", brandingProfile.Id);

            Append(sb, "Show Logo", brandingProfile.ShowLogo?.ToString());

        }

        else if (SelectedAzureBrandingLocalization is { } azureBranding)

        {

            sb.AppendLine("=== Azure Branding Localization ===");

            Append(sb, "Localization ID", azureBranding.Id);

            Append(sb, "ID", azureBranding.Id);

            Append(sb, "Sign-in Page Text", azureBranding.SignInPageText);

            Append(sb, "Username Hint Text", azureBranding.UsernameHintText);

            Append(sb, "Tenant Banner Logo Relative URL", azureBranding.BannerLogoRelativeUrl);

        }

        else if (SelectedAutopilotProfile is { } autopilot)

        {

            sb.AppendLine("=== Autopilot Profile ===");

            Append(sb, "Name", TryReadStringProperty(autopilot, "DisplayName"));

            Append(sb, "Description", TryReadStringProperty(autopilot, "Description"));

            Append(sb, "ID", autopilot.Id);

            if (SelectedItemLanguage is { Length: > 0 })

                Append(sb, "Language", SelectedItemLanguage);

        }

        else if (SelectedDeviceHealthScript is { } healthScript)

        {

            sb.AppendLine("=== Device Health Script ===");

            Append(sb, "Name", TryReadStringProperty(healthScript, "DisplayName"));

            Append(sb, "Description", TryReadStringProperty(healthScript, "Description"));

            Append(sb, "ID", healthScript.Id);

            if (SelectedItemPublisher is { Length: > 0 })

                Append(sb, "Publisher", SelectedItemPublisher);

            if (SelectedItemRunAsAccount is { Length: > 0 })

                Append(sb, "Run As Account", SelectedItemRunAsAccount);

            Append(sb, "Run As 32-bit", SelectedItemRunAs32BitText);

        }

        else if (SelectedMacCustomAttribute is { } macCustomAttribute)

        {

            sb.AppendLine("=== Mac Custom Attribute ===");

            Append(sb, "Name", TryReadStringProperty(macCustomAttribute, "DisplayName"));

            Append(sb, "Description", TryReadStringProperty(macCustomAttribute, "Description"));

            Append(sb, "ID", macCustomAttribute.Id);

        }

        else if (SelectedFeatureUpdateProfile is { } featureUpdate)

        {

            sb.AppendLine("=== Feature Update Profile ===");

            Append(sb, "Name", TryReadStringProperty(featureUpdate, "DisplayName"));

            Append(sb, "Description", TryReadStringProperty(featureUpdate, "Description"));

            Append(sb, "ID", featureUpdate.Id);

            if (SelectedItemFeatureUpdateVersion is { Length: > 0 })

                Append(sb, "Feature Update Version", SelectedItemFeatureUpdateVersion);

        }

        else if (SelectedNamedLocation is { } namedLocation)

        {

            sb.AppendLine("=== Named Location ===");

            Append(sb, "Name", TryReadStringProperty(namedLocation, "DisplayName"));

            Append(sb, "ID", namedLocation.Id);

            if (SelectedItemIpRanges.Count > 0)

                Append(sb, "IP Ranges", string.Join(", ", SelectedItemIpRanges));

            if (SelectedItemCountryCodes.Count > 0)

                Append(sb, "Country Codes", string.Join(", ", SelectedItemCountryCodes));

        }

        else if (SelectedAuthenticationStrengthPolicy is { } authStrength)

        {

            sb.AppendLine("=== Authentication Strength ===");

            Append(sb, "Name", TryReadStringProperty(authStrength, "DisplayName"));

            Append(sb, "Description", TryReadStringProperty(authStrength, "Description"));

            Append(sb, "ID", authStrength.Id);

            if (SelectedItemAllowedCombinations.Count > 0)

                Append(sb, "Allowed Combinations", string.Join(", ", SelectedItemAllowedCombinations));

        }

        else if (SelectedAuthenticationContextClassReference is { } authContext)

        {

            sb.AppendLine("=== Authentication Context ===");

            Append(sb, "Name", TryReadStringProperty(authContext, "DisplayName"));

            Append(sb, "Description", TryReadStringProperty(authContext, "Description"));

            Append(sb, "ID", authContext.Id);

        }

        else if (SelectedTermsOfUseAgreement is { } termsOfUse)

        {

            sb.AppendLine("=== Terms of Use ===");

            Append(sb, "Name", TryReadStringProperty(termsOfUse, "DisplayName"));

            Append(sb, "Description", TryReadStringProperty(termsOfUse, "Description"));

            Append(sb, "ID", termsOfUse.Id);

        }

        else if (SelectedConditionalAccessPolicy is { } cap)

        {

            sb.AppendLine("=== Conditional Access Policy ===");

            Append(sb, "Name", cap.DisplayName);

            Append(sb, "State", cap.State?.ToString());

            Append(sb, "ID", cap.Id);

        }

        else if (SelectedAssignmentFilter is { } filter)

        {

            sb.AppendLine("=== Assignment Filter ===");

            Append(sb, "Name", filter.DisplayName);

            Append(sb, "Platform", filter.Platform?.ToString());

            Append(sb, "Type", filter.AssignmentFilterManagementType?.ToString());

            Append(sb, "ID", filter.Id);

        }

        else if (SelectedPolicySet is { } policySet)

        {

            sb.AppendLine("=== Policy Set ===");

            Append(sb, "Name", policySet.DisplayName);

            Append(sb, "Description", policySet.Description);

            Append(sb, "ID", policySet.Id);

        }

        else if (SelectedDeviceManagementScript is { } dms)

        {

            sb.AppendLine("=== Device Management Script ===");

            Append(sb, "Name", dms.DisplayName);

            Append(sb, "Description", dms.Description);

            Append(sb, "File Name", dms.FileName);

            Append(sb, "Run As Account", dms.RunAsAccount?.ToString());

            Append(sb, "Run As 32-Bit", dms.RunAs32Bit?.ToString());

            Append(sb, "Enforce Signature Check", dms.EnforceSignatureCheck?.ToString());

            Append(sb, "Created", dms.CreatedDateTime?.ToString("g"));

            Append(sb, "Last Modified", dms.LastModifiedDateTime?.ToString("g"));

            Append(sb, "ID", dms.Id);

            AppendAssignments(sb);

        }

        else if (SelectedDeviceShellScript is { } dss)

        {

            sb.AppendLine("=== Device Shell Script ===");

            Append(sb, "Name", dss.DisplayName);

            Append(sb, "Description", dss.Description);

            Append(sb, "File Name", dss.FileName);

            Append(sb, "Run As Account", dss.RunAsAccount?.ToString());

            Append(sb, "Execution Frequency", dss.ExecutionFrequency?.ToString());

            Append(sb, "Retry Count", dss.RetryCount?.ToString());

            Append(sb, "Block Execution Notifications", dss.BlockExecutionNotifications?.ToString());

            Append(sb, "Created", dss.CreatedDateTime?.ToString("g"));

            Append(sb, "Last Modified", dss.LastModifiedDateTime?.ToString("g"));

            Append(sb, "ID", dss.Id);

            AppendAssignments(sb);

        }

        else if (SelectedComplianceScript is { } cs)

        {

            sb.AppendLine("=== Compliance Script ===");

            Append(sb, "Name", cs.DisplayName);

            Append(sb, "Description", cs.Description);

            Append(sb, "Created", cs.CreatedDateTime?.ToString("g"));

            Append(sb, "Last Modified", cs.LastModifiedDateTime?.ToString("g"));

            Append(sb, "ID", cs.Id);

        }

        else if (SelectedAppAssignmentRow is { } row)

        {

            sb.AppendLine("=== Application Assignment ===");

            Append(sb, "App Name", row.AppName);

            Append(sb, "App Type", row.AppType);

            Append(sb, "Platform", row.Platform);

            Append(sb, "Publisher", row.Publisher);

            Append(sb, "Version", row.Version);

            Append(sb, "Description", row.Description);

            sb.AppendLine();

            Append(sb, "Assignment Type", row.AssignmentType);

            Append(sb, "Install Intent", row.InstallIntent);

            Append(sb, "Target Name", row.TargetName);

            Append(sb, "Target Group ID", row.TargetGroupId);

            Append(sb, "Is Exclusion", row.IsExclusion);

            Append(sb, "Is Featured", row.IsFeatured);

            Append(sb, "Assignment Settings", row.AssignmentSettings);

            sb.AppendLine();

            Append(sb, "Bundle ID", row.BundleId);

            Append(sb, "Package ID", row.PackageId);

            Append(sb, "Min OS Version", row.MinimumOsVersion);

            Append(sb, "Min Disk (MB)", row.MinimumFreeDiskSpaceMB);

            Append(sb, "Min RAM (MB)", row.MinimumMemoryMB);

            Append(sb, "Min CPUs", row.MinimumProcessors);

            sb.AppendLine();

            Append(sb, "Information URL", row.InformationUrl);

            Append(sb, "Privacy URL", row.PrivacyUrl);

            Append(sb, "App Store URL", row.AppStoreUrl);

            Append(sb, "Created", row.CreatedDate);

            Append(sb, "Last Modified", row.LastModified);

            Append(sb, "Categories", row.Categories);

            Append(sb, "Notes", row.Notes);

        }

        else if (SelectedDynamicGroupRow is { } dg)

        {

            sb.AppendLine("=== Dynamic Group ===");

            Append(sb, "Group Name", dg.GroupName);

            Append(sb, "Description", dg.Description);

            Append(sb, "Membership Rule", dg.MembershipRule);

            Append(sb, "Processing State", dg.ProcessingState);

            Append(sb, "Group Type", dg.GroupType);

            Append(sb, "Total Members", dg.TotalMembers);

            Append(sb, "Users", dg.Users);

            Append(sb, "Devices", dg.Devices);

            Append(sb, "Nested Groups", dg.NestedGroups);

            Append(sb, "Security Enabled", dg.SecurityEnabled);

            Append(sb, "Mail Enabled", dg.MailEnabled);

            Append(sb, "Created Date", dg.CreatedDate);

            Append(sb, "Group ID", dg.GroupId);

        }

        else if (SelectedAssignedGroupRow is { } ag)

        {

            sb.AppendLine("=== Assigned Group ===");

            Append(sb, "Group Name", ag.GroupName);

            Append(sb, "Description", ag.Description);

            Append(sb, "Group Type", ag.GroupType);

            Append(sb, "Total Members", ag.TotalMembers);

            Append(sb, "Users", ag.Users);

            Append(sb, "Devices", ag.Devices);

            Append(sb, "Nested Groups", ag.NestedGroups);

            Append(sb, "Security Enabled", ag.SecurityEnabled);

            Append(sb, "Mail Enabled", ag.MailEnabled);

            Append(sb, "Created Date", ag.CreatedDate);

            Append(sb, "Group ID", ag.GroupId);

        }



        return sb.ToString();

    }



    private static void Append(StringBuilder sb, string label, string? value)

    {

        if (!string.IsNullOrEmpty(value))

            sb.AppendLine($"{label}: {value}");

    }



    private void AppendAssignments(StringBuilder sb)

    {

        if (SelectedItemAssignments.Count == 0) return;

        sb.AppendLine();

        sb.AppendLine("Assignments:");

        foreach (var a in SelectedItemAssignments)

        {

            var line = $"  [{a.TargetKind}] {a.Target}";

            if (!string.IsNullOrEmpty(a.Intent)) line += $" ({a.Intent})";

            if (!string.IsNullOrEmpty(a.GroupId)) line += $" [{a.GroupId}]";

            sb.AppendLine(line);

        }

    }





    private static string? TryReadStringProperty(object? instance, string propertyName)

    {

        if (instance == null) return null;

        var propertyInfo = instance.GetType().GetProperty(propertyName);

        return propertyInfo?.GetValue(instance) as string;

    }





    private static string FriendlyODataType(string? odataType)

    {

        if (string.IsNullOrEmpty(odataType)) return "";

        // OData type is like "#microsoft.graph.windows10GeneralConfiguration"

        var name = odataType.Split('.').LastOrDefault() ?? odataType;

        // Insert spaces before capitals: "windows10GeneralConfiguration" → "Windows10 General Configuration"

        var spaced = System.Text.RegularExpressions.Regex.Replace(name, "(?<=[a-z])(?=[A-Z])", " ");

        return char.ToUpper(spaced[0]) + spaced[1..];

    }

    /// <summary>
    /// Resolves a Named Location ID to its display name by looking up the loaded collection.
    /// </summary>
    private string ResolveNamedLocationId(string? id)
    {
        if (string.IsNullOrEmpty(id)) return "";
        
        var location = NamedLocations.FirstOrDefault(nl => 
            TryReadStringProperty(nl, "Id") == id);
        
        if (location != null)
        {
            var displayName = TryReadStringProperty(location, "DisplayName");
            if (!string.IsNullOrEmpty(displayName))
                return displayName;
        }
        
        return $"Unknown Location ({id})";
    }

    /// <summary>
    /// Resolves a User Group ID to its display name by looking up the loaded collection.
    /// TODO: Add a unified Groups collection for ID resolution.
    /// </summary>
    private string ResolveGroupId(string? id)
    {
        if (string.IsNullOrEmpty(id)) return "";
        
        // Check DynamicGroupRows
        var dynamicGroup = DynamicGroupRows.FirstOrDefault(g => g.GroupId == id);
        if (dynamicGroup != null && !string.IsNullOrEmpty(dynamicGroup.GroupName))
            return dynamicGroup.GroupName;
        
        // Check AssignedGroupRows
        var assignedGroup = AssignedGroupRows.FirstOrDefault(g => g.GroupId == id);
        if (assignedGroup != null && !string.IsNullOrEmpty(assignedGroup.GroupName))
            return assignedGroup.GroupName;
        
        return $"Unknown Group ({id})";
    }

    /// <summary>
    /// Well-known Microsoft app IDs and their display names.
    /// </summary>
    private static readonly System.Collections.Generic.Dictionary<string, string> WellKnownApps = new()
    {
        ["00000002-0000-0ff1-ce00-000000000000"] = "Office 365",
        ["00000003-0000-0000-c000-000000000000"] = "Microsoft Graph",
        ["0000000a-0000-0000-c000-000000000000"] = "Microsoft Intune",
        ["00000006-0000-0ff1-ce00-000000000000"] = "Microsoft Office 365 Portal",
        ["c44b4083-3bb0-49c1-b47d-974e53cbdf3c"] = "Azure Portal",
        ["89bee1f7-5e6e-4d8a-9f3d-ecd601259da7"] = "Azure Management",
        ["797f4846-ba00-4fd7-ba43-dac1f8f63013"] = "Azure Service Management",
        ["cc15fd57-2c6c-4117-a88c-83b1d56b4bbe"] = "Microsoft Teams",
        ["5e3ce6c0-2b1f-4285-8d4b-75ee78787346"] = "Microsoft Stream",
        ["00000007-0000-0ff1-ce00-000000000000"] = "Microsoft Office 365 Hybrid",
        ["4345a7b9-9a63-4910-a426-35363201d503"] = "Microsoft O365 Suite UX",
        ["28b567f6-162c-4f54-99a0-6887f387bbcc"] = "Azure DevOps",
        ["499b84ac-1321-427f-aa17-267ca6975798"] = "Microsoft Visual Studio",
        ["d3590ed6-52b3-4102-aeff-aad2292ab01c"] = "Microsoft Office",
        ["00b41c95-dab0-4487-9791-b9d2c32c80f2"] = "Office 365 Management",
        ["00000012-0000-0000-c000-000000000000"] = "Microsoft Rights Management Services",
        ["1fec8e78-bce4-4aaf-ab1b-5451cc387264"] = "Microsoft Teams Web Client",
        ["27922004-5251-4030-b22d-91ecd9a37ea4"] = "Outlook Mobile",
        ["57fb890c-0dab-4253-a5e0-7188c88b2bb4"] = "SharePoint Android",
        ["d3590ed6-52b3-4102-aeff-aad2292ab01c"] = "Office UWP PWA",
        ["93d53678-613d-4013-afc1-62e9e444a0a5"] = "Power Platform Admin Center",
        ["871c010f-5e61-4fb1-83ac-98610a7e9110"] = "Power Apps",
        ["4e291c71-d680-4d0e-9640-0a3358e31177"] = "Power Automate",
        ["a8f7a65c-f5ba-4859-b2d6-df772c264e9d"] = "Power BI Service",
        ["26a7ee05-5602-4d76-a7ba-eae8b7b67941"] = "Windows Azure Service Management API",
        ["cf53fce8-def6-4aeb-8d30-b158e7b1cf83"] = "Microsoft Password Reset Service",
        ["0000000c-0000-0000-c000-000000000000"] = "Microsoft App Access Panel",
        ["ffcb16e8-f789-467c-8ce9-f826a080d987"] = "Microsoft Bing",
        ["20a11fe0-faa8-4df5-baf2-f965f8f9972e"] = "Microsoft Exchange Online Protection",
        ["fb78d390-0c51-40cd-8e17-fdbfab77341b"] = "Microsoft Exchange Rest API Auth"
    };

    /// <summary>
    /// Resolves an application ID to a well-known display name, or returns the ID if unknown.
    /// </summary>
    private static string ResolveApplicationId(string? id)
    {
        if (string.IsNullOrEmpty(id)) return "";
        
        if (WellKnownApps.TryGetValue(id, out var appName))
            return appName;
        
        return id;
    }

    /// <summary>
    /// Decodes a Base64-encoded string (commonly used for script content).
    /// </summary>
    private static string DecodeBase64Script(string? base64Content)
    {
        if (string.IsNullOrEmpty(base64Content)) return "";
        
        try
        {
            var bytes = System.Convert.FromBase64String(base64Content);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return "(Unable to decode script content)";
        }
    }

}
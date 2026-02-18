using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IntuneManager.Core.Auth;
using IntuneManager.Core.Models;
using IntuneManager.Core.Services;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;
using Microsoft.Graph.Beta.Models.ODataErrors;

namespace IntuneManager.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ProfileService _profileService;
    private readonly IntuneGraphClientFactory _graphClientFactory;
    private readonly IExportService _exportService;
    private readonly ICacheService _cacheService;

    // Cache data-type keys
    private const string CacheKeyDeviceConfigs = "DeviceConfigurations";
    private const string CacheKeyCompliancePolicies = "CompliancePolicies";
    private const string CacheKeyApplications = "Applications";
    private const string CacheKeySettingsCatalog = "SettingsCatalog";
    private const string CacheKeyConditionalAccess = "ConditionalAccessPolicies";
    private const string CacheKeyAssignmentFilters = "AssignmentFilters";
    private const string CacheKeyPolicySets = "PolicySets";
    private const string CacheKeyEndpointSecurity = "EndpointSecurityIntents";
    private const string CacheKeyAdministrativeTemplates = "AdministrativeTemplates";
    private const string CacheKeyEnrollmentConfigurations = "EnrollmentConfigurations";
    private const string CacheKeyAppProtectionPolicies = "AppProtectionPolicies";
    private const string CacheKeyManagedDeviceAppConfigurations = "ManagedDeviceAppConfigurations";
    private const string CacheKeyTargetedManagedAppConfigurations = "TargetedManagedAppConfigurations";
    private const string CacheKeyTermsAndConditions = "TermsAndConditions";
    private const string CacheKeyScopeTags = "ScopeTags";
    private const string CacheKeyRoleDefinitions = "RoleDefinitions";
    private const string CacheKeyIntuneBrandingProfiles = "IntuneBrandingProfiles";
    private const string CacheKeyAzureBrandingLocalizations = "AzureBrandingLocalizations";
    private const string CacheKeyAppAssignments = "AppAssignments";
    private const string CacheKeyDynamicGroups = "DynamicGroups";
    private const string CacheKeyAssignedGroups = "AssignedGroups";

    private GraphServiceClient? _graphClient;
    private IConfigurationProfileService? _configProfileService;
    private IImportService? _importService;
    private ICompliancePolicyService? _compliancePolicyService;
    private IApplicationService? _applicationService;
    private IGroupService? _groupService;
    private ISettingsCatalogService? _settingsCatalogService;
    private IConditionalAccessPolicyService? _conditionalAccessPolicyService;
    private IAssignmentFilterService? _assignmentFilterService;
    private IPolicySetService? _policySetService;
    private IEndpointSecurityService? _endpointSecurityService;
    private IAdministrativeTemplateService? _administrativeTemplateService;
    private IEnrollmentConfigurationService? _enrollmentConfigurationService;
    private IAppProtectionPolicyService? _appProtectionPolicyService;
    private IManagedAppConfigurationService? _managedAppConfigurationService;
    private ITermsAndConditionsService? _termsAndConditionsService;
    private IScopeTagService? _scopeTagService;
    private IRoleDefinitionService? _roleDefinitionService;
    private IIntuneBrandingService? _intuneBrandingService;
    private IAzureBrandingService? _azureBrandingService;

    [ObservableProperty]
    private ViewModelBase? _currentView;

    [ObservableProperty]
    private string _windowTitle = "Intune Commander";

    [ObservableProperty]
    private TenantProfile? _activeProfile;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _statusText = "Not connected";

    [ObservableProperty]
    private string _cacheStatusText = "";

    partial void OnStatusTextChanged(string value)
    {
        DebugLog.Log("Status", value);
    }

    // --- Navigation ---
    [ObservableProperty]
    private NavCategory? _selectedCategory;

    public ObservableCollection<NavCategory> NavCategories { get; } =
    [
        new NavCategory { Name = "Overview", Icon = "📊" },
        new NavCategory { Name = "Device Configurations", Icon = "⚙" },
        new NavCategory { Name = "Compliance Policies", Icon = "✓" },
        new NavCategory { Name = "Applications", Icon = "📦" },
        new NavCategory { Name = "Application Assignments", Icon = "📋" },
        new NavCategory { Name = "Settings Catalog", Icon = "⚙" },
        new NavCategory { Name = "Endpoint Security", Icon = "🛡" },
        new NavCategory { Name = "Administrative Templates", Icon = "🧾" },
        new NavCategory { Name = "Enrollment Configurations", Icon = "🪪" },
        new NavCategory { Name = "App Protection Policies", Icon = "🔒" },
        new NavCategory { Name = "Managed Device App Configurations", Icon = "📱" },
        new NavCategory { Name = "Targeted Managed App Configurations", Icon = "🎯" },
        new NavCategory { Name = "Terms and Conditions", Icon = "📜" },
        new NavCategory { Name = "Scope Tags", Icon = "🏷" },
        new NavCategory { Name = "Role Definitions", Icon = "🧑‍💼" },
        new NavCategory { Name = "Intune Branding", Icon = "🎨" },
        new NavCategory { Name = "Azure Branding", Icon = "🟦" },
        new NavCategory { Name = "Conditional Access", Icon = "🔐" },
        new NavCategory { Name = "Assignment Filters", Icon = "🧩" },
        new NavCategory { Name = "Policy Sets", Icon = "🗂" },
        new NavCategory { Name = "Dynamic Groups", Icon = "🔄" },
        new NavCategory { Name = "Assigned Groups", Icon = "👥" }
    ];

    // --- Overview dashboard ---
    public OverviewViewModel Overview { get; } = new();

    // --- Device Configurations ---
    [ObservableProperty]
    private ObservableCollection<DeviceConfiguration> _deviceConfigurations = [];

    [ObservableProperty]
    private DeviceConfiguration? _selectedConfiguration;

    // --- Compliance Policies ---
    [ObservableProperty]
    private ObservableCollection<DeviceCompliancePolicy> _compliancePolicies = [];

    [ObservableProperty]
    private DeviceCompliancePolicy? _selectedCompliancePolicy;

    // --- Applications ---
    [ObservableProperty]
    private ObservableCollection<MobileApp> _applications = [];

    [ObservableProperty]
    private MobileApp? _selectedApplication;

    // --- Settings Catalog ---
    [ObservableProperty]
    private ObservableCollection<DeviceManagementConfigurationPolicy> _settingsCatalogPolicies = [];

    [ObservableProperty]
    private DeviceManagementConfigurationPolicy? _selectedSettingsCatalogPolicy;

    // --- Endpoint Security ---
    [ObservableProperty]
    private ObservableCollection<DeviceManagementIntent> _endpointSecurityIntents = [];

    [ObservableProperty]
    private DeviceManagementIntent? _selectedEndpointSecurityIntent;

    private bool _endpointSecurityLoaded;

    // --- Administrative Templates ---
    [ObservableProperty]
    private ObservableCollection<GroupPolicyConfiguration> _administrativeTemplates = [];

    [ObservableProperty]
    private GroupPolicyConfiguration? _selectedAdministrativeTemplate;

    private bool _administrativeTemplatesLoaded;

    // --- Enrollment Configurations ---
    [ObservableProperty]
    private ObservableCollection<DeviceEnrollmentConfiguration> _enrollmentConfigurations = [];

    [ObservableProperty]
    private DeviceEnrollmentConfiguration? _selectedEnrollmentConfiguration;

    private bool _enrollmentConfigurationsLoaded;

    // --- App Protection Policies ---
    [ObservableProperty]
    private ObservableCollection<ManagedAppPolicy> _appProtectionPolicies = [];

    [ObservableProperty]
    private ManagedAppPolicy? _selectedAppProtectionPolicy;

    private bool _appProtectionPoliciesLoaded;

    // --- Managed Device App Configurations ---
    [ObservableProperty]
    private ObservableCollection<ManagedDeviceMobileAppConfiguration> _managedDeviceAppConfigurations = [];

    [ObservableProperty]
    private ManagedDeviceMobileAppConfiguration? _selectedManagedDeviceAppConfiguration;

    private bool _managedDeviceAppConfigurationsLoaded;

    // --- Targeted Managed App Configurations ---
    [ObservableProperty]
    private ObservableCollection<TargetedManagedAppConfiguration> _targetedManagedAppConfigurations = [];

    [ObservableProperty]
    private TargetedManagedAppConfiguration? _selectedTargetedManagedAppConfiguration;

    private bool _targetedManagedAppConfigurationsLoaded;

    // --- Terms and Conditions ---
    [ObservableProperty]
    private ObservableCollection<TermsAndConditions> _termsAndConditionsCollection = [];

    [ObservableProperty]
    private TermsAndConditions? _selectedTermsAndConditions;

    private bool _termsAndConditionsLoaded;

    // --- Scope Tags ---
    [ObservableProperty]
    private ObservableCollection<RoleScopeTag> _scopeTags = [];

    [ObservableProperty]
    private RoleScopeTag? _selectedScopeTag;

    private bool _scopeTagsLoaded;

    // --- Role Definitions ---
    [ObservableProperty]
    private ObservableCollection<RoleDefinition> _roleDefinitions = [];

    [ObservableProperty]
    private RoleDefinition? _selectedRoleDefinition;

    private bool _roleDefinitionsLoaded;

    // --- Intune Branding ---
    [ObservableProperty]
    private ObservableCollection<IntuneBrandingProfile> _intuneBrandingProfiles = [];

    [ObservableProperty]
    private IntuneBrandingProfile? _selectedIntuneBrandingProfile;

    private bool _intuneBrandingProfilesLoaded;

    // --- Azure Branding ---
    [ObservableProperty]
    private ObservableCollection<OrganizationalBrandingLocalization> _azureBrandingLocalizations = [];

    [ObservableProperty]
    private OrganizationalBrandingLocalization? _selectedAzureBrandingLocalization;

    private bool _azureBrandingLocalizationsLoaded;

    // --- Conditional Access ---
    [ObservableProperty]
    private ObservableCollection<ConditionalAccessPolicy> _conditionalAccessPolicies = [];

    [ObservableProperty]
    private ConditionalAccessPolicy? _selectedConditionalAccessPolicy;

    private bool _conditionalAccessLoaded;

    // --- Assignment Filters ---
    [ObservableProperty]
    private ObservableCollection<DeviceAndAppManagementAssignmentFilter> _assignmentFilters = [];

    [ObservableProperty]
    private DeviceAndAppManagementAssignmentFilter? _selectedAssignmentFilter;

    private bool _assignmentFiltersLoaded;

    // --- Policy Sets ---
    [ObservableProperty]
    private ObservableCollection<PolicySet> _policySets = [];

    [ObservableProperty]
    private PolicySet? _selectedPolicySet;

    private bool _policySetsLoaded;

    // --- Application Assignments (flattened view) ---
    [ObservableProperty]
    private ObservableCollection<AppAssignmentRow> _appAssignmentRows = [];

    [ObservableProperty]
    private AppAssignmentRow? _selectedAppAssignmentRow;

    private bool _appAssignmentsLoaded;

    // --- Dynamic Groups ---
    [ObservableProperty]
    private ObservableCollection<GroupRow> _dynamicGroupRows = [];

    [ObservableProperty]
    private GroupRow? _selectedDynamicGroupRow;

    private bool _dynamicGroupsLoaded;

    // --- Assigned Groups ---
    [ObservableProperty]
    private ObservableCollection<GroupRow> _assignedGroupRows = [];

    [ObservableProperty]
    private GroupRow? _selectedAssignedGroupRow;

    private bool _assignedGroupsLoaded;

    // --- Detail pane ---
    [ObservableProperty]
    private ObservableCollection<AssignmentDisplayItem> _selectedItemAssignments = [];

    [ObservableProperty]
    private ObservableCollection<GroupMemberItem> _selectedGroupMembers = [];

    [ObservableProperty]
    private bool _isLoadingGroupMembers;

    [ObservableProperty]
    private string _selectedItemTypeName = "";

    [ObservableProperty]
    private string _selectedItemPlatform = "";

    [ObservableProperty]
    private bool _isLoadingDetails;

    /// <summary>
    /// Raised when the user clicks "Copy Details". The view handles clipboard access.
    /// </summary>
    public event Action<string>? CopyDetailsRequested;
    public event Action<string, string>? ViewRawJsonRequested;

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
            ?? SelectedConditionalAccessPolicy as object
            ?? SelectedAssignmentFilter as object
            ?? SelectedPolicySet as object;

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
            ConditionalAccessPolicy cap => cap.DisplayName ?? "Conditional Access Policy",
            DeviceAndAppManagementAssignmentFilter af => af.DisplayName ?? "Assignment Filter",
            PolicySet ps => ps.DisplayName ?? "Policy Set",
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
        else if (SelectedAppProtectionPolicy is { } appProtection)
        {
            sb.AppendLine("=== App Protection Policy ===");
            Append(sb, "Name", appProtection.DisplayName);
            Append(sb, "Description", appProtection.Description);
            Append(sb, "Type", FriendlyODataType(appProtection.OdataType));
            Append(sb, "ID", appProtection.Id);
            Append(sb, "Version", appProtection.Version?.ToString());
            Append(sb, "Last Modified", appProtection.LastModifiedDateTime?.ToString("g"));
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

    // --- Search / filter ---
    [ObservableProperty]
    private string _searchText = "";

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    /// <summary>
    /// Filtered views exposed for DataGrid binding.
    /// These are rebuilt whenever the source collection or SearchText changes.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<DeviceConfiguration> _filteredDeviceConfigurations = [];

    [ObservableProperty]
    private ObservableCollection<DeviceCompliancePolicy> _filteredCompliancePolicies = [];

    [ObservableProperty]
    private ObservableCollection<MobileApp> _filteredApplications = [];

    [ObservableProperty]
    private ObservableCollection<AppAssignmentRow> _filteredAppAssignmentRows = [];

    [ObservableProperty]
    private ObservableCollection<GroupRow> _filteredDynamicGroupRows = [];

    [ObservableProperty]
    private ObservableCollection<GroupRow> _filteredAssignedGroupRows = [];

    [ObservableProperty]
    private ObservableCollection<DeviceManagementConfigurationPolicy> _filteredSettingsCatalogPolicies = [];

    [ObservableProperty]
    private ObservableCollection<DeviceManagementIntent> _filteredEndpointSecurityIntents = [];

    [ObservableProperty]
    private ObservableCollection<GroupPolicyConfiguration> _filteredAdministrativeTemplates = [];

    [ObservableProperty]
    private ObservableCollection<DeviceEnrollmentConfiguration> _filteredEnrollmentConfigurations = [];

    [ObservableProperty]
    private ObservableCollection<ManagedAppPolicy> _filteredAppProtectionPolicies = [];

    [ObservableProperty]
    private ObservableCollection<ManagedDeviceMobileAppConfiguration> _filteredManagedDeviceAppConfigurations = [];

    [ObservableProperty]
    private ObservableCollection<TargetedManagedAppConfiguration> _filteredTargetedManagedAppConfigurations = [];

    [ObservableProperty]
    private ObservableCollection<TermsAndConditions> _filteredTermsAndConditionsCollection = [];

    [ObservableProperty]
    private ObservableCollection<RoleScopeTag> _filteredScopeTags = [];

    [ObservableProperty]
    private ObservableCollection<RoleDefinition> _filteredRoleDefinitions = [];

    [ObservableProperty]
    private ObservableCollection<IntuneBrandingProfile> _filteredIntuneBrandingProfiles = [];

    [ObservableProperty]
    private ObservableCollection<OrganizationalBrandingLocalization> _filteredAzureBrandingLocalizations = [];

    [ObservableProperty]
    private ObservableCollection<ConditionalAccessPolicy> _filteredConditionalAccessPolicies = [];

    [ObservableProperty]
    private ObservableCollection<DeviceAndAppManagementAssignmentFilter> _filteredAssignmentFilters = [];

    [ObservableProperty]
    private ObservableCollection<PolicySet> _filteredPolicySets = [];

    private void ApplyFilter()
    {
        var q = SearchText.Trim();

        if (string.IsNullOrEmpty(q))
        {
            FilteredDeviceConfigurations = new ObservableCollection<DeviceConfiguration>(DeviceConfigurations);
            FilteredCompliancePolicies = new ObservableCollection<DeviceCompliancePolicy>(CompliancePolicies);
            FilteredApplications = new ObservableCollection<MobileApp>(Applications);
            FilteredAppAssignmentRows = new ObservableCollection<AppAssignmentRow>(AppAssignmentRows);
            FilteredDynamicGroupRows = new ObservableCollection<GroupRow>(DynamicGroupRows);
            FilteredAssignedGroupRows = new ObservableCollection<GroupRow>(AssignedGroupRows);
            FilteredSettingsCatalogPolicies = new ObservableCollection<DeviceManagementConfigurationPolicy>(SettingsCatalogPolicies);
            FilteredEndpointSecurityIntents = new ObservableCollection<DeviceManagementIntent>(EndpointSecurityIntents);
            FilteredAdministrativeTemplates = new ObservableCollection<GroupPolicyConfiguration>(AdministrativeTemplates);
            FilteredEnrollmentConfigurations = new ObservableCollection<DeviceEnrollmentConfiguration>(EnrollmentConfigurations);
            FilteredAppProtectionPolicies = new ObservableCollection<ManagedAppPolicy>(AppProtectionPolicies);
            FilteredManagedDeviceAppConfigurations = new ObservableCollection<ManagedDeviceMobileAppConfiguration>(ManagedDeviceAppConfigurations);
            FilteredTargetedManagedAppConfigurations = new ObservableCollection<TargetedManagedAppConfiguration>(TargetedManagedAppConfigurations);
            FilteredTermsAndConditionsCollection = new ObservableCollection<TermsAndConditions>(TermsAndConditionsCollection);
            FilteredScopeTags = new ObservableCollection<RoleScopeTag>(ScopeTags);
            FilteredRoleDefinitions = new ObservableCollection<RoleDefinition>(RoleDefinitions);
            FilteredIntuneBrandingProfiles = new ObservableCollection<IntuneBrandingProfile>(IntuneBrandingProfiles);
            FilteredAzureBrandingLocalizations = new ObservableCollection<OrganizationalBrandingLocalization>(AzureBrandingLocalizations);
            FilteredConditionalAccessPolicies = new ObservableCollection<ConditionalAccessPolicy>(ConditionalAccessPolicies);
            FilteredAssignmentFilters = new ObservableCollection<DeviceAndAppManagementAssignmentFilter>(AssignmentFilters);
            FilteredPolicySets = new ObservableCollection<PolicySet>(PolicySets);
            return;
        }

        FilteredDeviceConfigurations = new ObservableCollection<DeviceConfiguration>(
            DeviceConfigurations.Where(c =>
                Contains(c.DisplayName, q) ||
                Contains(c.Description, q) ||
                Contains(c.OdataType, q)));

        FilteredCompliancePolicies = new ObservableCollection<DeviceCompliancePolicy>(
            CompliancePolicies.Where(p =>
                Contains(p.DisplayName, q) ||
                Contains(p.Description, q) ||
                Contains(p.OdataType, q)));

        FilteredApplications = new ObservableCollection<MobileApp>(
            Applications.Where(a =>
                Contains(a.DisplayName, q) ||
                Contains(a.Publisher, q) ||
                Contains(a.Description, q) ||
                Contains(a.OdataType, q)));

        FilteredAppAssignmentRows = new ObservableCollection<AppAssignmentRow>(
            AppAssignmentRows.Where(r =>
                Contains(r.AppName, q) ||
                Contains(r.Publisher, q) ||
                Contains(r.TargetName, q) ||
                Contains(r.AppType, q) ||
                Contains(r.Platform, q) ||
                Contains(r.InstallIntent, q)));

        FilteredDynamicGroupRows = new ObservableCollection<GroupRow>(
            DynamicGroupRows.Where(g =>
                Contains(g.GroupName, q) ||
                Contains(g.Description, q) ||
                Contains(g.MembershipRule, q) ||
                Contains(g.GroupType, q) ||
                Contains(g.GroupId, q)));

        FilteredAssignedGroupRows = new ObservableCollection<GroupRow>(
            AssignedGroupRows.Where(g =>
                Contains(g.GroupName, q) ||
                Contains(g.Description, q) ||
                Contains(g.GroupType, q) ||
                Contains(g.GroupId, q)));

        FilteredSettingsCatalogPolicies = new ObservableCollection<DeviceManagementConfigurationPolicy>(
            SettingsCatalogPolicies.Where(p =>
                Contains(p.Name, q) ||
                Contains(p.Description, q) ||
                Contains(p.Platforms?.ToString(), q) ||
                Contains(p.Technologies?.ToString(), q)));

        FilteredEndpointSecurityIntents = new ObservableCollection<DeviceManagementIntent>(
            EndpointSecurityIntents.Where(i =>
                Contains(i.DisplayName, q) ||
                Contains(i.Description, q) ||
                Contains(i.Id, q)));

        FilteredAdministrativeTemplates = new ObservableCollection<GroupPolicyConfiguration>(
            AdministrativeTemplates.Where(t =>
                Contains(t.DisplayName, q) ||
                Contains(t.Description, q) ||
                Contains(t.Id, q)));

        FilteredEnrollmentConfigurations = new ObservableCollection<DeviceEnrollmentConfiguration>(
            EnrollmentConfigurations.Where(c =>
                Contains(c.DisplayName, q) ||
                Contains(c.Description, q) ||
                Contains(c.Id, q) ||
                Contains(c.OdataType, q)));

        FilteredAppProtectionPolicies = new ObservableCollection<ManagedAppPolicy>(
            AppProtectionPolicies.Where(p =>
                Contains(p.DisplayName, q) ||
                Contains(p.Description, q) ||
                Contains(p.Id, q) ||
                Contains(p.OdataType, q)));

        FilteredManagedDeviceAppConfigurations = new ObservableCollection<ManagedDeviceMobileAppConfiguration>(
            ManagedDeviceAppConfigurations.Where(c =>
                Contains(c.DisplayName, q) ||
                Contains(c.Description, q) ||
                Contains(c.Id, q) ||
                Contains(c.OdataType, q)));

        FilteredTargetedManagedAppConfigurations = new ObservableCollection<TargetedManagedAppConfiguration>(
            TargetedManagedAppConfigurations.Where(c =>
                Contains(c.DisplayName, q) ||
                Contains(c.Description, q) ||
                Contains(c.Id, q) ||
                Contains(c.OdataType, q)));

        FilteredTermsAndConditionsCollection = new ObservableCollection<TermsAndConditions>(
            TermsAndConditionsCollection.Where(t =>
                Contains(t.DisplayName, q) ||
                Contains(t.Description, q) ||
                Contains(t.Id, q)));

        FilteredScopeTags = new ObservableCollection<RoleScopeTag>(
            ScopeTags.Where(t =>
                Contains(t.DisplayName, q) ||
                Contains(t.Description, q) ||
                Contains(t.Id, q)));

        FilteredRoleDefinitions = new ObservableCollection<RoleDefinition>(
            RoleDefinitions.Where(r =>
                Contains(r.DisplayName, q) ||
                Contains(r.Description, q) ||
                Contains(r.Id, q)));

        FilteredIntuneBrandingProfiles = new ObservableCollection<IntuneBrandingProfile>(
            IntuneBrandingProfiles.Where(b =>
                Contains(b.ProfileName, q) ||
                Contains(b.Id, q)));

        FilteredAzureBrandingLocalizations = new ObservableCollection<OrganizationalBrandingLocalization>(
            AzureBrandingLocalizations.Where(b =>
                Contains(b.Id, q) ||
                Contains(b.SignInPageText, q)));

        FilteredConditionalAccessPolicies = new ObservableCollection<ConditionalAccessPolicy>(
            ConditionalAccessPolicies.Where(p =>
                Contains(p.DisplayName, q) ||
                Contains(p.State?.ToString(), q) ||
                Contains(p.Id, q)));

        FilteredAssignmentFilters = new ObservableCollection<DeviceAndAppManagementAssignmentFilter>(
            AssignmentFilters.Where(f =>
                Contains(f.DisplayName, q) ||
                Contains(f.Platform?.ToString(), q) ||
                Contains(f.AssignmentFilterManagementType?.ToString(), q) ||
                Contains(f.Id, q)));

        FilteredPolicySets = new ObservableCollection<PolicySet>(
            PolicySets.Where(p =>
                Contains(p.DisplayName, q) ||
                Contains(p.Description, q) ||
                Contains(p.Id, q)));
    }

    private static bool Contains(string? source, string search)
        => source?.Contains(search, StringComparison.OrdinalIgnoreCase) == true;

    // --- Configurable columns per category ---
    public ObservableCollection<DataGridColumnConfig> DeviceConfigColumns { get; } =
    [
        new() { Header = "Display Name", BindingPath = "DisplayName", IsStar = true, IsVisible = true },
        new() { Header = "Type", BindingPath = "Computed:ODataType", Width = 180, IsVisible = true },
        new() { Header = "Platform", BindingPath = "Computed:Platform", Width = 100, IsVisible = true },
        new() { Header = "Description", BindingPath = "Description", Width = 200, IsVisible = false },
        new() { Header = "Created", BindingPath = "CreatedDateTime", Width = 150, IsVisible = false },
        new() { Header = "Last Modified", BindingPath = "LastModifiedDateTime", Width = 150, IsVisible = true },
        new() { Header = "Version", BindingPath = "Version", Width = 80, IsVisible = false }
    ];

    public ObservableCollection<DataGridColumnConfig> CompliancePolicyColumns { get; } =
    [
        new() { Header = "Display Name", BindingPath = "DisplayName", IsStar = true, IsVisible = true },
        new() { Header = "Type", BindingPath = "Computed:ODataType", Width = 180, IsVisible = true },
        new() { Header = "Platform", BindingPath = "Computed:Platform", Width = 100, IsVisible = true },
        new() { Header = "Description", BindingPath = "Description", Width = 200, IsVisible = false },
        new() { Header = "Created", BindingPath = "CreatedDateTime", Width = 150, IsVisible = false },
        new() { Header = "Last Modified", BindingPath = "LastModifiedDateTime", Width = 150, IsVisible = true },
        new() { Header = "Version", BindingPath = "Version", Width = 80, IsVisible = false }
    ];

    public ObservableCollection<DataGridColumnConfig> ApplicationColumns { get; } =
    [
        new() { Header = "Display Name", BindingPath = "DisplayName", IsStar = true, IsVisible = true },
        new() { Header = "App Type", BindingPath = "Computed:ODataType", Width = 180, IsVisible = true },
        new() { Header = "Platform", BindingPath = "Computed:Platform", Width = 120, IsVisible = true },
        new() { Header = "Publisher", BindingPath = "Publisher", Width = 150, IsVisible = true },
        new() { Header = "Description", BindingPath = "Description", Width = 200, IsVisible = false },
        new() { Header = "Owner", BindingPath = "Owner", Width = 120, IsVisible = false },
        new() { Header = "Developer", BindingPath = "Developer", Width = 120, IsVisible = false },
        new() { Header = "Created", BindingPath = "CreatedDateTime", Width = 150, IsVisible = false },
        new() { Header = "Last Modified", BindingPath = "LastModifiedDateTime", Width = 150, IsVisible = false },
        new() { Header = "Publishing State", BindingPath = "PublishingState", Width = 120, IsVisible = false }
    ];

    public ObservableCollection<DataGridColumnConfig> SettingsCatalogColumns { get; } =
    [
        new() { Header = "Name", BindingPath = "Name", IsStar = true, IsVisible = true },
        new() { Header = "Platforms", BindingPath = "Platforms", Width = 120, IsVisible = true },
        new() { Header = "Technologies", BindingPath = "Technologies", Width = 140, IsVisible = true },
        new() { Header = "Description", BindingPath = "Description", Width = 200, IsVisible = false },
        new() { Header = "Is Assigned", BindingPath = "IsAssigned", Width = 90, IsVisible = true },
        new() { Header = "Created", BindingPath = "CreatedDateTime", Width = 150, IsVisible = false },
        new() { Header = "Last Modified", BindingPath = "LastModifiedDateTime", Width = 150, IsVisible = true },
        new() { Header = "Role Scope Tags", BindingPath = "Computed:RoleScopeTags", Width = 120, IsVisible = false }
    ];

    public ObservableCollection<DataGridColumnConfig> EndpointSecurityColumns { get; } =
    [
        new() { Header = "Display Name", BindingPath = "DisplayName", IsStar = true, IsVisible = true },
        new() { Header = "Description", BindingPath = "Description", Width = 260, IsVisible = true },
        new() { Header = "Is Assigned", BindingPath = "IsAssigned", Width = 90, IsVisible = true },
        new() { Header = "Last Modified", BindingPath = "LastModifiedDateTime", Width = 150, IsVisible = true },
        new() { Header = "ID", BindingPath = "Id", Width = 280, IsVisible = false }
    ];

    public ObservableCollection<DataGridColumnConfig> AdministrativeTemplateColumns { get; } =
    [
        new() { Header = "Display Name", BindingPath = "DisplayName", IsStar = true, IsVisible = true },
        new() { Header = "Description", BindingPath = "Description", Width = 260, IsVisible = true },
        new() { Header = "Ingestion Type", BindingPath = "PolicyConfigurationIngestionType", Width = 140, IsVisible = true },
        new() { Header = "Last Modified", BindingPath = "LastModifiedDateTime", Width = 150, IsVisible = true },
        new() { Header = "ID", BindingPath = "Id", Width = 280, IsVisible = false }
    ];

    public ObservableCollection<DataGridColumnConfig> EnrollmentConfigurationColumns { get; } =
    [
        new() { Header = "Display Name", BindingPath = "DisplayName", IsStar = true, IsVisible = true },
        new() { Header = "Description", BindingPath = "Description", Width = 260, IsVisible = true },
        new() { Header = "Type", BindingPath = "Computed:ODataType", Width = 200, IsVisible = true },
        new() { Header = "Priority", BindingPath = "Priority", Width = 90, IsVisible = true },
        new() { Header = "ID", BindingPath = "Id", Width = 280, IsVisible = false }
    ];

    public ObservableCollection<DataGridColumnConfig> AppProtectionPolicyColumns { get; } =
    [
        new() { Header = "Display Name", BindingPath = "DisplayName", IsStar = true, IsVisible = true },
        new() { Header = "Description", BindingPath = "Description", Width = 260, IsVisible = true },
        new() { Header = "Type", BindingPath = "Computed:ODataType", Width = 220, IsVisible = true },
        new() { Header = "Version", BindingPath = "Version", Width = 90, IsVisible = true },
        new() { Header = "Last Modified", BindingPath = "LastModifiedDateTime", Width = 150, IsVisible = true },
        new() { Header = "ID", BindingPath = "Id", Width = 280, IsVisible = false }
    ];

    public ObservableCollection<DataGridColumnConfig> ManagedDeviceAppConfigurationColumns { get; } =
    [
        new() { Header = "Display Name", BindingPath = "DisplayName", IsStar = true, IsVisible = true },
        new() { Header = "Description", BindingPath = "Description", Width = 260, IsVisible = true },
        new() { Header = "Type", BindingPath = "Computed:ODataType", Width = 220, IsVisible = true },
        new() { Header = "Version", BindingPath = "Version", Width = 90, IsVisible = true },
        new() { Header = "Last Modified", BindingPath = "LastModifiedDateTime", Width = 150, IsVisible = true },
        new() { Header = "ID", BindingPath = "Id", Width = 280, IsVisible = false }
    ];

    public ObservableCollection<DataGridColumnConfig> TargetedManagedAppConfigurationColumns { get; } =
    [
        new() { Header = "Display Name", BindingPath = "DisplayName", IsStar = true, IsVisible = true },
        new() { Header = "Description", BindingPath = "Description", Width = 260, IsVisible = true },
        new() { Header = "Type", BindingPath = "Computed:ODataType", Width = 220, IsVisible = true },
        new() { Header = "Version", BindingPath = "Version", Width = 90, IsVisible = true },
        new() { Header = "Last Modified", BindingPath = "LastModifiedDateTime", Width = 150, IsVisible = true },
        new() { Header = "ID", BindingPath = "Id", Width = 280, IsVisible = false }
    ];

    public ObservableCollection<DataGridColumnConfig> TermsAndConditionsColumns { get; } =
    [
        new() { Header = "Display Name", BindingPath = "DisplayName", IsStar = true, IsVisible = true },
        new() { Header = "Description", BindingPath = "Description", Width = 260, IsVisible = true },
        new() { Header = "Version", BindingPath = "Version", Width = 90, IsVisible = true },
        new() { Header = "Created", BindingPath = "CreatedDateTime", Width = 150, IsVisible = true },
        new() { Header = "Last Modified", BindingPath = "LastModifiedDateTime", Width = 150, IsVisible = true },
        new() { Header = "ID", BindingPath = "Id", Width = 280, IsVisible = false }
    ];

    public ObservableCollection<DataGridColumnConfig> ScopeTagColumns { get; } =
    [
        new() { Header = "Display Name", BindingPath = "DisplayName", IsStar = true, IsVisible = true },
        new() { Header = "Description", BindingPath = "Description", Width = 260, IsVisible = true },
        new() { Header = "Built In", BindingPath = "IsBuiltIn", Width = 90, IsVisible = true },
        new() { Header = "ID", BindingPath = "Id", Width = 280, IsVisible = false }
    ];

    public ObservableCollection<DataGridColumnConfig> RoleDefinitionColumns { get; } =
    [
        new() { Header = "Display Name", BindingPath = "DisplayName", IsStar = true, IsVisible = true },
        new() { Header = "Description", BindingPath = "Description", Width = 260, IsVisible = true },
        new() { Header = "Built In", BindingPath = "IsBuiltIn", Width = 90, IsVisible = true },
        new() { Header = "Built In Role Definition", BindingPath = "IsBuiltInRoleDefinition", Width = 180, IsVisible = true },
        new() { Header = "ID", BindingPath = "Id", Width = 280, IsVisible = false }
    ];

    public ObservableCollection<DataGridColumnConfig> IntuneBrandingColumns { get; } =
    [
        new() { Header = "Display Name", BindingPath = "DisplayName", IsStar = true, IsVisible = true },
        new() { Header = "Profile Name", BindingPath = "ProfileName", IsStar = true, IsVisible = true },
        new() { Header = "Show Logo", BindingPath = "ShowLogo", Width = 110, IsVisible = true },
        new() { Header = "ID", BindingPath = "Id", Width = 280, IsVisible = false }
    ];

    public ObservableCollection<DataGridColumnConfig> AzureBrandingColumns { get; } =
    [
        new() { Header = "Localization ID", BindingPath = "Id", Width = 140, IsVisible = true },
        new() { Header = "Sign-in Text", BindingPath = "SignInPageText", IsStar = true, IsVisible = true },
        new() { Header = "Username Hint", BindingPath = "UsernameHintText", Width = 220, IsVisible = true },
        new() { Header = "Banner Logo URL", BindingPath = "BannerLogoRelativeUrl", Width = 220, IsVisible = false },
        new() { Header = "ID", BindingPath = "Id", Width = 280, IsVisible = false }
    ];

    public ObservableCollection<DataGridColumnConfig> ConditionalAccessColumns { get; } =
    [
        new() { Header = "Display Name", BindingPath = "DisplayName", IsStar = true, IsVisible = true },
        new() { Header = "State", BindingPath = "State", Width = 120, IsVisible = true },
        new() { Header = "ID", BindingPath = "Id", Width = 280, IsVisible = false }
    ];

    public ObservableCollection<DataGridColumnConfig> AssignmentFilterColumns { get; } =
    [
        new() { Header = "Display Name", BindingPath = "DisplayName", IsStar = true, IsVisible = true },
        new() { Header = "Platform", BindingPath = "Platform", Width = 120, IsVisible = true },
        new() { Header = "Type", BindingPath = "AssignmentFilterManagementType", Width = 140, IsVisible = true },
        new() { Header = "ID", BindingPath = "Id", Width = 280, IsVisible = false }
    ];

    public ObservableCollection<DataGridColumnConfig> PolicySetColumns { get; } =
    [
        new() { Header = "Display Name", BindingPath = "DisplayName", IsStar = true, IsVisible = true },
        new() { Header = "Description", BindingPath = "Description", Width = 240, IsVisible = true },
        new() { Header = "ID", BindingPath = "Id", Width = 280, IsVisible = false }
    ];

    public ObservableCollection<DataGridColumnConfig> DynamicGroupColumns { get; } =
    [
        new() { Header = "Group Name", BindingPath = "GroupName", IsStar = true, IsVisible = true },
        new() { Header = "Description", BindingPath = "Description", Width = 200, IsVisible = false },
        new() { Header = "Membership Rule", BindingPath = "MembershipRule", Width = 280, IsVisible = true },
        new() { Header = "Processing State", BindingPath = "ProcessingState", Width = 120, IsVisible = true },
        new() { Header = "Group Type", BindingPath = "GroupType", Width = 130, IsVisible = true },
        new() { Header = "Total Members", BindingPath = "TotalMembers", Width = 100, IsVisible = true },
        new() { Header = "Users", BindingPath = "Users", Width = 70, IsVisible = true },
        new() { Header = "Devices", BindingPath = "Devices", Width = 70, IsVisible = true },
        new() { Header = "Nested Groups", BindingPath = "NestedGroups", Width = 100, IsVisible = false },
        new() { Header = "Security Enabled", BindingPath = "SecurityEnabled", Width = 110, IsVisible = false },
        new() { Header = "Mail Enabled", BindingPath = "MailEnabled", Width = 100, IsVisible = false },
        new() { Header = "Created Date", BindingPath = "CreatedDate", Width = 150, IsVisible = false },
        new() { Header = "Group ID", BindingPath = "GroupId", Width = 280, IsVisible = false }
    ];

    public ObservableCollection<DataGridColumnConfig> AssignedGroupColumns { get; } =
    [
        new() { Header = "Group Name", BindingPath = "GroupName", IsStar = true, IsVisible = true },
        new() { Header = "Description", BindingPath = "Description", Width = 200, IsVisible = false },
        new() { Header = "Group Type", BindingPath = "GroupType", Width = 130, IsVisible = true },
        new() { Header = "Total Members", BindingPath = "TotalMembers", Width = 100, IsVisible = true },
        new() { Header = "Users", BindingPath = "Users", Width = 70, IsVisible = true },
        new() { Header = "Devices", BindingPath = "Devices", Width = 70, IsVisible = true },
        new() { Header = "Nested Groups", BindingPath = "NestedGroups", Width = 100, IsVisible = false },
        new() { Header = "Security Enabled", BindingPath = "SecurityEnabled", Width = 110, IsVisible = false },
        new() { Header = "Mail Enabled", BindingPath = "MailEnabled", Width = 100, IsVisible = false },
        new() { Header = "Created Date", BindingPath = "CreatedDate", Width = 150, IsVisible = false },
        new() { Header = "Group ID", BindingPath = "GroupId", Width = 280, IsVisible = false }
    ];

    public ObservableCollection<DataGridColumnConfig> AppAssignmentColumns { get; } =
    [
        new() { Header = "App Name", BindingPath = "AppName", IsStar = true, IsVisible = true },
        new() { Header = "App Type", BindingPath = "AppType", Width = 140, IsVisible = true },
        new() { Header = "Platform", BindingPath = "Platform", Width = 90, IsVisible = true },
        new() { Header = "Assignment Type", BindingPath = "AssignmentType", Width = 110, IsVisible = true },
        new() { Header = "Target Name", BindingPath = "TargetName", Width = 180, IsVisible = true },
        new() { Header = "Install Intent", BindingPath = "InstallIntent", Width = 90, IsVisible = true },
        new() { Header = "Is Exclusion", BindingPath = "IsExclusion", Width = 80, IsVisible = true },
        new() { Header = "Publisher", BindingPath = "Publisher", Width = 140, IsVisible = false },
        new() { Header = "Version", BindingPath = "Version", Width = 100, IsVisible = false },
        new() { Header = "Assignment Settings", BindingPath = "AssignmentSettings", Width = 260, IsVisible = false },
        new() { Header = "Target Group ID", BindingPath = "TargetGroupId", Width = 280, IsVisible = false },
        new() { Header = "Description", BindingPath = "Description", Width = 250, IsVisible = false },
        new() { Header = "Bundle ID", BindingPath = "BundleId", Width = 160, IsVisible = false },
        new() { Header = "Package ID", BindingPath = "PackageId", Width = 160, IsVisible = false },
        new() { Header = "Is Featured", BindingPath = "IsFeatured", Width = 80, IsVisible = false },
        new() { Header = "Created Date", BindingPath = "CreatedDate", Width = 150, IsVisible = false },
        new() { Header = "Last Modified", BindingPath = "LastModified", Width = 150, IsVisible = false },
        new() { Header = "App Store URL", BindingPath = "AppStoreUrl", Width = 200, IsVisible = false },
        new() { Header = "Privacy URL", BindingPath = "PrivacyUrl", Width = 200, IsVisible = false },
        new() { Header = "Information URL", BindingPath = "InformationUrl", Width = 200, IsVisible = false },
        new() { Header = "Min OS Version", BindingPath = "MinimumOsVersion", Width = 120, IsVisible = false },
        new() { Header = "Min Disk (MB)", BindingPath = "MinimumFreeDiskSpaceMB", Width = 100, IsVisible = false },
        new() { Header = "Min Memory (MB)", BindingPath = "MinimumMemoryMB", Width = 100, IsVisible = false },
        new() { Header = "Min Processors", BindingPath = "MinimumProcessors", Width = 100, IsVisible = false },
        new() { Header = "Categories", BindingPath = "Categories", Width = 180, IsVisible = false },
        new() { Header = "Notes", BindingPath = "Notes", Width = 200, IsVisible = false }
    ];

    /// <summary>
    /// Returns the column configs for the currently selected nav category.
    /// </summary>
    public ObservableCollection<DataGridColumnConfig>? ActiveColumns => SelectedCategory?.Name switch
    {
        "Device Configurations" => DeviceConfigColumns,
        "Compliance Policies" => CompliancePolicyColumns,
        "Applications" => ApplicationColumns,
        "Application Assignments" => AppAssignmentColumns,
        "Settings Catalog" => SettingsCatalogColumns,
        "Endpoint Security" => EndpointSecurityColumns,
        "Administrative Templates" => AdministrativeTemplateColumns,
        "Enrollment Configurations" => EnrollmentConfigurationColumns,
        "App Protection Policies" => AppProtectionPolicyColumns,
        "Managed Device App Configurations" => ManagedDeviceAppConfigurationColumns,
        "Targeted Managed App Configurations" => TargetedManagedAppConfigurationColumns,
        "Terms and Conditions" => TermsAndConditionsColumns,
        "Scope Tags" => ScopeTagColumns,
        "Role Definitions" => RoleDefinitionColumns,
        "Intune Branding" => IntuneBrandingColumns,
        "Azure Branding" => AzureBrandingColumns,
        "Conditional Access" => ConditionalAccessColumns,
        "Assignment Filters" => AssignmentFilterColumns,
        "Policy Sets" => PolicySetColumns,
        "Dynamic Groups" => DynamicGroupColumns,
        "Assigned Groups" => AssignedGroupColumns,
        _ => null
    };

    /// <summary>
    /// Maps an OData type string to an inferred platform name.
    /// </summary>
    public static string InferPlatform(string? odataType)
    {
        if (string.IsNullOrEmpty(odataType)) return "";
        var lower = odataType.ToLowerInvariant();
        if (lower.Contains("windows") || lower.Contains("win32") || lower.Contains("msi")) return "Windows";
        if (lower.Contains("ios") || lower.Contains("iphone")) return "iOS";
        if (lower.Contains("macos") || lower.Contains("mac")) return "macOS";
        if (lower.Contains("android")) return "Android";
        if (lower.Contains("webapp")) return "Web";
        return "Cross-platform";
    }

    // --- Profile switcher ---
    [ObservableProperty]
    private TenantProfile? _selectedSwitchProfile;

    public ObservableCollection<TenantProfile> SwitcherProfiles { get; } = [];

    public event Func<TenantProfile, Task<bool>>? SwitchProfileRequested;

    public LoginViewModel LoginViewModel { get; }

    public MainWindowViewModel(
        ProfileService profileService,
        IntuneGraphClientFactory graphClientFactory,
        IExportService exportService,
        ICacheService cacheService)
    {
        _profileService = profileService;
        _graphClientFactory = graphClientFactory;
        _exportService = exportService;
        _cacheService = cacheService;

        LoginViewModel = new LoginViewModel(profileService, graphClientFactory);
        LoginViewModel.LoginSucceeded += OnLoginSucceeded;

        CurrentView = LoginViewModel;

        DebugLog.Log("App", "Intune Commander started");

        // Load profiles asynchronously — never block the UI thread
        _ = LoadProfilesAsync();
    }

    private async Task LoadProfilesAsync()
    {
        try
        {
            await _profileService.LoadAsync();
            LoginViewModel.PopulateSavedProfiles();
            LoginViewModel.SelectActiveProfile();
        }
        catch (Exception ex)
        {
            SetError($"Failed to load profiles: {ex.Message}");
        }
    }

    // Design-time constructor
    public MainWindowViewModel()
    {
        _profileService = null!;
        _graphClientFactory = null!;
        _exportService = null!;
        _cacheService = null!;
        LoginViewModel = null!;
    }

    // --- Navigation ---

    // Computed visibility helpers for the view
    public bool IsOverviewCategory => SelectedCategory?.Name == "Overview";
    public bool IsDeviceConfigCategory => SelectedCategory?.Name == "Device Configurations";
    public bool IsCompliancePolicyCategory => SelectedCategory?.Name == "Compliance Policies";
    public bool IsApplicationCategory => SelectedCategory?.Name == "Applications";
    public bool IsAppAssignmentsCategory => SelectedCategory?.Name == "Application Assignments";
    public bool IsSettingsCatalogCategory => SelectedCategory?.Name == "Settings Catalog";
    public bool IsEndpointSecurityCategory => SelectedCategory?.Name == "Endpoint Security";
    public bool IsAdministrativeTemplatesCategory => SelectedCategory?.Name == "Administrative Templates";
    public bool IsEnrollmentConfigurationsCategory => SelectedCategory?.Name == "Enrollment Configurations";
    public bool IsAppProtectionPoliciesCategory => SelectedCategory?.Name == "App Protection Policies";
    public bool IsManagedDeviceAppConfigurationsCategory => SelectedCategory?.Name == "Managed Device App Configurations";
    public bool IsTargetedManagedAppConfigurationsCategory => SelectedCategory?.Name == "Targeted Managed App Configurations";
    public bool IsTermsAndConditionsCategory => SelectedCategory?.Name == "Terms and Conditions";
    public bool IsScopeTagsCategory => SelectedCategory?.Name == "Scope Tags";
    public bool IsRoleDefinitionsCategory => SelectedCategory?.Name == "Role Definitions";
    public bool IsIntuneBrandingCategory => SelectedCategory?.Name == "Intune Branding";
    public bool IsAzureBrandingCategory => SelectedCategory?.Name == "Azure Branding";
    public bool IsConditionalAccessCategory => SelectedCategory?.Name == "Conditional Access";
    public bool IsAssignmentFiltersCategory => SelectedCategory?.Name == "Assignment Filters";
    public bool IsPolicySetsCategory => SelectedCategory?.Name == "Policy Sets";
    public bool IsDynamicGroupsCategory => SelectedCategory?.Name == "Dynamic Groups";
    public bool IsAssignedGroupsCategory => SelectedCategory?.Name == "Assigned Groups";

    partial void OnSelectedCategoryChanged(NavCategory? value)
    {
        // Clear selections when switching categories
        SelectedConfiguration = null;
        SelectedCompliancePolicy = null;
        SelectedApplication = null;
        SelectedAppAssignmentRow = null;
        SelectedSettingsCatalogPolicy = null;
        SelectedEndpointSecurityIntent = null;
        SelectedAdministrativeTemplate = null;
        SelectedEnrollmentConfiguration = null;
        SelectedAppProtectionPolicy = null;
        SelectedManagedDeviceAppConfiguration = null;
        SelectedTargetedManagedAppConfiguration = null;
        SelectedTermsAndConditions = null;
        SelectedScopeTag = null;
        SelectedRoleDefinition = null;
        SelectedIntuneBrandingProfile = null;
        SelectedAzureBrandingLocalization = null;
        SelectedConditionalAccessPolicy = null;
        SelectedAssignmentFilter = null;
        SelectedPolicySet = null;
        SelectedDynamicGroupRow = null;
        SelectedAssignedGroupRow = null;
        SelectedItemAssignments.Clear();
        SelectedGroupMembers.Clear();
        SelectedItemTypeName = "";
        SelectedItemPlatform = "";

        // Notify category/column changes BEFORE resetting SearchText.
        // SearchText = "" triggers ApplyFilter → data rebinding, and columns
        // must already reflect the new category to avoid binding mismatches
        // (e.g. AppAssignmentColumns applied to MobileApp objects).
        OnPropertyChanged(nameof(IsOverviewCategory));
        OnPropertyChanged(nameof(IsDeviceConfigCategory));
        OnPropertyChanged(nameof(IsCompliancePolicyCategory));
        OnPropertyChanged(nameof(IsApplicationCategory));
        OnPropertyChanged(nameof(IsAppAssignmentsCategory));
        OnPropertyChanged(nameof(IsSettingsCatalogCategory));
        OnPropertyChanged(nameof(IsEndpointSecurityCategory));
        OnPropertyChanged(nameof(IsAdministrativeTemplatesCategory));
        OnPropertyChanged(nameof(IsEnrollmentConfigurationsCategory));
        OnPropertyChanged(nameof(IsAppProtectionPoliciesCategory));
        OnPropertyChanged(nameof(IsManagedDeviceAppConfigurationsCategory));
        OnPropertyChanged(nameof(IsTargetedManagedAppConfigurationsCategory));
        OnPropertyChanged(nameof(IsTermsAndConditionsCategory));
        OnPropertyChanged(nameof(IsScopeTagsCategory));
        OnPropertyChanged(nameof(IsRoleDefinitionsCategory));
        OnPropertyChanged(nameof(IsIntuneBrandingCategory));
        OnPropertyChanged(nameof(IsAzureBrandingCategory));
        OnPropertyChanged(nameof(IsConditionalAccessCategory));
        OnPropertyChanged(nameof(IsAssignmentFiltersCategory));
        OnPropertyChanged(nameof(IsPolicySetsCategory));
        OnPropertyChanged(nameof(IsDynamicGroupsCategory));
        OnPropertyChanged(nameof(IsAssignedGroupsCategory));
        OnPropertyChanged(nameof(ActiveColumns));
        OnPropertyChanged(nameof(CanRefreshSelectedItem));

        SearchText = "";

        // Lazy-load assignments when navigating to tabs that require them
        if ((value?.Name == "Application Assignments" || value?.Name == "Overview") && !_appAssignmentsLoaded)
        {
            if (!TryLoadLazyCacheEntry<AppAssignmentRow>(CacheKeyAppAssignments, rows =>
            {
                AppAssignmentRows = new ObservableCollection<AppAssignmentRow>(rows);
                _appAssignmentsLoaded = true;
                ApplyFilter();
                StatusText = $"Loaded {rows.Count} app assignment(s) from cache";

                // Update Overview dashboard when assignments loaded from cache
                Overview.Update(
                    ActiveProfile,
                    (IReadOnlyList<DeviceConfiguration>)DeviceConfigurations,
                    (IReadOnlyList<DeviceCompliancePolicy>)CompliancePolicies,
                    (IReadOnlyList<MobileApp>)Applications,
                    (IReadOnlyList<AppAssignmentRow>)AppAssignmentRows);
            }))
            {
                _ = LoadAppAssignmentRowsAsync();
            }
        }

        // Lazy-load group views
        if (value?.Name == "Dynamic Groups" && !_dynamicGroupsLoaded)
        {
            if (!TryLoadLazyCacheEntry<GroupRow>(CacheKeyDynamicGroups, rows =>
            {
                DynamicGroupRows = new ObservableCollection<GroupRow>(rows);
                _dynamicGroupsLoaded = true;
                ApplyFilter();
                StatusText = $"Loaded {rows.Count} dynamic group(s) from cache";
            }))
            {
                _ = LoadDynamicGroupRowsAsync();
            }
        }
        if (value?.Name == "Assigned Groups" && !_assignedGroupsLoaded)
        {
            if (!TryLoadLazyCacheEntry<GroupRow>(CacheKeyAssignedGroups, rows =>
            {
                AssignedGroupRows = new ObservableCollection<GroupRow>(rows);
                _assignedGroupsLoaded = true;
                ApplyFilter();
                StatusText = $"Loaded {rows.Count} assigned group(s) from cache";
            }))
            {
                _ = LoadAssignedGroupRowsAsync();
            }
        }

        if (value?.Name == "Conditional Access" && !_conditionalAccessLoaded)
        {
            if (!TryLoadLazyCacheEntry<ConditionalAccessPolicy>(CacheKeyConditionalAccess, rows =>
            {
                ConditionalAccessPolicies = new ObservableCollection<ConditionalAccessPolicy>(rows);
                _conditionalAccessLoaded = true;
                ApplyFilter();
                StatusText = $"Loaded {rows.Count} conditional access policy(ies) from cache";
            }))
            {
                _ = LoadConditionalAccessPoliciesAsync();
            }
        }

        if (value?.Name == "Assignment Filters" && !_assignmentFiltersLoaded)
        {
            if (!TryLoadLazyCacheEntry<DeviceAndAppManagementAssignmentFilter>(CacheKeyAssignmentFilters, rows =>
            {
                AssignmentFilters = new ObservableCollection<DeviceAndAppManagementAssignmentFilter>(rows);
                _assignmentFiltersLoaded = true;
                ApplyFilter();
                StatusText = $"Loaded {rows.Count} assignment filter(s) from cache";
            }))
            {
                _ = LoadAssignmentFiltersAsync();
            }
        }

        if (value?.Name == "Policy Sets" && !_policySetsLoaded)
        {
            if (!TryLoadLazyCacheEntry<PolicySet>(CacheKeyPolicySets, rows =>
            {
                PolicySets = new ObservableCollection<PolicySet>(rows);
                _policySetsLoaded = true;
                ApplyFilter();
                StatusText = $"Loaded {rows.Count} policy set(s) from cache";
            }))
            {
                _ = LoadPolicySetsAsync();
            }
        }

        if (value?.Name == "Endpoint Security" && !_endpointSecurityLoaded)
        {
            if (!TryLoadLazyCacheEntry<DeviceManagementIntent>(CacheKeyEndpointSecurity, rows =>
            {
                EndpointSecurityIntents = new ObservableCollection<DeviceManagementIntent>(rows);
                _endpointSecurityLoaded = true;
                ApplyFilter();
                StatusText = $"Loaded {rows.Count} endpoint security intent(s) from cache";
            }))
            {
                _ = LoadEndpointSecurityIntentsAsync();
            }
        }

        if (value?.Name == "Administrative Templates" && !_administrativeTemplatesLoaded)
        {
            if (!TryLoadLazyCacheEntry<GroupPolicyConfiguration>(CacheKeyAdministrativeTemplates, rows =>
            {
                AdministrativeTemplates = new ObservableCollection<GroupPolicyConfiguration>(rows);
                _administrativeTemplatesLoaded = true;
                ApplyFilter();
                StatusText = $"Loaded {rows.Count} administrative template(s) from cache";
            }))
            {
                _ = LoadAdministrativeTemplatesAsync();
            }
        }

        if (value?.Name == "Enrollment Configurations" && !_enrollmentConfigurationsLoaded)
        {
            if (!TryLoadLazyCacheEntry<DeviceEnrollmentConfiguration>(CacheKeyEnrollmentConfigurations, rows =>
            {
                EnrollmentConfigurations = new ObservableCollection<DeviceEnrollmentConfiguration>(rows);
                _enrollmentConfigurationsLoaded = true;
                ApplyFilter();
                StatusText = $"Loaded {rows.Count} enrollment configuration(s) from cache";
            }))
            {
                _ = LoadEnrollmentConfigurationsAsync();
            }
        }

        if (value?.Name == "App Protection Policies" && !_appProtectionPoliciesLoaded)
        {
            if (!TryLoadLazyCacheEntry<ManagedAppPolicy>(CacheKeyAppProtectionPolicies, rows =>
            {
                AppProtectionPolicies = new ObservableCollection<ManagedAppPolicy>(rows);
                _appProtectionPoliciesLoaded = true;
                ApplyFilter();
                StatusText = $"Loaded {rows.Count} app protection policy(ies) from cache";
            }))
            {
                _ = LoadAppProtectionPoliciesAsync();
            }
        }

        if (value?.Name == "Managed Device App Configurations" && !_managedDeviceAppConfigurationsLoaded)
        {
            if (!TryLoadLazyCacheEntry<ManagedDeviceMobileAppConfiguration>(CacheKeyManagedDeviceAppConfigurations, rows =>
            {
                ManagedDeviceAppConfigurations = new ObservableCollection<ManagedDeviceMobileAppConfiguration>(rows);
                _managedDeviceAppConfigurationsLoaded = true;
                ApplyFilter();
                StatusText = $"Loaded {rows.Count} managed device app configuration(s) from cache";
            }))
            {
                _ = LoadManagedDeviceAppConfigurationsAsync();
            }
        }

        if (value?.Name == "Targeted Managed App Configurations" && !_targetedManagedAppConfigurationsLoaded)
        {
            if (!TryLoadLazyCacheEntry<TargetedManagedAppConfiguration>(CacheKeyTargetedManagedAppConfigurations, rows =>
            {
                TargetedManagedAppConfigurations = new ObservableCollection<TargetedManagedAppConfiguration>(rows);
                _targetedManagedAppConfigurationsLoaded = true;
                ApplyFilter();
                StatusText = $"Loaded {rows.Count} targeted managed app configuration(s) from cache";
            }))
            {
                _ = LoadTargetedManagedAppConfigurationsAsync();
            }
        }

        if (value?.Name == "Terms and Conditions" && !_termsAndConditionsLoaded)
        {
            if (!TryLoadLazyCacheEntry<TermsAndConditions>(CacheKeyTermsAndConditions, rows =>
            {
                TermsAndConditionsCollection = new ObservableCollection<TermsAndConditions>(rows);
                _termsAndConditionsLoaded = true;
                ApplyFilter();
                StatusText = $"Loaded {rows.Count} terms and conditions item(s) from cache";
            }))
            {
                _ = LoadTermsAndConditionsAsync();
            }
        }

        if (value?.Name == "Scope Tags" && !_scopeTagsLoaded)
        {
            if (!TryLoadLazyCacheEntry<RoleScopeTag>(CacheKeyScopeTags, rows =>
            {
                ScopeTags = new ObservableCollection<RoleScopeTag>(rows);
                _scopeTagsLoaded = true;
                ApplyFilter();
                StatusText = $"Loaded {rows.Count} scope tag(s) from cache";
            }))
            {
                _ = LoadScopeTagsAsync();
            }
        }

        if (value?.Name == "Role Definitions" && !_roleDefinitionsLoaded)
        {
            if (!TryLoadLazyCacheEntry<RoleDefinition>(CacheKeyRoleDefinitions, rows =>
            {
                RoleDefinitions = new ObservableCollection<RoleDefinition>(rows);
                _roleDefinitionsLoaded = true;
                ApplyFilter();
                StatusText = $"Loaded {rows.Count} role definition(s) from cache";
            }))
            {
                _ = LoadRoleDefinitionsAsync();
            }
        }

        if (value?.Name == "Intune Branding" && !_intuneBrandingProfilesLoaded)
        {
            if (!TryLoadLazyCacheEntry<IntuneBrandingProfile>(CacheKeyIntuneBrandingProfiles, rows =>
            {
                IntuneBrandingProfiles = new ObservableCollection<IntuneBrandingProfile>(rows);
                _intuneBrandingProfilesLoaded = true;
                ApplyFilter();
                StatusText = $"Loaded {rows.Count} Intune branding profile(s) from cache";
            }))
            {
                _ = LoadIntuneBrandingProfilesAsync();
            }
        }

        if (value?.Name == "Azure Branding" && !_azureBrandingLocalizationsLoaded)
        {
            if (!TryLoadLazyCacheEntry<OrganizationalBrandingLocalization>(CacheKeyAzureBrandingLocalizations, rows =>
            {
                AzureBrandingLocalizations = new ObservableCollection<OrganizationalBrandingLocalization>(rows);
                _azureBrandingLocalizationsLoaded = true;
                ApplyFilter();
                StatusText = $"Loaded {rows.Count} Azure branding localization(s) from cache";
            }))
            {
                _ = LoadAzureBrandingLocalizationsAsync();
            }
        }
    }

    // --- Refresh single item from Graph ---

    [RelayCommand]
    private async Task RefreshSelectedItemAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (IsDeviceConfigCategory && SelectedConfiguration?.Id != null && _configProfileService != null)
            {
                StatusText = $"Refreshing {SelectedConfiguration.DisplayName}...";
                var updated = await _configProfileService.GetDeviceConfigurationAsync(SelectedConfiguration.Id, cancellationToken);
                if (updated != null)
                {
                    var idx = DeviceConfigurations.IndexOf(SelectedConfiguration);
                    if (idx >= 0)
                    {
                        DeviceConfigurations[idx] = updated;
                        SelectedConfiguration = updated;
                    }
                    DebugLog.Log("Graph", $"Refreshed device configuration: {updated.DisplayName}");
                }
            }
            else if (IsCompliancePolicyCategory && SelectedCompliancePolicy?.Id != null && _compliancePolicyService != null)
            {
                StatusText = $"Refreshing {SelectedCompliancePolicy.DisplayName}...";
                var updated = await _compliancePolicyService.GetCompliancePolicyAsync(SelectedCompliancePolicy.Id, cancellationToken);
                if (updated != null)
                {
                    var idx = CompliancePolicies.IndexOf(SelectedCompliancePolicy);
                    if (idx >= 0)
                    {
                        CompliancePolicies[idx] = updated;
                        SelectedCompliancePolicy = updated;
                    }
                    DebugLog.Log("Graph", $"Refreshed compliance policy: {updated.DisplayName}");
                }
            }
            else if (IsApplicationCategory && SelectedApplication?.Id != null && _applicationService != null)
            {
                StatusText = $"Refreshing {SelectedApplication.DisplayName}...";
                var updated = await _applicationService.GetApplicationAsync(SelectedApplication.Id, cancellationToken);
                if (updated != null)
                {
                    var idx = Applications.IndexOf(SelectedApplication);
                    if (idx >= 0)
                    {
                        Applications[idx] = updated;
                        SelectedApplication = updated;
                    }
                    DebugLog.Log("Graph", $"Refreshed application: {updated.DisplayName}");
                }
            }
            else if (IsSettingsCatalogCategory && SelectedSettingsCatalogPolicy?.Id != null && _settingsCatalogService != null)
            {
                StatusText = $"Refreshing {SelectedSettingsCatalogPolicy.Name}...";
                var updated = await _settingsCatalogService.GetSettingsCatalogPolicyAsync(SelectedSettingsCatalogPolicy.Id, cancellationToken);
                if (updated != null)
                {
                    var idx = SettingsCatalogPolicies.IndexOf(SelectedSettingsCatalogPolicy);
                    if (idx >= 0)
                    {
                        SettingsCatalogPolicies[idx] = updated;
                        SelectedSettingsCatalogPolicy = updated;
                    }
                    DebugLog.Log("Graph", $"Refreshed settings catalog policy: {updated.Name}");
                }
            }
            else if (IsEndpointSecurityCategory && SelectedEndpointSecurityIntent?.Id != null && _endpointSecurityService != null)
            {
                StatusText = $"Refreshing {SelectedEndpointSecurityIntent.DisplayName}...";
                var updated = await _endpointSecurityService.GetEndpointSecurityIntentAsync(SelectedEndpointSecurityIntent.Id, cancellationToken);
                if (updated != null)
                {
                    var idx = EndpointSecurityIntents.IndexOf(SelectedEndpointSecurityIntent);
                    if (idx >= 0)
                    {
                        EndpointSecurityIntents[idx] = updated;
                        SelectedEndpointSecurityIntent = updated;
                    }
                    DebugLog.Log("Graph", $"Refreshed endpoint security intent: {updated.DisplayName}");
                }
            }
            else if (IsAdministrativeTemplatesCategory && SelectedAdministrativeTemplate?.Id != null && _administrativeTemplateService != null)
            {
                StatusText = $"Refreshing {SelectedAdministrativeTemplate.DisplayName}...";
                var updated = await _administrativeTemplateService.GetAdministrativeTemplateAsync(SelectedAdministrativeTemplate.Id, cancellationToken);
                if (updated != null)
                {
                    var idx = AdministrativeTemplates.IndexOf(SelectedAdministrativeTemplate);
                    if (idx >= 0)
                    {
                        AdministrativeTemplates[idx] = updated;
                        SelectedAdministrativeTemplate = updated;
                    }
                    DebugLog.Log("Graph", $"Refreshed administrative template: {updated.DisplayName}");
                }
            }
            else if (IsEnrollmentConfigurationsCategory && SelectedEnrollmentConfiguration?.Id != null && _enrollmentConfigurationService != null)
            {
                StatusText = $"Refreshing {SelectedEnrollmentConfiguration.DisplayName}...";
                var updated = await _enrollmentConfigurationService.GetEnrollmentConfigurationAsync(SelectedEnrollmentConfiguration.Id, cancellationToken);
                if (updated != null)
                {
                    var idx = EnrollmentConfigurations.IndexOf(SelectedEnrollmentConfiguration);
                    if (idx >= 0)
                    {
                        EnrollmentConfigurations[idx] = updated;
                        SelectedEnrollmentConfiguration = updated;
                    }
                    DebugLog.Log("Graph", $"Refreshed enrollment configuration: {updated.DisplayName}");
                }
            }
            else if (IsAppProtectionPoliciesCategory && SelectedAppProtectionPolicy?.Id != null && _appProtectionPolicyService != null)
            {
                StatusText = $"Refreshing {SelectedAppProtectionPolicy.DisplayName}...";
                var updated = await _appProtectionPolicyService.GetAppProtectionPolicyAsync(SelectedAppProtectionPolicy.Id, cancellationToken);
                if (updated != null)
                {
                    var idx = AppProtectionPolicies.IndexOf(SelectedAppProtectionPolicy);
                    if (idx >= 0)
                    {
                        AppProtectionPolicies[idx] = updated;
                        SelectedAppProtectionPolicy = updated;
                    }
                    DebugLog.Log("Graph", $"Refreshed app protection policy: {updated.DisplayName}");
                }
            }
            else if (IsManagedDeviceAppConfigurationsCategory && SelectedManagedDeviceAppConfiguration?.Id != null && _managedAppConfigurationService != null)
            {
                StatusText = $"Refreshing {SelectedManagedDeviceAppConfiguration.DisplayName}...";
                var updated = await _managedAppConfigurationService.GetManagedDeviceAppConfigurationAsync(SelectedManagedDeviceAppConfiguration.Id, cancellationToken);
                if (updated != null)
                {
                    var idx = ManagedDeviceAppConfigurations.IndexOf(SelectedManagedDeviceAppConfiguration);
                    if (idx >= 0)
                    {
                        ManagedDeviceAppConfigurations[idx] = updated;
                        SelectedManagedDeviceAppConfiguration = updated;
                    }
                    DebugLog.Log("Graph", $"Refreshed managed device app configuration: {updated.DisplayName}");
                }
            }
            else if (IsTargetedManagedAppConfigurationsCategory && SelectedTargetedManagedAppConfiguration?.Id != null && _managedAppConfigurationService != null)
            {
                StatusText = $"Refreshing {SelectedTargetedManagedAppConfiguration.DisplayName}...";
                var updated = await _managedAppConfigurationService.GetTargetedManagedAppConfigurationAsync(SelectedTargetedManagedAppConfiguration.Id, cancellationToken);
                if (updated != null)
                {
                    var idx = TargetedManagedAppConfigurations.IndexOf(SelectedTargetedManagedAppConfiguration);
                    if (idx >= 0)
                    {
                        TargetedManagedAppConfigurations[idx] = updated;
                        SelectedTargetedManagedAppConfiguration = updated;
                    }
                    DebugLog.Log("Graph", $"Refreshed targeted managed app configuration: {updated.DisplayName}");
                }
            }
            else if (IsTermsAndConditionsCategory && SelectedTermsAndConditions?.Id != null && _termsAndConditionsService != null)
            {
                StatusText = $"Refreshing {SelectedTermsAndConditions.DisplayName}...";
                var updated = await _termsAndConditionsService.GetTermsAndConditionsAsync(SelectedTermsAndConditions.Id, cancellationToken);
                if (updated != null)
                {
                    var idx = TermsAndConditionsCollection.IndexOf(SelectedTermsAndConditions);
                    if (idx >= 0)
                    {
                        TermsAndConditionsCollection[idx] = updated;
                        SelectedTermsAndConditions = updated;
                    }
                    DebugLog.Log("Graph", $"Refreshed terms and conditions: {updated.DisplayName}");
                }
            }
            else if (IsScopeTagsCategory && SelectedScopeTag?.Id != null && _scopeTagService != null)
            {
                StatusText = $"Refreshing {SelectedScopeTag.DisplayName}...";
                var updated = await _scopeTagService.GetScopeTagAsync(SelectedScopeTag.Id, cancellationToken);
                if (updated != null)
                {
                    var idx = ScopeTags.IndexOf(SelectedScopeTag);
                    if (idx >= 0)
                    {
                        ScopeTags[idx] = updated;
                        SelectedScopeTag = updated;
                    }
                    DebugLog.Log("Graph", $"Refreshed scope tag: {updated.DisplayName}");
                }
            }
            else if (IsRoleDefinitionsCategory && SelectedRoleDefinition?.Id != null && _roleDefinitionService != null)
            {
                StatusText = $"Refreshing {SelectedRoleDefinition.DisplayName}...";
                var updated = await _roleDefinitionService.GetRoleDefinitionAsync(SelectedRoleDefinition.Id, cancellationToken);
                if (updated != null)
                {
                    var idx = RoleDefinitions.IndexOf(SelectedRoleDefinition);
                    if (idx >= 0)
                    {
                        RoleDefinitions[idx] = updated;
                        SelectedRoleDefinition = updated;
                    }
                    DebugLog.Log("Graph", $"Refreshed role definition: {updated.DisplayName}");
                }
            }
            else if (IsIntuneBrandingCategory && SelectedIntuneBrandingProfile?.Id != null && _intuneBrandingService != null)
            {
                StatusText = $"Refreshing {SelectedIntuneBrandingProfile.ProfileName ?? SelectedIntuneBrandingProfile.DisplayName}...";
                var updated = await _intuneBrandingService.GetIntuneBrandingProfileAsync(SelectedIntuneBrandingProfile.Id, cancellationToken);
                if (updated != null)
                {
                    var idx = IntuneBrandingProfiles.IndexOf(SelectedIntuneBrandingProfile);
                    if (idx >= 0)
                    {
                        IntuneBrandingProfiles[idx] = updated;
                        SelectedIntuneBrandingProfile = updated;
                    }
                    DebugLog.Log("Graph", $"Refreshed Intune branding profile: {updated.ProfileName ?? updated.DisplayName}");
                }
            }
            else if (IsAzureBrandingCategory && SelectedAzureBrandingLocalization?.Id != null && _azureBrandingService != null)
            {
                StatusText = $"Refreshing {SelectedAzureBrandingLocalization.Id}...";
                var updated = await _azureBrandingService.GetBrandingLocalizationAsync(SelectedAzureBrandingLocalization.Id, cancellationToken);
                if (updated != null)
                {
                    var idx = AzureBrandingLocalizations.IndexOf(SelectedAzureBrandingLocalization);
                    if (idx >= 0)
                    {
                        AzureBrandingLocalizations[idx] = updated;
                        SelectedAzureBrandingLocalization = updated;
                    }
                    DebugLog.Log("Graph", $"Refreshed Azure branding localization: {updated.Id}");
                }
            }
            else if (IsConditionalAccessCategory && SelectedConditionalAccessPolicy?.Id != null && _conditionalAccessPolicyService != null)
            {
                StatusText = $"Refreshing {SelectedConditionalAccessPolicy.DisplayName}...";
                var updated = await _conditionalAccessPolicyService.GetPolicyAsync(SelectedConditionalAccessPolicy.Id, cancellationToken);
                if (updated != null)
                {
                    var idx = ConditionalAccessPolicies.IndexOf(SelectedConditionalAccessPolicy);
                    if (idx >= 0)
                    {
                        ConditionalAccessPolicies[idx] = updated;
                        SelectedConditionalAccessPolicy = updated;
                    }
                    DebugLog.Log("Graph", $"Refreshed conditional access policy: {updated.DisplayName}");
                }
            }
            else if (IsAssignmentFiltersCategory && SelectedAssignmentFilter?.Id != null && _assignmentFilterService != null)
            {
                StatusText = $"Refreshing {SelectedAssignmentFilter.DisplayName}...";
                var updated = await _assignmentFilterService.GetFilterAsync(SelectedAssignmentFilter.Id, cancellationToken);
                if (updated != null)
                {
                    var idx = AssignmentFilters.IndexOf(SelectedAssignmentFilter);
                    if (idx >= 0)
                    {
                        AssignmentFilters[idx] = updated;
                        SelectedAssignmentFilter = updated;
                    }
                    DebugLog.Log("Graph", $"Refreshed assignment filter: {updated.DisplayName}");
                }
            }
            else if (IsPolicySetsCategory && SelectedPolicySet?.Id != null && _policySetService != null)
            {
                StatusText = $"Refreshing {SelectedPolicySet.DisplayName}...";
                var updated = await _policySetService.GetPolicySetAsync(SelectedPolicySet.Id, cancellationToken);
                if (updated != null)
                {
                    var idx = PolicySets.IndexOf(SelectedPolicySet);
                    if (idx >= 0)
                    {
                        PolicySets[idx] = updated;
                        SelectedPolicySet = updated;
                    }
                    DebugLog.Log("Graph", $"Refreshed policy set: {updated.DisplayName}");
                }
            }
            else
            {
                return;
            }

            StatusText = "Item refreshed";
        }
        catch (Exception ex)
        {
            DebugLog.LogError($"Failed to refresh item: {FormatGraphError(ex)}", ex);
            SetError($"Refresh failed: {FormatGraphError(ex)}");
        }
    }

    /// <summary>
    /// Whether a single item is currently selected and can be refreshed.
    /// </summary>
    public bool CanRefreshSelectedItem =>
        (IsDeviceConfigCategory && SelectedConfiguration != null) ||
        (IsCompliancePolicyCategory && SelectedCompliancePolicy != null) ||
        (IsApplicationCategory && SelectedApplication != null) ||
        (IsSettingsCatalogCategory && SelectedSettingsCatalogPolicy != null) ||
        (IsEndpointSecurityCategory && SelectedEndpointSecurityIntent != null) ||
        (IsAdministrativeTemplatesCategory && SelectedAdministrativeTemplate != null) ||
        (IsEnrollmentConfigurationsCategory && SelectedEnrollmentConfiguration != null) ||
        (IsAppProtectionPoliciesCategory && SelectedAppProtectionPolicy != null) ||
        (IsManagedDeviceAppConfigurationsCategory && SelectedManagedDeviceAppConfiguration != null) ||
        (IsTargetedManagedAppConfigurationsCategory && SelectedTargetedManagedAppConfiguration != null) ||
        (IsTermsAndConditionsCategory && SelectedTermsAndConditions != null) ||
        (IsScopeTagsCategory && SelectedScopeTag != null) ||
        (IsRoleDefinitionsCategory && SelectedRoleDefinition != null) ||
        (IsIntuneBrandingCategory && SelectedIntuneBrandingProfile != null) ||
        (IsAzureBrandingCategory && SelectedAzureBrandingLocalization != null) ||
        (IsConditionalAccessCategory && SelectedConditionalAccessPolicy != null) ||
        (IsAssignmentFiltersCategory && SelectedAssignmentFilter != null) ||
        (IsPolicySetsCategory && SelectedPolicySet != null);

    // --- Selection-changed handlers (load detail + assignments) ---

    partial void OnSelectedConfigurationChanged(DeviceConfiguration? value)
    {
        SelectedItemAssignments.Clear();
        SelectedItemTypeName = FriendlyODataType(value?.OdataType);
        OnPropertyChanged(nameof(CanRefreshSelectedItem));
        if (value?.Id != null)
            _ = LoadConfigAssignmentsAsync(value.Id);
    }

    partial void OnSelectedCompliancePolicyChanged(DeviceCompliancePolicy? value)
    {
        SelectedItemAssignments.Clear();
        SelectedItemTypeName = FriendlyODataType(value?.OdataType);
        OnPropertyChanged(nameof(CanRefreshSelectedItem));
        if (value?.Id != null)
            _ = LoadCompliancePolicyAssignmentsAsync(value.Id);
    }

    partial void OnSelectedSettingsCatalogPolicyChanged(DeviceManagementConfigurationPolicy? value)
    {
        SelectedItemAssignments.Clear();
        SelectedItemTypeName = value?.Platforms?.ToString() ?? "";
        OnPropertyChanged(nameof(CanRefreshSelectedItem));
        if (value?.Id != null)
            _ = LoadSettingsCatalogAssignmentsAsync(value.Id);
    }

    partial void OnSelectedApplicationChanged(MobileApp? value)
    {
        SelectedItemAssignments.Clear();
        var odataType = value?.OdataType;
        SelectedItemTypeName = FriendlyODataType(odataType);
        SelectedItemPlatform = InferPlatform(odataType);
        OnPropertyChanged(nameof(CanRefreshSelectedItem));
        if (value?.Id != null)
            _ = LoadApplicationAssignmentsAsync(value.Id);
    }

    partial void OnSelectedConditionalAccessPolicyChanged(ConditionalAccessPolicy? value)
    {
        SelectedItemAssignments.Clear();
        SelectedItemTypeName = "Conditional Access";
        SelectedItemPlatform = "";
        OnPropertyChanged(nameof(CanRefreshSelectedItem));
    }

    partial void OnSelectedEndpointSecurityIntentChanged(DeviceManagementIntent? value)
    {
        SelectedItemAssignments.Clear();
        SelectedItemTypeName = "Endpoint Security";
        SelectedItemPlatform = "";
        OnPropertyChanged(nameof(CanRefreshSelectedItem));
    }

    partial void OnSelectedAdministrativeTemplateChanged(GroupPolicyConfiguration? value)
    {
        SelectedItemAssignments.Clear();
        SelectedItemTypeName = "Administrative Template";
        SelectedItemPlatform = "";
        OnPropertyChanged(nameof(CanRefreshSelectedItem));
    }

    partial void OnSelectedEnrollmentConfigurationChanged(DeviceEnrollmentConfiguration? value)
    {
        SelectedItemAssignments.Clear();
        SelectedItemTypeName = FriendlyODataType(value?.OdataType);
        SelectedItemPlatform = "";
        OnPropertyChanged(nameof(CanRefreshSelectedItem));
    }

    partial void OnSelectedAppProtectionPolicyChanged(ManagedAppPolicy? value)
    {
        SelectedItemAssignments.Clear();
        SelectedItemTypeName = FriendlyODataType(value?.OdataType);
        SelectedItemPlatform = "";
        OnPropertyChanged(nameof(CanRefreshSelectedItem));
    }

    partial void OnSelectedManagedDeviceAppConfigurationChanged(ManagedDeviceMobileAppConfiguration? value)
    {
        SelectedItemAssignments.Clear();
        SelectedItemTypeName = FriendlyODataType(value?.OdataType);
        SelectedItemPlatform = "";
        OnPropertyChanged(nameof(CanRefreshSelectedItem));
    }

    partial void OnSelectedTargetedManagedAppConfigurationChanged(TargetedManagedAppConfiguration? value)
    {
        SelectedItemAssignments.Clear();
        SelectedItemTypeName = FriendlyODataType(value?.OdataType);
        SelectedItemPlatform = "";
        OnPropertyChanged(nameof(CanRefreshSelectedItem));
    }

    partial void OnSelectedTermsAndConditionsChanged(TermsAndConditions? value)
    {
        SelectedItemAssignments.Clear();
        SelectedItemTypeName = "Terms and Conditions";
        SelectedItemPlatform = "";
        OnPropertyChanged(nameof(CanRefreshSelectedItem));
    }

    partial void OnSelectedScopeTagChanged(RoleScopeTag? value)
    {
        SelectedItemAssignments.Clear();
        SelectedItemTypeName = "Scope Tag";
        SelectedItemPlatform = "";
        OnPropertyChanged(nameof(CanRefreshSelectedItem));
    }

    partial void OnSelectedRoleDefinitionChanged(RoleDefinition? value)
    {
        SelectedItemAssignments.Clear();
        SelectedItemTypeName = "Role Definition";
        SelectedItemPlatform = "";
        OnPropertyChanged(nameof(CanRefreshSelectedItem));
    }

    partial void OnSelectedIntuneBrandingProfileChanged(IntuneBrandingProfile? value)
    {
        SelectedItemAssignments.Clear();
        SelectedItemTypeName = "Intune Branding";
        SelectedItemPlatform = "";
        OnPropertyChanged(nameof(CanRefreshSelectedItem));
    }

    partial void OnSelectedAzureBrandingLocalizationChanged(OrganizationalBrandingLocalization? value)
    {
        SelectedItemAssignments.Clear();
        SelectedItemTypeName = "Azure Branding";
        SelectedItemPlatform = "";
        OnPropertyChanged(nameof(CanRefreshSelectedItem));
    }

    partial void OnSelectedAssignmentFilterChanged(DeviceAndAppManagementAssignmentFilter? value)
    {
        SelectedItemAssignments.Clear();
        SelectedItemTypeName = "Assignment Filter";
        SelectedItemPlatform = "";
        OnPropertyChanged(nameof(CanRefreshSelectedItem));
    }

    partial void OnSelectedPolicySetChanged(PolicySet? value)
    {
        SelectedItemAssignments.Clear();
        SelectedItemTypeName = "Policy Set";
        SelectedItemPlatform = "";
        OnPropertyChanged(nameof(CanRefreshSelectedItem));
    }

    partial void OnSelectedDynamicGroupRowChanged(GroupRow? value)
    {
        SelectedGroupMembers.Clear();
        if (value?.GroupId != null && !string.IsNullOrEmpty(value.GroupId))
            _ = LoadGroupMembersAsync(value.GroupId);
    }

    partial void OnSelectedAssignedGroupRowChanged(GroupRow? value)
    {
        SelectedGroupMembers.Clear();
        if (value?.GroupId != null && !string.IsNullOrEmpty(value.GroupId))
            _ = LoadGroupMembersAsync(value.GroupId);
    }

    private async Task LoadGroupMembersAsync(string groupId)
    {
        if (_groupService == null) return;
        IsLoadingGroupMembers = true;
        try
        {
            var members = await _groupService.ListGroupMembersAsync(groupId);
            var items = members.Select(m => new GroupMemberItem
            {
                MemberType = m.MemberType,
                DisplayName = m.DisplayName,
                SecondaryInfo = m.SecondaryInfo,
                TertiaryInfo = m.TertiaryInfo,
                Status = m.Status,
                Id = m.Id
            }).ToList();
            SelectedGroupMembers = new ObservableCollection<GroupMemberItem>(items);
            DebugLog.Log("Graph", $"Loaded {items.Count} member(s) for group {groupId}");
        }
        catch (Exception ex)
        {
            DebugLog.LogError($"Failed to load group members: {FormatGraphError(ex)}", ex);
        }
        finally { IsLoadingGroupMembers = false; }
    }

    private async Task LoadConfigAssignmentsAsync(string configId)
    {
        if (_configProfileService == null) return;
        IsLoadingDetails = true;
        try
        {
            var assignments = await _configProfileService.GetAssignmentsAsync(configId);
            var items = new List<AssignmentDisplayItem>();
            foreach (var a in assignments)
                items.Add(await MapAssignmentAsync(a.Target));
            SelectedItemAssignments = new ObservableCollection<AssignmentDisplayItem>(items);
        }
        catch (Exception ex)
        {
            DebugLog.LogError($"Failed to load configuration assignments: {FormatGraphError(ex)}", ex);
        }
        finally { IsLoadingDetails = false; }
    }

    private async Task LoadCompliancePolicyAssignmentsAsync(string policyId)
    {
        if (_compliancePolicyService == null) return;
        IsLoadingDetails = true;
        try
        {
            var assignments = await _compliancePolicyService.GetAssignmentsAsync(policyId);
            var items = new List<AssignmentDisplayItem>();
            foreach (var a in assignments)
                items.Add(await MapAssignmentAsync(a.Target));
            SelectedItemAssignments = new ObservableCollection<AssignmentDisplayItem>(items);
        }
        catch (Exception ex)
        {
            DebugLog.LogError($"Failed to load compliance policy assignments: {FormatGraphError(ex)}", ex);
        }
        finally { IsLoadingDetails = false; }
    }

    private async Task LoadSettingsCatalogAssignmentsAsync(string policyId)
    {
        if (_settingsCatalogService == null) return;
        IsLoadingDetails = true;
        try
        {
            var assignments = await _settingsCatalogService.GetAssignmentsAsync(policyId);
            var items = new List<AssignmentDisplayItem>();
            foreach (var a in assignments)
                items.Add(await MapAssignmentAsync(a.Target));
            SelectedItemAssignments = new ObservableCollection<AssignmentDisplayItem>(items);
        }
        catch (Exception ex)
        {
            DebugLog.LogError($"Failed to load settings catalog assignments: {FormatGraphError(ex)}", ex);
        }
        finally { IsLoadingDetails = false; }
    }

    private async Task LoadApplicationAssignmentsAsync(string appId)
    {
        if (_applicationService == null) return;
        IsLoadingDetails = true;
        try
        {
            var assignments = await _applicationService.GetAssignmentsAsync(appId);
            var items = new List<AssignmentDisplayItem>();
            foreach (var a in assignments)
            {
                var item = await MapAssignmentAsync(a.Target);
                items.Add(new AssignmentDisplayItem
                {
                    Target = item.Target,
                    GroupId = item.GroupId,
                    TargetKind = item.TargetKind,
                    Intent = a.Intent?.ToString() ?? ""
                });
            }
            SelectedItemAssignments = new ObservableCollection<AssignmentDisplayItem>(items);
        }
        catch (Exception ex)
        {
            DebugLog.LogError($"Failed to load application assignments: {FormatGraphError(ex)}", ex);
        }
        finally { IsLoadingDetails = false; }
    }

    private async Task<AssignmentDisplayItem> MapAssignmentAsync(DeviceAndAppManagementAssignmentTarget? target)
    {
        switch (target)
        {
            case AllDevicesAssignmentTarget:
                return new AssignmentDisplayItem { Target = "All Devices", TargetKind = "Include" };
            case AllLicensedUsersAssignmentTarget:
                return new AssignmentDisplayItem { Target = "All Users", TargetKind = "Include" };
            case ExclusionGroupAssignmentTarget excl:
                return new AssignmentDisplayItem
                {
                    Target = await ResolveGroupNameAsync(excl.GroupId),
                    GroupId = excl.GroupId ?? "",
                    TargetKind = "Exclude"
                };
            case GroupAssignmentTarget grp:
                return new AssignmentDisplayItem
                {
                    Target = await ResolveGroupNameAsync(grp.GroupId),
                    GroupId = grp.GroupId ?? "",
                    TargetKind = "Include"
                };
            default:
                return new AssignmentDisplayItem { Target = "Unknown", TargetKind = "Include" };
        }
    }

    private readonly Dictionary<string, string> _groupNameCache = new();

    private async Task<string> ResolveGroupNameAsync(string? groupId)
    {
        if (string.IsNullOrEmpty(groupId)) return "Unknown Group";
        if (!Guid.TryParse(groupId, out _)) return groupId; // Not a valid GUID — skip Graph call
        if (_groupNameCache.TryGetValue(groupId, out var cached)) return cached;

        try
        {
            if (_graphClient != null)
            {
                // Use $filter to avoid 404 ODataError for deleted/inaccessible groups
                var response = await _graphClient.Groups
                    .GetAsync(req =>
                    {
                        req.QueryParameters.Filter = $"id eq '{groupId}'";
                        req.QueryParameters.Select = new[] { "displayName" };
                        req.QueryParameters.Top = 1;
                    });
                var group = response?.Value?.FirstOrDefault();
                var name = group?.DisplayName ?? groupId;
                _groupNameCache[groupId] = name;
                return name;
            }
        }
        catch (Exception ex)
        {
            DebugLog.Log("Graph", $"Could not resolve group {groupId}: {FormatGraphError(ex)}");
        }

        _groupNameCache[groupId] = groupId;
        return groupId;
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

    // --- Application Assignments flattened view ---

    private async Task LoadAppAssignmentRowsAsync()
    {
        if (_applicationService == null || _graphClient == null) return;

        IsBusy = true;
        IsLoadingDetails = true;
        Overview.IsLoading = true;
        StatusText = "Loading application assignments...";

        try
        {
            // Reuse existing apps list if available, otherwise fetch
            var apps = Applications.Count > 0
                ? Applications.ToList()
                : await _applicationService.ListApplicationsAsync();

            var rows = new List<AppAssignmentRow>();
            var total = apps.Count;
            var processed = 0;

            // Use a semaphore to limit concurrent Graph API calls
            using var semaphore = new SemaphoreSlim(5, 5);
            var tasks = apps.Select(async app =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var assignments = app.Id != null
                        ? await _applicationService.GetAssignmentsAsync(app.Id)
                        : [];

                    var appRows = new List<AppAssignmentRow>();
                    foreach (var assignment in assignments)
                    {
                        appRows.Add(await BuildAppAssignmentRowAsync(app, assignment));
                    }

                    // If app has no assignments, still include it with empty assignment fields
                    if (assignments.Count == 0)
                    {
                        appRows.Add(BuildAppRowNoAssignment(app));
                    }

                    var currentProcessed = Interlocked.Increment(ref processed);
                    lock (rows)
                    {
                        rows.AddRange(appRows);
                    }

                    // Update status on UI thread periodically
                    if (currentProcessed % 10 == 0 || currentProcessed == total)
                    {
                        var currentTotal = total;
                        Dispatcher.UIThread.Post(() =>
                            StatusText = $"Loading assignments... {currentProcessed}/{currentTotal} apps");
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToList();

            await Task.WhenAll(tasks);

            // Sort by app name, then target name
            rows.Sort((a, b) =>
            {
                var cmp = string.Compare(a.AppName, b.AppName, StringComparison.OrdinalIgnoreCase);
                return cmp != 0 ? cmp : string.Compare(a.TargetName, b.TargetName, StringComparison.OrdinalIgnoreCase);
            });

            AppAssignmentRows = new ObservableCollection<AppAssignmentRow>(rows);
            _appAssignmentsLoaded = true;
            ApplyFilter();

            // Save to cache
            if (ActiveProfile?.TenantId != null)
            {
                _cacheService.Set(ActiveProfile.TenantId, CacheKeyAppAssignments, rows);
                DebugLog.Log("Cache", $"Saved {rows.Count} app assignment row(s) to cache");
            }

            // Update Overview dashboard now that all data is ready
            Overview.Update(
                ActiveProfile,
                (IReadOnlyList<DeviceConfiguration>)DeviceConfigurations,
                (IReadOnlyList<DeviceCompliancePolicy>)CompliancePolicies,
                (IReadOnlyList<MobileApp>)Applications,
                (IReadOnlyList<AppAssignmentRow>)AppAssignmentRows);

            StatusText = $"Loaded {rows.Count} application assignments row(s) from {total} apps";
        }
        catch (Exception ex)
        {
            SetError($"Failed to load Application Assignments: {FormatGraphError(ex)}");
            StatusText = "Error loading Application Assignments";
        }
        finally
        {
            IsBusy = false;
            IsLoadingDetails = false;
            Overview.IsLoading = false;
        }
    }

    private async Task<AppAssignmentRow> BuildAppAssignmentRowAsync(MobileApp app, MobileAppAssignment assignment)
    {
        var (assignmentType, targetName, targetGroupId, isExclusion) =
            await ResolveAssignmentTargetAsync(assignment.Target);

        return new AppAssignmentRow
        {
            AppId = app.Id ?? "",
            AppName = app.DisplayName ?? "",
            Publisher = app.Publisher ?? "",
            Description = app.Description ?? "",
            AppType = ExtractShortTypeName(app.OdataType),
            Version = ExtractVersion(app),
            Platform = InferPlatform(app.OdataType),
            BundleId = ExtractBundleId(app),
            PackageId = ExtractPackageId(app),
            IsFeatured = app.IsFeatured == true ? "True" : "False",
            CreatedDate = app.CreatedDateTime?.ToString("g", CultureInfo.InvariantCulture) ?? "",
            LastModified = app.LastModifiedDateTime?.ToString("g", CultureInfo.InvariantCulture) ?? "",
            AssignmentType = assignmentType,
            TargetName = targetName,
            TargetGroupId = targetGroupId,
            InstallIntent = assignment.Intent?.ToString()?.ToLowerInvariant() ?? "",
            AssignmentSettings = FormatAssignmentSettings(assignment.Settings),
            IsExclusion = isExclusion,
            AppStoreUrl = ExtractAppStoreUrl(app),
            PrivacyUrl = app.PrivacyInformationUrl ?? "",
            InformationUrl = app.InformationUrl ?? "",
            MinimumOsVersion = ExtractMinOsVersion(app),
            MinimumFreeDiskSpaceMB = ExtractMinDiskSpace(app),
            MinimumMemoryMB = ExtractMinMemory(app),
            MinimumProcessors = ExtractMinProcessors(app),
            Categories = app.Categories != null
                ? string.Join(", ", app.Categories.Select(c => c.DisplayName ?? ""))
                : "",
            Notes = app.Notes ?? ""
        };
    }

    private AppAssignmentRow BuildAppRowNoAssignment(MobileApp app)
    {
        return new AppAssignmentRow
        {
            AppId = app.Id ?? "",
            AppName = app.DisplayName ?? "",
            Publisher = app.Publisher ?? "",
            Description = app.Description ?? "",
            AppType = ExtractShortTypeName(app.OdataType),
            Version = ExtractVersion(app),
            Platform = InferPlatform(app.OdataType),
            BundleId = ExtractBundleId(app),
            PackageId = ExtractPackageId(app),
            IsFeatured = app.IsFeatured == true ? "True" : "False",
            CreatedDate = app.CreatedDateTime?.ToString("g", CultureInfo.InvariantCulture) ?? "",
            LastModified = app.LastModifiedDateTime?.ToString("g", CultureInfo.InvariantCulture) ?? "",
            AssignmentType = "None",
            TargetName = "",
            TargetGroupId = "",
            InstallIntent = "",
            AssignmentSettings = "",
            IsExclusion = "False",
            AppStoreUrl = ExtractAppStoreUrl(app),
            PrivacyUrl = app.PrivacyInformationUrl ?? "",
            InformationUrl = app.InformationUrl ?? "",
            MinimumOsVersion = ExtractMinOsVersion(app),
            MinimumFreeDiskSpaceMB = ExtractMinDiskSpace(app),
            MinimumMemoryMB = ExtractMinMemory(app),
            MinimumProcessors = ExtractMinProcessors(app),
            Categories = app.Categories != null
                ? string.Join(", ", app.Categories.Select(c => c.DisplayName ?? ""))
                : "",
            Notes = app.Notes ?? ""
        };
    }

    private async Task<(string Type, string Name, string GroupId, string IsExclusion)>
        ResolveAssignmentTargetAsync(DeviceAndAppManagementAssignmentTarget? target)
    {
        return target switch
        {
            AllDevicesAssignmentTarget => ("All Devices", "All Devices", "", "False"),
            AllLicensedUsersAssignmentTarget => ("All Users", "All Users", "", "False"),
            ExclusionGroupAssignmentTarget excl =>
                ("Group", await ResolveGroupNameAsync(excl.GroupId), excl.GroupId ?? "", "True"),
            GroupAssignmentTarget grp =>
                ("Group", await ResolveGroupNameAsync(grp.GroupId), grp.GroupId ?? "", "False"),
            _ => ("Unknown", "Unknown", "", "False")
        };
    }

    // --- Type-specific field extractors ---

    private static string? TryGetAdditionalString(MobileApp app, string key)
    {
        if (app.AdditionalData?.TryGetValue(key, out var val) == true)
            return val?.ToString();
        return null;
    }

    private static string ExtractShortTypeName(string? odataType)
    {
        if (string.IsNullOrEmpty(odataType)) return "";
        // "#microsoft.graph.win32LobApp" → "win32LobApp"
        return odataType.Split('.').LastOrDefault() ?? odataType;
    }

    private static string ExtractVersion(MobileApp app)
    {
        return app switch
        {
            Win32LobApp w => TryGetAdditionalString(w, "displayVersion")
                             ?? w.MsiInformation?.ProductVersion ?? "",
            MacOSLobApp m => m.VersionNumber ?? "",
            MacOSDmgApp d => d.PrimaryBundleVersion ?? "",
            IosLobApp i => i.VersionNumber ?? "",
            _ => ""
        };
    }

    private static string ExtractBundleId(MobileApp app)
    {
        return app switch
        {
            IosLobApp i => i.BundleId ?? "",
            IosStoreApp s => s.BundleId ?? "",
            IosVppApp v => v.BundleId ?? "",
            MacOSLobApp m => m.BundleId ?? "",
            MacOSDmgApp d => d.PrimaryBundleId ?? "",
            _ => ""
        };
    }

    private static string ExtractPackageId(MobileApp app)
    {
        return app switch
        {
            AndroidStoreApp a => a.PackageId ?? "",
            _ => ""
        };
    }

    private static string ExtractAppStoreUrl(MobileApp app)
    {
        return app switch
        {
            IosStoreApp i => i.AppStoreUrl ?? "",
            AndroidStoreApp a => a.AppStoreUrl ?? "",
            WebApp w => w.AppUrl ?? "",
            _ => ""
        };
    }

    private static string ExtractMinOsVersion(MobileApp app)
    {
        return app switch
        {
            Win32LobApp w => w.MinimumSupportedWindowsRelease ?? "",
            _ => ""
        };
    }

    private static string ExtractMinDiskSpace(MobileApp app)
    {
        return app switch
        {
            Win32LobApp w when w.MinimumFreeDiskSpaceInMB.HasValue =>
                w.MinimumFreeDiskSpaceInMB.Value.ToString(CultureInfo.InvariantCulture),
            _ => ""
        };
    }

    private static string ExtractMinMemory(MobileApp app)
    {
        return app switch
        {
            Win32LobApp w when w.MinimumMemoryInMB.HasValue =>
                w.MinimumMemoryInMB.Value.ToString(CultureInfo.InvariantCulture),
            _ => ""
        };
    }

    private static string ExtractMinProcessors(MobileApp app)
    {
        return app switch
        {
            Win32LobApp w when w.MinimumNumberOfProcessors.HasValue =>
                w.MinimumNumberOfProcessors.Value.ToString(CultureInfo.InvariantCulture),
            _ => ""
        };
    }

    private static string FormatAssignmentSettings(MobileAppAssignmentSettings? settings)
    {
        if (settings is Win32LobAppAssignmentSettings w32)
        {
            var parts = new List<string>();
            if (w32.Notifications.HasValue)
                parts.Add($"Notifications: {w32.Notifications.Value.ToString().ToLowerInvariant()}");
            if (w32.InstallTimeSettings != null)
                parts.Add("Install Time: configured");
            if (w32.DeliveryOptimizationPriority.HasValue)
                parts.Add($"Delivery Priority: {w32.DeliveryOptimizationPriority.Value.ToString().ToLowerInvariant()}");
            return parts.Count > 0 ? string.Join("; ", parts) : "N/A";
        }

        return settings != null ? "N/A" : "N/A";
    }

    // --- CSV Export ---

    [RelayCommand]
    private async Task ExportAppAssignmentsCsvAsync(CancellationToken cancellationToken)
    {
        if (AppAssignmentRows.Count == 0)
        {
            StatusText = "No application assignments data to export";
            return;
        }

        IsBusy = true;
        StatusText = "Exporting Application Assignments to CSV...";

        try
        {
            var outputPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "IntuneExport");
            Directory.CreateDirectory(outputPath);

            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var csvPath = Path.Combine(outputPath, $"ApplicationAssignments-{timestamp}.csv");

            var sb = new StringBuilder();

            // Header row matching the CSV columns
            sb.AppendLine("\"App Name\",\"Publisher\",\"Description\",\"App Type\",\"Version\"," +
                          "\"Bundle ID\",\"Package ID\",\"Is Featured\",\"Created Date\",\"Last Modified\"," +
                          "\"Assignment Type\",\"Target Name\",\"Target Group ID\",\"Install Intent\"," +
                          "\"Assignment Settings\",\"Is Exclusion\",\"App Store URL\",\"Privacy URL\"," +
                          "\"Information URL\",\"Minimum OS Version\",\"Minimum Free Disk Space (MB)\"," +
                          "\"Minimum Memory (MB)\",\"Minimum Processors\",\"Categories\",\"Notes\"");

            foreach (var row in AppAssignmentRows)
            {
                sb.AppendLine(string.Join(",",
                    CsvEscape(row.AppName),
                    CsvEscape(row.Publisher),
                    CsvEscape(row.Description),
                    CsvEscape(row.AppType),
                    CsvEscape(row.Version),
                    CsvEscape(row.BundleId),
                    CsvEscape(row.PackageId),
                    CsvEscape(row.IsFeatured),
                    CsvEscape(row.CreatedDate),
                    CsvEscape(row.LastModified),
                    CsvEscape(row.AssignmentType),
                    CsvEscape(row.TargetName),
                    CsvEscape(row.TargetGroupId),
                    CsvEscape(row.InstallIntent),
                    CsvEscape(row.AssignmentSettings),
                    CsvEscape(row.IsExclusion),
                    CsvEscape(row.AppStoreUrl),
                    CsvEscape(row.PrivacyUrl),
                    CsvEscape(row.InformationUrl),
                    CsvEscape(row.MinimumOsVersion),
                    CsvEscape(row.MinimumFreeDiskSpaceMB),
                    CsvEscape(row.MinimumMemoryMB),
                    CsvEscape(row.MinimumProcessors),
                    CsvEscape(row.Categories),
                    CsvEscape(row.Notes)));
            }

            await File.WriteAllTextAsync(csvPath, sb.ToString(), Encoding.UTF8, cancellationToken);
            StatusText = $"Exported {AppAssignmentRows.Count} rows to {csvPath}";
        }
        catch (Exception ex)
        {
            SetError($"CSV export failed: {ex.Message}");
            StatusText = "CSV export failed";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string CsvEscape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    // --- Group CSV Export ---

    [RelayCommand]
    private async Task ExportGroupsCsvAsync(CancellationToken cancellationToken)
    {
        var isDynamic = IsDynamicGroupsCategory;
        var rows = isDynamic ? DynamicGroupRows : AssignedGroupRows;
        var label = isDynamic ? "Dynamic Groups" : "Assigned Groups";

        if (rows.Count == 0)
        {
            StatusText = $"No {label.ToLowerInvariant()} data to export";
            return;
        }

        IsBusy = true;
        StatusText = $"Exporting {label} to CSV...";

        try
        {
            var outputPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "IntuneExport");
            Directory.CreateDirectory(outputPath);

            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var fileName = isDynamic ? "DynamicGroups" : "AssignedGroups";
            var csvPath = Path.Combine(outputPath, $"{fileName}-{timestamp}.csv");

            var sb = new StringBuilder();

            if (isDynamic)
            {
                sb.AppendLine("\"Group Name\",\"Description\",\"Membership Rule\",\"Processing State\"," +
                              "\"Group Type\",\"Total Members\",\"Users\",\"Devices\",\"Nested Groups\"," +
                              "\"Security Enabled\",\"Mail Enabled\",\"Created Date\",\"Group ID\"");

                foreach (var row in rows)
                {
                    sb.AppendLine(string.Join(",",
                        CsvEscape(row.GroupName), CsvEscape(row.Description),
                        CsvEscape(row.MembershipRule), CsvEscape(row.ProcessingState),
                        CsvEscape(row.GroupType), CsvEscape(row.TotalMembers),
                        CsvEscape(row.Users), CsvEscape(row.Devices),
                        CsvEscape(row.NestedGroups), CsvEscape(row.SecurityEnabled),
                        CsvEscape(row.MailEnabled), CsvEscape(row.CreatedDate),
                        CsvEscape(row.GroupId)));
                }
            }
            else
            {
                sb.AppendLine("\"Group Name\",\"Description\",\"Group Type\"," +
                              "\"Total Members\",\"Users\",\"Devices\",\"Nested Groups\"," +
                              "\"Security Enabled\",\"Mail Enabled\",\"Created Date\",\"Group ID\"");

                foreach (var row in rows)
                {
                    sb.AppendLine(string.Join(",",
                        CsvEscape(row.GroupName), CsvEscape(row.Description),
                        CsvEscape(row.GroupType), CsvEscape(row.TotalMembers),
                        CsvEscape(row.Users), CsvEscape(row.Devices),
                        CsvEscape(row.NestedGroups), CsvEscape(row.SecurityEnabled),
                        CsvEscape(row.MailEnabled), CsvEscape(row.CreatedDate),
                        CsvEscape(row.GroupId)));
                }
            }

            await File.WriteAllTextAsync(csvPath, sb.ToString(), Encoding.UTF8, cancellationToken);
            StatusText = $"Exported {rows.Count} {label.ToLowerInvariant()} to {csvPath}";
        }
        catch (Exception ex)
        {
            SetError($"CSV export failed: {ex.Message}");
            StatusText = "CSV export failed";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // --- Dynamic Groups view ---

    private async Task LoadDynamicGroupRowsAsync()
    {
        if (_groupService == null) return;

        IsBusy = true;
        StatusText = "Loading dynamic groups...";

        try
        {
            var groups = await _groupService.ListDynamicGroupsAsync();
            var rows = new List<GroupRow>();
            var total = groups.Count;
            var processed = 0;

            using var semaphore = new SemaphoreSlim(5, 5);
            var tasks = groups.Select(async group =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var counts = group.Id != null
                        ? await _groupService.GetMemberCountsAsync(group.Id)
                        : new GroupMemberCounts(0, 0, 0, 0);

                    var row = BuildGroupRow(group, counts);

                    var currentProcessed = Interlocked.Increment(ref processed);
                    lock (rows)
                    {
                        rows.Add(row);
                    }

                    if (currentProcessed % 10 == 0 || currentProcessed == total)
                    {
                        Dispatcher.UIThread.Post(() =>
                            StatusText = $"Loading dynamic groups... {currentProcessed}/{total}");
                    }
                }
                finally { semaphore.Release(); }
            }).ToList();

            await Task.WhenAll(tasks);

            rows.Sort((a, b) => string.Compare(a.GroupName, b.GroupName, StringComparison.OrdinalIgnoreCase));

            DynamicGroupRows = new ObservableCollection<GroupRow>(rows);
            _dynamicGroupsLoaded = true;
            ApplyFilter();

            // Save to cache
            if (ActiveProfile?.TenantId != null)
            {
                _cacheService.Set(ActiveProfile.TenantId, CacheKeyDynamicGroups, rows);
                DebugLog.Log("Cache", $"Saved {rows.Count} dynamic group row(s) to cache");
            }

            StatusText = $"Loaded {rows.Count} dynamic group(s)";
        }
        catch (Exception ex)
        {
            SetError($"Failed to load dynamic groups: {FormatGraphError(ex)}");
            StatusText = "Error loading dynamic groups";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // --- Assigned Groups view ---

    private async Task LoadAssignedGroupRowsAsync()
    {
        if (_groupService == null) return;

        IsBusy = true;
        StatusText = "Loading assigned groups...";

        try
        {
            var groups = await _groupService.ListAssignedGroupsAsync();
            var rows = new List<GroupRow>();
            var total = groups.Count;
            var processed = 0;

            using var semaphore = new SemaphoreSlim(5, 5);
            var tasks = groups.Select(async group =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var counts = group.Id != null
                        ? await _groupService.GetMemberCountsAsync(group.Id)
                        : new GroupMemberCounts(0, 0, 0, 0);

                    var row = BuildGroupRow(group, counts);

                    var currentProcessed = Interlocked.Increment(ref processed);
                    lock (rows)
                    {
                        rows.Add(row);
                    }

                    if (currentProcessed % 10 == 0 || currentProcessed == total)
                    {
                        Dispatcher.UIThread.Post(() =>
                            StatusText = $"Loading assigned groups... {currentProcessed}/{total}");
                    }
                }
                finally { semaphore.Release(); }
            }).ToList();

            await Task.WhenAll(tasks);

            rows.Sort((a, b) => string.Compare(a.GroupName, b.GroupName, StringComparison.OrdinalIgnoreCase));

            AssignedGroupRows = new ObservableCollection<GroupRow>(rows);
            _assignedGroupsLoaded = true;
            ApplyFilter();

            // Save to cache
            if (ActiveProfile?.TenantId != null)
            {
                _cacheService.Set(ActiveProfile.TenantId, CacheKeyAssignedGroups, rows);
                DebugLog.Log("Cache", $"Saved {rows.Count} assigned group row(s) to cache");
            }

            StatusText = $"Loaded {rows.Count} assigned group(s)";
        }
        catch (Exception ex)
        {
            SetError($"Failed to load assigned groups: {FormatGraphError(ex)}");
            StatusText = "Error loading assigned groups";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // --- Conditional Access view ---

    private async Task LoadConditionalAccessPoliciesAsync()
    {
        if (_conditionalAccessPolicyService == null) return;

        IsBusy = true;
        StatusText = "Loading conditional access policies...";

        try
        {
            var policies = await _conditionalAccessPolicyService.ListPoliciesAsync();
            ConditionalAccessPolicies = new ObservableCollection<ConditionalAccessPolicy>(policies);
            _conditionalAccessLoaded = true;
            ApplyFilter();

            if (ActiveProfile?.TenantId != null)
            {
                _cacheService.Set(ActiveProfile.TenantId, CacheKeyConditionalAccess, policies);
                DebugLog.Log("Cache", $"Saved {policies.Count} conditional access policy(ies) to cache");
            }

            StatusText = $"Loaded {policies.Count} conditional access policy(ies)";
        }
        catch (Exception ex)
        {
            SetError($"Failed to load conditional access policies: {FormatGraphError(ex)}");
            StatusText = "Error loading conditional access policies";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // --- Assignment Filters view ---

    private async Task LoadAssignmentFiltersAsync()
    {
        if (_assignmentFilterService == null) return;

        IsBusy = true;
        StatusText = "Loading assignment filters...";

        try
        {
            var filters = await _assignmentFilterService.ListFiltersAsync();
            AssignmentFilters = new ObservableCollection<DeviceAndAppManagementAssignmentFilter>(filters);
            _assignmentFiltersLoaded = true;
            ApplyFilter();

            if (ActiveProfile?.TenantId != null)
            {
                _cacheService.Set(ActiveProfile.TenantId, CacheKeyAssignmentFilters, filters);
                DebugLog.Log("Cache", $"Saved {filters.Count} assignment filter(s) to cache");
            }

            StatusText = $"Loaded {filters.Count} assignment filter(s)";
        }
        catch (Exception ex)
        {
            SetError($"Failed to load assignment filters: {FormatGraphError(ex)}");
            StatusText = "Error loading assignment filters";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // --- Policy Sets view ---

    private async Task LoadPolicySetsAsync()
    {
        if (_policySetService == null) return;

        IsBusy = true;
        StatusText = "Loading policy sets...";

        try
        {
            var sets = await _policySetService.ListPolicySetsAsync();
            PolicySets = new ObservableCollection<PolicySet>(sets);
            _policySetsLoaded = true;
            ApplyFilter();

            if (ActiveProfile?.TenantId != null)
            {
                _cacheService.Set(ActiveProfile.TenantId, CacheKeyPolicySets, sets);
                DebugLog.Log("Cache", $"Saved {sets.Count} policy set(s) to cache");
            }

            StatusText = $"Loaded {sets.Count} policy set(s)";
        }
        catch (Exception ex)
        {
            SetError($"Failed to load policy sets: {FormatGraphError(ex)}");
            StatusText = "Error loading policy sets";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // --- Endpoint Security view ---

    private async Task LoadEndpointSecurityIntentsAsync()
    {
        if (_endpointSecurityService == null) return;

        IsBusy = true;
        StatusText = "Loading endpoint security intents...";

        try
        {
            var intents = await _endpointSecurityService.ListEndpointSecurityIntentsAsync();
            EndpointSecurityIntents = new ObservableCollection<DeviceManagementIntent>(intents);
            _endpointSecurityLoaded = true;
            ApplyFilter();

            if (ActiveProfile?.TenantId != null)
            {
                _cacheService.Set(ActiveProfile.TenantId, CacheKeyEndpointSecurity, intents);
                DebugLog.Log("Cache", $"Saved {intents.Count} endpoint security intent(s) to cache");
            }

            StatusText = $"Loaded {intents.Count} endpoint security intent(s)";
        }
        catch (Exception ex)
        {
            SetError($"Failed to load endpoint security intents: {FormatGraphError(ex)}");
            StatusText = "Error loading endpoint security intents";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // --- Administrative Templates view ---

    private async Task LoadAdministrativeTemplatesAsync()
    {
        if (_administrativeTemplateService == null) return;

        IsBusy = true;
        StatusText = "Loading administrative templates...";

        try
        {
            var templates = await _administrativeTemplateService.ListAdministrativeTemplatesAsync();
            AdministrativeTemplates = new ObservableCollection<GroupPolicyConfiguration>(templates);
            _administrativeTemplatesLoaded = true;
            ApplyFilter();

            if (ActiveProfile?.TenantId != null)
            {
                _cacheService.Set(ActiveProfile.TenantId, CacheKeyAdministrativeTemplates, templates);
                DebugLog.Log("Cache", $"Saved {templates.Count} administrative template(s) to cache");
            }

            StatusText = $"Loaded {templates.Count} administrative template(s)";
        }
        catch (Exception ex)
        {
            SetError($"Failed to load administrative templates: {FormatGraphError(ex)}");
            StatusText = "Error loading administrative templates";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // --- Enrollment Configurations view ---

    private async Task LoadEnrollmentConfigurationsAsync()
    {
        if (_enrollmentConfigurationService == null) return;

        IsBusy = true;
        StatusText = "Loading enrollment configurations...";

        try
        {
            var configurations = await _enrollmentConfigurationService.ListEnrollmentConfigurationsAsync();
            EnrollmentConfigurations = new ObservableCollection<DeviceEnrollmentConfiguration>(configurations);
            _enrollmentConfigurationsLoaded = true;
            ApplyFilter();

            if (ActiveProfile?.TenantId != null)
            {
                _cacheService.Set(ActiveProfile.TenantId, CacheKeyEnrollmentConfigurations, configurations);
                DebugLog.Log("Cache", $"Saved {configurations.Count} enrollment configuration(s) to cache");
            }

            StatusText = $"Loaded {configurations.Count} enrollment configuration(s)";
        }
        catch (Exception ex)
        {
            SetError($"Failed to load enrollment configurations: {FormatGraphError(ex)}");
            StatusText = "Error loading enrollment configurations";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // --- App Protection Policies view ---

    private async Task LoadAppProtectionPoliciesAsync()
    {
        if (_appProtectionPolicyService == null) return;

        IsBusy = true;
        StatusText = "Loading app protection policies...";

        try
        {
            var policies = await _appProtectionPolicyService.ListAppProtectionPoliciesAsync();
            AppProtectionPolicies = new ObservableCollection<ManagedAppPolicy>(policies);
            _appProtectionPoliciesLoaded = true;
            ApplyFilter();

            if (ActiveProfile?.TenantId != null)
            {
                _cacheService.Set(ActiveProfile.TenantId, CacheKeyAppProtectionPolicies, policies);
                DebugLog.Log("Cache", $"Saved {policies.Count} app protection policy(ies) to cache");
            }

            StatusText = $"Loaded {policies.Count} app protection policy(ies)";
        }
        catch (Exception ex)
        {
            SetError($"Failed to load app protection policies: {FormatGraphError(ex)}");
            StatusText = "Error loading app protection policies";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // --- Managed Device App Configurations view ---

    private async Task LoadManagedDeviceAppConfigurationsAsync()
    {
        if (_managedAppConfigurationService == null) return;

        IsBusy = true;
        StatusText = "Loading managed device app configurations...";

        try
        {
            var configurations = await _managedAppConfigurationService.ListManagedDeviceAppConfigurationsAsync();
            ManagedDeviceAppConfigurations = new ObservableCollection<ManagedDeviceMobileAppConfiguration>(configurations);
            _managedDeviceAppConfigurationsLoaded = true;
            ApplyFilter();

            if (ActiveProfile?.TenantId != null)
            {
                _cacheService.Set(ActiveProfile.TenantId, CacheKeyManagedDeviceAppConfigurations, configurations);
                DebugLog.Log("Cache", $"Saved {configurations.Count} managed device app configuration(s) to cache");
            }

            StatusText = $"Loaded {configurations.Count} managed device app configuration(s)";
        }
        catch (Exception ex)
        {
            SetError($"Failed to load managed device app configurations: {FormatGraphError(ex)}");
            StatusText = "Error loading managed device app configurations";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // --- Targeted Managed App Configurations view ---

    private async Task LoadTargetedManagedAppConfigurationsAsync()
    {
        if (_managedAppConfigurationService == null) return;

        IsBusy = true;
        StatusText = "Loading targeted managed app configurations...";

        try
        {
            var configurations = await _managedAppConfigurationService.ListTargetedManagedAppConfigurationsAsync();
            TargetedManagedAppConfigurations = new ObservableCollection<TargetedManagedAppConfiguration>(configurations);
            _targetedManagedAppConfigurationsLoaded = true;
            ApplyFilter();

            if (ActiveProfile?.TenantId != null)
            {
                _cacheService.Set(ActiveProfile.TenantId, CacheKeyTargetedManagedAppConfigurations, configurations);
                DebugLog.Log("Cache", $"Saved {configurations.Count} targeted managed app configuration(s) to cache");
            }

            StatusText = $"Loaded {configurations.Count} targeted managed app configuration(s)";
        }
        catch (Exception ex)
        {
            SetError($"Failed to load targeted managed app configurations: {FormatGraphError(ex)}");
            StatusText = "Error loading targeted managed app configurations";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // --- Terms and Conditions view ---

    private async Task LoadTermsAndConditionsAsync()
    {
        if (_termsAndConditionsService == null) return;

        IsBusy = true;
        StatusText = "Loading terms and conditions...";

        try
        {
            var termsCollection = await _termsAndConditionsService.ListTermsAndConditionsAsync();
            TermsAndConditionsCollection = new ObservableCollection<TermsAndConditions>(termsCollection);
            _termsAndConditionsLoaded = true;
            ApplyFilter();

            if (ActiveProfile?.TenantId != null)
            {
                _cacheService.Set(ActiveProfile.TenantId, CacheKeyTermsAndConditions, termsCollection);
                DebugLog.Log("Cache", $"Saved {termsCollection.Count} terms and conditions item(s) to cache");
            }

            StatusText = $"Loaded {termsCollection.Count} terms and conditions item(s)";
        }
        catch (Exception ex)
        {
            SetError($"Failed to load terms and conditions: {FormatGraphError(ex)}");
            StatusText = "Error loading terms and conditions";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // --- Scope Tags view ---

    private async Task LoadScopeTagsAsync()
    {
        if (_scopeTagService == null) return;

        IsBusy = true;
        StatusText = "Loading scope tags...";

        try
        {
            var scopeTags = await _scopeTagService.ListScopeTagsAsync();
            ScopeTags = new ObservableCollection<RoleScopeTag>(scopeTags);
            _scopeTagsLoaded = true;
            ApplyFilter();

            if (ActiveProfile?.TenantId != null)
            {
                _cacheService.Set(ActiveProfile.TenantId, CacheKeyScopeTags, scopeTags);
                DebugLog.Log("Cache", $"Saved {scopeTags.Count} scope tag(s) to cache");
            }

            StatusText = $"Loaded {scopeTags.Count} scope tag(s)";
        }
        catch (Exception ex)
        {
            SetError($"Failed to load scope tags: {FormatGraphError(ex)}");
            StatusText = "Error loading scope tags";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // --- Role Definitions view ---

    private async Task LoadRoleDefinitionsAsync()
    {
        if (_roleDefinitionService == null) return;

        IsBusy = true;
        StatusText = "Loading role definitions...";

        try
        {
            var roleDefinitions = await _roleDefinitionService.ListRoleDefinitionsAsync();
            RoleDefinitions = new ObservableCollection<RoleDefinition>(roleDefinitions);
            _roleDefinitionsLoaded = true;
            ApplyFilter();

            if (ActiveProfile?.TenantId != null)
            {
                _cacheService.Set(ActiveProfile.TenantId, CacheKeyRoleDefinitions, roleDefinitions);
                DebugLog.Log("Cache", $"Saved {roleDefinitions.Count} role definition(s) to cache");
            }

            StatusText = $"Loaded {roleDefinitions.Count} role definition(s)";
        }
        catch (Exception ex)
        {
            SetError($"Failed to load role definitions: {FormatGraphError(ex)}");
            StatusText = "Error loading role definitions";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // --- Intune Branding view ---

    private async Task LoadIntuneBrandingProfilesAsync()
    {
        if (_intuneBrandingService == null) return;

        IsBusy = true;
        StatusText = "Loading Intune branding profiles...";

        try
        {
            var profiles = await _intuneBrandingService.ListIntuneBrandingProfilesAsync();
            IntuneBrandingProfiles = new ObservableCollection<IntuneBrandingProfile>(profiles);
            _intuneBrandingProfilesLoaded = true;
            ApplyFilter();

            if (ActiveProfile?.TenantId != null)
            {
                _cacheService.Set(ActiveProfile.TenantId, CacheKeyIntuneBrandingProfiles, profiles);
                DebugLog.Log("Cache", $"Saved {profiles.Count} Intune branding profile(s) to cache");
            }

            StatusText = $"Loaded {profiles.Count} Intune branding profile(s)";
        }
        catch (Exception ex)
        {
            SetError($"Failed to load Intune branding profiles: {FormatGraphError(ex)}");
            StatusText = "Error loading Intune branding profiles";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // --- Azure Branding view ---

    private async Task LoadAzureBrandingLocalizationsAsync()
    {
        if (_azureBrandingService == null) return;

        IsBusy = true;
        StatusText = "Loading Azure branding localizations...";

        try
        {
            var localizations = await _azureBrandingService.ListBrandingLocalizationsAsync();
            AzureBrandingLocalizations = new ObservableCollection<OrganizationalBrandingLocalization>(localizations);
            _azureBrandingLocalizationsLoaded = true;
            ApplyFilter();

            if (ActiveProfile?.TenantId != null)
            {
                _cacheService.Set(ActiveProfile.TenantId, CacheKeyAzureBrandingLocalizations, localizations);
                DebugLog.Log("Cache", $"Saved {localizations.Count} Azure branding localization(s) to cache");
            }

            StatusText = $"Loaded {localizations.Count} Azure branding localization(s)";
        }
        catch (Exception ex)
        {
            SetError($"Failed to load Azure branding localizations: {FormatGraphError(ex)}");
            StatusText = "Error loading Azure branding localizations";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static GroupRow BuildGroupRow(Microsoft.Graph.Beta.Models.Group group, GroupMemberCounts counts)
    {
        return new GroupRow
        {
            GroupName = group.DisplayName ?? "",
            Description = group.Description ?? "",
            MembershipRule = group.MembershipRule ?? "",
            ProcessingState = group.MembershipRuleProcessingState ?? "",
            GroupType = GroupService.InferGroupType(group),
            TotalMembers = counts.Total.ToString(CultureInfo.InvariantCulture),
            Users = counts.Users.ToString(CultureInfo.InvariantCulture),
            Devices = counts.Devices.ToString(CultureInfo.InvariantCulture),
            NestedGroups = counts.NestedGroups.ToString(CultureInfo.InvariantCulture),
            SecurityEnabled = group.SecurityEnabled == true ? "Yes" : "No",
            MailEnabled = group.MailEnabled == true ? "Yes" : "No",
            CreatedDate = group.CreatedDateTime?.ToString("g", CultureInfo.InvariantCulture) ?? "",
            GroupId = group.Id ?? ""
        };
    }

    // --- Connection ---

    private async void OnLoginSucceeded(object? sender, TenantProfile profile)
    {
        await ConnectToProfile(profile);
    }

    private async Task ConnectToProfile(TenantProfile profile)
    {
        ClearError();
        IsBusy = true;
        StatusText = $"Connecting to {profile.Name}...";
        DebugLog.Log("Auth", $"Authenticating to tenant {profile.TenantId} ({profile.Cloud}) as {profile.ClientId}");

        try
        {
            ActiveProfile = profile;
            IsConnected = true;
            WindowTitle = $"Intune Commander - {profile.Name}";
            CurrentView = null;

            _graphClient = await _graphClientFactory.CreateClientAsync(profile);
            DebugLog.Log("Auth", "Graph client created successfully");
            _configProfileService = new ConfigurationProfileService(_graphClient);
            _compliancePolicyService = new CompliancePolicyService(_graphClient);
            _applicationService = new ApplicationService(_graphClient);
            _groupService = new GroupService(_graphClient);
            _settingsCatalogService = new SettingsCatalogService(_graphClient);
            _conditionalAccessPolicyService = new ConditionalAccessPolicyService(_graphClient);
            _assignmentFilterService = new AssignmentFilterService(_graphClient);
            _policySetService = new PolicySetService(_graphClient);
            _endpointSecurityService = new EndpointSecurityService(_graphClient);
            _administrativeTemplateService = new AdministrativeTemplateService(_graphClient);
            _enrollmentConfigurationService = new EnrollmentConfigurationService(_graphClient);
            _appProtectionPolicyService = new AppProtectionPolicyService(_graphClient);
            _managedAppConfigurationService = new ManagedAppConfigurationService(_graphClient);
            _termsAndConditionsService = new TermsAndConditionsService(_graphClient);
            _scopeTagService = new ScopeTagService(_graphClient);
            _roleDefinitionService = new RoleDefinitionService(_graphClient);
            _intuneBrandingService = new IntuneBrandingService(_graphClient);
            _azureBrandingService = new AzureBrandingService(_graphClient);
            _importService = new ImportService(
                _configProfileService,
                _compliancePolicyService,
                _endpointSecurityService,
                _administrativeTemplateService,
                _enrollmentConfigurationService,
                _appProtectionPolicyService,
                _managedAppConfigurationService,
                _termsAndConditionsService,
                _scopeTagService,
                _roleDefinitionService,
                _intuneBrandingService,
                _azureBrandingService);

            RefreshSwitcherProfiles();
            SelectedSwitchProfile = profile;

            // Default to first nav category
            SelectedCategory = NavCategories.FirstOrDefault();

            StatusText = $"Connected to {profile.Name}";
            DebugLog.Log("Auth", $"Connected to {profile.Name}");

            // Try loading cached data — if all primary types are cached, skip Graph refresh
            var cachedCount = TryLoadFromCache(profile.TenantId ?? "");
            if (cachedCount >= 18)
            {
                DebugLog.Log("Cache", "All data loaded from cache — skipping Graph refresh");
                IsBusy = false;
            }
            else
            {
                if (cachedCount > 0)
                    DebugLog.Log("Cache", $"Partial cache hit ({cachedCount}/18) — refreshing from Graph");
                await RefreshAsync(CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            DebugLog.LogError($"Connection to {profile.Name} failed: {FormatGraphError(ex)}", ex);
            SetError($"Connection failed: {FormatGraphError(ex)}");
            StatusText = "Connection failed";
            DisconnectInternal();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RefreshSwitcherProfiles()
    {
        SwitcherProfiles.Clear();
        foreach (var p in _profileService.Profiles)
            SwitcherProfiles.Add(p);
    }

    partial void OnSelectedSwitchProfileChanged(TenantProfile? value)
    {
        if (value is null || !IsConnected || value.Id == ActiveProfile?.Id)
            return;

        _ = RequestSwitchAsync(value);
    }

    private async Task RequestSwitchAsync(TenantProfile target)
    {
        if (SwitchProfileRequested is not null)
        {
            var confirmed = await SwitchProfileRequested.Invoke(target);
            if (confirmed)
            {
                DisconnectInternal();
                await ConnectToProfile(target);
                target.LastUsed = DateTime.UtcNow;
                _profileService.SetActiveProfile(target.Id);
                await _profileService.SaveAsync();
            }
            else
            {
                SelectedSwitchProfile = ActiveProfile;
            }
        }
    }

    // --- Refresh (loads data for the selected category) ---

    /// <summary>
    /// Extracts a human-readable error message from an ODataError, including
    /// the response status code, error code, and inner error details.
    /// </summary>
    private static string FormatODataError(ODataError odataError)
    {
        var sb = new StringBuilder();
        sb.Append($"HTTP {odataError.ResponseStatusCode}");

        if (odataError.Error != null)
        {
            if (!string.IsNullOrEmpty(odataError.Error.Code))
                sb.Append($" [{odataError.Error.Code}]");
            if (!string.IsNullOrEmpty(odataError.Error.Message))
                sb.Append($": {odataError.Error.Message}");
        }
        else if (!string.IsNullOrEmpty(odataError.Message))
        {
            sb.Append($": {odataError.Message}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats any exception for display, with special handling for ODataError.
    /// </summary>
    private static string FormatGraphError(Exception ex)
    {
        return ex is ODataError odata ? FormatODataError(odata) : ex.Message;
    }

    [RelayCommand]
    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        ClearError();
        IsBusy = true;
        DebugLog.Log("Graph", "Refreshing data from Graph API...");
        var errors = new List<string>();
        var loadConditionalAccess = IsConditionalAccessCategory;
        var loadAssignmentFilters = IsAssignmentFiltersCategory;
        var loadPolicySets = IsPolicySetsCategory;
        var loadEndpointSecurity = IsEndpointSecurityCategory;
        var loadAdministrativeTemplates = IsAdministrativeTemplatesCategory;
        var loadEnrollmentConfigurations = IsEnrollmentConfigurationsCategory;
        var loadAppProtectionPolicies = IsAppProtectionPoliciesCategory;
        var loadManagedDeviceAppConfigurations = IsManagedDeviceAppConfigurationsCategory;
        var loadTargetedManagedAppConfigurations = IsTargetedManagedAppConfigurationsCategory;
        var loadTermsAndConditions = IsTermsAndConditionsCategory;
        var loadScopeTags = IsScopeTagsCategory;
        var loadRoleDefinitions = IsRoleDefinitionsCategory;
        var loadIntuneBranding = IsIntuneBrandingCategory;
        var loadAzureBranding = IsAzureBrandingCategory;

        try
        {
            if (_configProfileService != null)
            {
                try
                {
                    StatusText = "Loading device configurations...";
                    var configs = await _configProfileService.ListDeviceConfigurationsAsync(cancellationToken);
                    DeviceConfigurations = new ObservableCollection<DeviceConfiguration>(configs);
                    DebugLog.Log("Graph", $"Loaded {configs.Count} device configuration(s)");
                }
                catch (Exception ex)
                {
                    var detail = FormatGraphError(ex);
                    DebugLog.LogError($"Failed to load device configurations: {detail}", ex);
                    errors.Add($"Device Configs: {detail}");
                }
            }

            if (_compliancePolicyService != null)
            {
                try
                {
                    StatusText = "Loading compliance policies...";
                    var policies = await _compliancePolicyService.ListCompliancePoliciesAsync(cancellationToken);
                    CompliancePolicies = new ObservableCollection<DeviceCompliancePolicy>(policies);
                    DebugLog.Log("Graph", $"Loaded {policies.Count} compliance policy(ies)");
                }
                catch (Exception ex)
                {
                    var detail = FormatGraphError(ex);
                    DebugLog.LogError($"Failed to load compliance policies: {detail}", ex);
                    errors.Add($"Compliance Policies: {detail}");
                }
            }

            if (_applicationService != null)
            {
                try
                {
                    StatusText = "Loading applications...";
                    var apps = await _applicationService.ListApplicationsAsync(cancellationToken);
                    Applications = new ObservableCollection<MobileApp>(apps);
                    DebugLog.Log("Graph", $"Loaded {apps.Count} application(s)");
                }
                catch (Exception ex)
                {
                    var detail = FormatGraphError(ex);
                    DebugLog.LogError($"Failed to load applications: {detail}", ex);
                    errors.Add($"Applications: {detail}");
                }
            }

            if (_settingsCatalogService != null)
            {
                try
                {
                    StatusText = "Loading settings catalog policies...";
                    var settingsPolicies = await _settingsCatalogService.ListSettingsCatalogPoliciesAsync(cancellationToken);
                    SettingsCatalogPolicies = new ObservableCollection<DeviceManagementConfigurationPolicy>(settingsPolicies);
                    DebugLog.Log("Graph", $"Loaded {settingsPolicies.Count} settings catalog policy(ies)");
                }
                catch (Exception ex)
                {
                    var detail = FormatGraphError(ex);
                    DebugLog.LogError($"Failed to load settings catalog: {detail}", ex);
                    errors.Add($"Settings Catalog: {detail}");
                }
            }

            if (_conditionalAccessPolicyService != null && loadConditionalAccess)
            {
                try
                {
                    StatusText = "Loading conditional access policies...";
                    var policies = await _conditionalAccessPolicyService.ListPoliciesAsync(cancellationToken);
                    ConditionalAccessPolicies = new ObservableCollection<ConditionalAccessPolicy>(policies);
                    _conditionalAccessLoaded = true;
                    DebugLog.Log("Graph", $"Loaded {policies.Count} conditional access policy(ies)");
                }
                catch (Exception ex)
                {
                    _conditionalAccessLoaded = false;
                    var detail = FormatGraphError(ex);
                    DebugLog.LogError($"Failed to load conditional access policies: {detail}", ex);
                    errors.Add($"Conditional Access: {detail}");
                }
            }
            else if (_conditionalAccessPolicyService != null)
            {
                DebugLog.Log("Graph", "Skipping conditional access refresh (lazy-load when tab selected)");
            }

            if (_endpointSecurityService != null && loadEndpointSecurity)
            {
                try
                {
                    StatusText = "Loading endpoint security intents...";
                    var intents = await _endpointSecurityService.ListEndpointSecurityIntentsAsync(cancellationToken);
                    EndpointSecurityIntents = new ObservableCollection<DeviceManagementIntent>(intents);
                    _endpointSecurityLoaded = true;
                    DebugLog.Log("Graph", $"Loaded {intents.Count} endpoint security intent(s)");
                }
                catch (Exception ex)
                {
                    _endpointSecurityLoaded = false;
                    var detail = FormatGraphError(ex);
                    DebugLog.LogError($"Failed to load endpoint security intents: {detail}", ex);
                    errors.Add($"Endpoint Security: {detail}");
                }
            }
            else if (_endpointSecurityService != null)
            {
                DebugLog.Log("Graph", "Skipping endpoint security refresh (lazy-load when tab selected)");
            }

            if (_administrativeTemplateService != null && loadAdministrativeTemplates)
            {
                try
                {
                    StatusText = "Loading administrative templates...";
                    var templates = await _administrativeTemplateService.ListAdministrativeTemplatesAsync(cancellationToken);
                    AdministrativeTemplates = new ObservableCollection<GroupPolicyConfiguration>(templates);
                    _administrativeTemplatesLoaded = true;
                    DebugLog.Log("Graph", $"Loaded {templates.Count} administrative template(s)");
                }
                catch (Exception ex)
                {
                    _administrativeTemplatesLoaded = false;
                    var detail = FormatGraphError(ex);
                    DebugLog.LogError($"Failed to load administrative templates: {detail}", ex);
                    errors.Add($"Administrative Templates: {detail}");
                }
            }
            else if (_administrativeTemplateService != null)
            {
                DebugLog.Log("Graph", "Skipping administrative templates refresh (lazy-load when tab selected)");
            }

            if (_enrollmentConfigurationService != null && loadEnrollmentConfigurations)
            {
                try
                {
                    StatusText = "Loading enrollment configurations...";
                    var configurations = await _enrollmentConfigurationService.ListEnrollmentConfigurationsAsync(cancellationToken);
                    EnrollmentConfigurations = new ObservableCollection<DeviceEnrollmentConfiguration>(configurations);
                    _enrollmentConfigurationsLoaded = true;
                    DebugLog.Log("Graph", $"Loaded {configurations.Count} enrollment configuration(s)");
                }
                catch (Exception ex)
                {
                    _enrollmentConfigurationsLoaded = false;
                    var detail = FormatGraphError(ex);
                    DebugLog.LogError($"Failed to load enrollment configurations: {detail}", ex);
                    errors.Add($"Enrollment Configurations: {detail}");
                }
            }
            else if (_enrollmentConfigurationService != null)
            {
                DebugLog.Log("Graph", "Skipping enrollment configurations refresh (lazy-load when tab selected)");
            }

            if (_appProtectionPolicyService != null && loadAppProtectionPolicies)
            {
                try
                {
                    StatusText = "Loading app protection policies...";
                    var policies = await _appProtectionPolicyService.ListAppProtectionPoliciesAsync(cancellationToken);
                    AppProtectionPolicies = new ObservableCollection<ManagedAppPolicy>(policies);
                    _appProtectionPoliciesLoaded = true;
                    DebugLog.Log("Graph", $"Loaded {policies.Count} app protection policy(ies)");
                }
                catch (Exception ex)
                {
                    _appProtectionPoliciesLoaded = false;
                    var detail = FormatGraphError(ex);
                    DebugLog.LogError($"Failed to load app protection policies: {detail}", ex);
                    errors.Add($"App Protection Policies: {detail}");
                }
            }
            else if (_appProtectionPolicyService != null)
            {
                DebugLog.Log("Graph", "Skipping app protection policies refresh (lazy-load when tab selected)");
            }

            if (_managedAppConfigurationService != null && loadManagedDeviceAppConfigurations)
            {
                try
                {
                    StatusText = "Loading managed device app configurations...";
                    var configurations = await _managedAppConfigurationService.ListManagedDeviceAppConfigurationsAsync(cancellationToken);
                    ManagedDeviceAppConfigurations = new ObservableCollection<ManagedDeviceMobileAppConfiguration>(configurations);
                    _managedDeviceAppConfigurationsLoaded = true;
                    DebugLog.Log("Graph", $"Loaded {configurations.Count} managed device app configuration(s)");
                }
                catch (Exception ex)
                {
                    _managedDeviceAppConfigurationsLoaded = false;
                    var detail = FormatGraphError(ex);
                    DebugLog.LogError($"Failed to load managed device app configurations: {detail}", ex);
                    errors.Add($"Managed Device App Configurations: {detail}");
                }
            }
            else if (_managedAppConfigurationService != null)
            {
                DebugLog.Log("Graph", "Skipping managed device app configurations refresh (lazy-load when tab selected)");
            }

            if (_managedAppConfigurationService != null && loadTargetedManagedAppConfigurations)
            {
                try
                {
                    StatusText = "Loading targeted managed app configurations...";
                    var configurations = await _managedAppConfigurationService.ListTargetedManagedAppConfigurationsAsync(cancellationToken);
                    TargetedManagedAppConfigurations = new ObservableCollection<TargetedManagedAppConfiguration>(configurations);
                    _targetedManagedAppConfigurationsLoaded = true;
                    DebugLog.Log("Graph", $"Loaded {configurations.Count} targeted managed app configuration(s)");
                }
                catch (Exception ex)
                {
                    _targetedManagedAppConfigurationsLoaded = false;
                    var detail = FormatGraphError(ex);
                    DebugLog.LogError($"Failed to load targeted managed app configurations: {detail}", ex);
                    errors.Add($"Targeted Managed App Configurations: {detail}");
                }
            }
            else if (_managedAppConfigurationService != null)
            {
                DebugLog.Log("Graph", "Skipping targeted managed app configurations refresh (lazy-load when tab selected)");
            }

            if (_termsAndConditionsService != null && loadTermsAndConditions)
            {
                try
                {
                    StatusText = "Loading terms and conditions...";
                    var termsCollection = await _termsAndConditionsService.ListTermsAndConditionsAsync(cancellationToken);
                    TermsAndConditionsCollection = new ObservableCollection<TermsAndConditions>(termsCollection);
                    _termsAndConditionsLoaded = true;
                    DebugLog.Log("Graph", $"Loaded {termsCollection.Count} terms and conditions item(s)");
                }
                catch (Exception ex)
                {
                    _termsAndConditionsLoaded = false;
                    var detail = FormatGraphError(ex);
                    DebugLog.LogError($"Failed to load terms and conditions: {detail}", ex);
                    errors.Add($"Terms and Conditions: {detail}");
                }
            }
            else if (_termsAndConditionsService != null)
            {
                DebugLog.Log("Graph", "Skipping terms and conditions refresh (lazy-load when tab selected)");
            }

            if (_scopeTagService != null && loadScopeTags)
            {
                try
                {
                    StatusText = "Loading scope tags...";
                    var scopeTags = await _scopeTagService.ListScopeTagsAsync(cancellationToken);
                    ScopeTags = new ObservableCollection<RoleScopeTag>(scopeTags);
                    _scopeTagsLoaded = true;
                    DebugLog.Log("Graph", $"Loaded {scopeTags.Count} scope tag(s)");
                }
                catch (Exception ex)
                {
                    _scopeTagsLoaded = false;
                    var detail = FormatGraphError(ex);
                    DebugLog.LogError($"Failed to load scope tags: {detail}", ex);
                    errors.Add($"Scope Tags: {detail}");
                }
            }
            else if (_scopeTagService != null)
            {
                DebugLog.Log("Graph", "Skipping scope tags refresh (lazy-load when tab selected)");
            }

            if (_roleDefinitionService != null && loadRoleDefinitions)
            {
                try
                {
                    StatusText = "Loading role definitions...";
                    var roleDefinitions = await _roleDefinitionService.ListRoleDefinitionsAsync(cancellationToken);
                    RoleDefinitions = new ObservableCollection<RoleDefinition>(roleDefinitions);
                    _roleDefinitionsLoaded = true;
                    DebugLog.Log("Graph", $"Loaded {roleDefinitions.Count} role definition(s)");
                }
                catch (Exception ex)
                {
                    _roleDefinitionsLoaded = false;
                    var detail = FormatGraphError(ex);
                    DebugLog.LogError($"Failed to load role definitions: {detail}", ex);
                    errors.Add($"Role Definitions: {detail}");
                }
            }
            else if (_roleDefinitionService != null)
            {
                DebugLog.Log("Graph", "Skipping role definitions refresh (lazy-load when tab selected)");
            }

            if (_intuneBrandingService != null && loadIntuneBranding)
            {
                try
                {
                    StatusText = "Loading Intune branding profiles...";
                    var profiles = await _intuneBrandingService.ListIntuneBrandingProfilesAsync(cancellationToken);
                    IntuneBrandingProfiles = new ObservableCollection<IntuneBrandingProfile>(profiles);
                    _intuneBrandingProfilesLoaded = true;
                    DebugLog.Log("Graph", $"Loaded {profiles.Count} Intune branding profile(s)");
                }
                catch (Exception ex)
                {
                    _intuneBrandingProfilesLoaded = false;
                    var detail = FormatGraphError(ex);
                    DebugLog.LogError($"Failed to load Intune branding profiles: {detail}", ex);
                    errors.Add($"Intune Branding: {detail}");
                }
            }
            else if (_intuneBrandingService != null)
            {
                DebugLog.Log("Graph", "Skipping Intune branding refresh (lazy-load when tab selected)");
            }

            if (_azureBrandingService != null && loadAzureBranding)
            {
                try
                {
                    StatusText = "Loading Azure branding localizations...";
                    var localizations = await _azureBrandingService.ListBrandingLocalizationsAsync(cancellationToken);
                    AzureBrandingLocalizations = new ObservableCollection<OrganizationalBrandingLocalization>(localizations);
                    _azureBrandingLocalizationsLoaded = true;
                    DebugLog.Log("Graph", $"Loaded {localizations.Count} Azure branding localization(s)");
                }
                catch (Exception ex)
                {
                    _azureBrandingLocalizationsLoaded = false;
                    var detail = FormatGraphError(ex);
                    DebugLog.LogError($"Failed to load Azure branding localizations: {detail}", ex);
                    errors.Add($"Azure Branding: {detail}");
                }
            }
            else if (_azureBrandingService != null)
            {
                DebugLog.Log("Graph", "Skipping Azure branding refresh (lazy-load when tab selected)");
            }

            if (_assignmentFilterService != null && loadAssignmentFilters)
            {
                try
                {
                    StatusText = "Loading assignment filters...";
                    var filters = await _assignmentFilterService.ListFiltersAsync(cancellationToken);
                    AssignmentFilters = new ObservableCollection<DeviceAndAppManagementAssignmentFilter>(filters);
                    _assignmentFiltersLoaded = true;
                    DebugLog.Log("Graph", $"Loaded {filters.Count} assignment filter(s)");
                }
                catch (Exception ex)
                {
                    _assignmentFiltersLoaded = false;
                    var detail = FormatGraphError(ex);
                    DebugLog.LogError($"Failed to load assignment filters: {detail}", ex);
                    errors.Add($"Assignment Filters: {detail}");
                }
            }
            else if (_assignmentFilterService != null)
            {
                DebugLog.Log("Graph", "Skipping assignment filter refresh (lazy-load when tab selected)");
            }

            if (_policySetService != null && loadPolicySets)
            {
                try
                {
                    StatusText = "Loading policy sets...";
                    var sets = await _policySetService.ListPolicySetsAsync(cancellationToken);
                    PolicySets = new ObservableCollection<PolicySet>(sets);
                    _policySetsLoaded = true;
                    DebugLog.Log("Graph", $"Loaded {sets.Count} policy set(s)");
                }
                catch (Exception ex)
                {
                    _policySetsLoaded = false;
                    var detail = FormatGraphError(ex);
                    DebugLog.LogError($"Failed to load policy sets: {detail}", ex);
                    errors.Add($"Policy Sets: {detail}");
                }
            }
            else if (_policySetService != null)
            {
                DebugLog.Log("Graph", "Skipping policy sets refresh (lazy-load when tab selected)");
            }

            var totalItems = DeviceConfigurations.Count + CompliancePolicies.Count + Applications.Count + SettingsCatalogPolicies.Count + EndpointSecurityIntents.Count + AdministrativeTemplates.Count + EnrollmentConfigurations.Count + AppProtectionPolicies.Count + ManagedDeviceAppConfigurations.Count + TargetedManagedAppConfigurations.Count + TermsAndConditionsCollection.Count + ScopeTags.Count + RoleDefinitions.Count + IntuneBrandingProfiles.Count + AzureBrandingLocalizations.Count + ConditionalAccessPolicies.Count + AssignmentFilters.Count + PolicySets.Count;
            StatusText = $"Loaded {totalItems} item(s) ({DeviceConfigurations.Count} configs, {CompliancePolicies.Count} compliance, {Applications.Count} apps, {SettingsCatalogPolicies.Count} settings catalog, {EndpointSecurityIntents.Count} endpoint security, {AdministrativeTemplates.Count} admin templates, {EnrollmentConfigurations.Count} enrollment configs, {AppProtectionPolicies.Count} app protection, {ManagedDeviceAppConfigurations.Count} managed device app configs, {TargetedManagedAppConfigurations.Count} targeted app configs, {TermsAndConditionsCollection.Count} terms, {ScopeTags.Count} scope tags, {RoleDefinitions.Count} role definitions, {IntuneBrandingProfiles.Count} intune branding, {AzureBrandingLocalizations.Count} azure branding, {ConditionalAccessPolicies.Count} conditional access, {AssignmentFilters.Count} filters, {PolicySets.Count} policy sets)";

            if (errors.Count > 0)
                SetError($"Some data failed to load — {string.Join("; ", errors)}");

            // Save successful loads to cache
            if (ActiveProfile?.TenantId != null)
                SaveToCache(ActiveProfile.TenantId);

            ApplyFilter();

            // Reset lazy-load state; actual loading is triggered when navigating to those tabs
            _appAssignmentsLoaded = false;
            _dynamicGroupsLoaded = false;
            _assignedGroupsLoaded = false;

            // Invalidate lazy-load caches so they reload from Graph on next tab visit
            if (ActiveProfile?.TenantId != null)
            {
                _cacheService.Invalidate(ActiveProfile.TenantId, CacheKeyAppAssignments);
                _cacheService.Invalidate(ActiveProfile.TenantId, CacheKeyDynamicGroups);
                _cacheService.Invalidate(ActiveProfile.TenantId, CacheKeyAssignedGroups);
            }
        }
        catch (Exception ex)
        {
            SetError($"Failed to load data: {FormatGraphError(ex)}");
            StatusText = "Error loading data";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // --- Export ---

    [RelayCommand]
    private async Task ExportSelectedAsync(CancellationToken cancellationToken)
    {
        ClearError();
        IsBusy = true;

        try
        {
            var outputPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "IntuneExport");

            var migrationTable = new MigrationTable();

            if (IsDeviceConfigCategory && SelectedConfiguration != null)
            {
                StatusText = $"Exporting {SelectedConfiguration.DisplayName}...";
                await _exportService.ExportDeviceConfigurationAsync(
                    SelectedConfiguration, outputPath, migrationTable, cancellationToken);
            }
            else if (IsCompliancePolicyCategory && SelectedCompliancePolicy != null)
            {
                StatusText = $"Exporting {SelectedCompliancePolicy.DisplayName}...";
                var assignments = _compliancePolicyService != null && SelectedCompliancePolicy.Id != null
                    ? await _compliancePolicyService.GetAssignmentsAsync(SelectedCompliancePolicy.Id, cancellationToken)
                    : [];
                await _exportService.ExportCompliancePolicyAsync(
                    SelectedCompliancePolicy, assignments, outputPath, migrationTable, cancellationToken);
            }
            else if (IsApplicationCategory && SelectedApplication != null)
            {
                StatusText = $"Exporting {SelectedApplication.DisplayName}...";
                var assignments = _applicationService != null && SelectedApplication.Id != null
                    ? await _applicationService.GetAssignmentsAsync(SelectedApplication.Id, cancellationToken)
                    : [];
                await _exportService.ExportApplicationAsync(
                    SelectedApplication, assignments, outputPath, migrationTable, cancellationToken);
            }
            else if (IsEndpointSecurityCategory && SelectedEndpointSecurityIntent != null)
            {
                StatusText = $"Exporting {SelectedEndpointSecurityIntent.DisplayName}...";
                var assignments = _endpointSecurityService != null && SelectedEndpointSecurityIntent.Id != null
                    ? await _endpointSecurityService.GetAssignmentsAsync(SelectedEndpointSecurityIntent.Id, cancellationToken)
                    : [];
                await _exportService.ExportEndpointSecurityIntentAsync(
                    SelectedEndpointSecurityIntent, assignments, outputPath, migrationTable, cancellationToken);
            }
            else if (IsAdministrativeTemplatesCategory && SelectedAdministrativeTemplate != null)
            {
                StatusText = $"Exporting {SelectedAdministrativeTemplate.DisplayName}...";
                var assignments = _administrativeTemplateService != null && SelectedAdministrativeTemplate.Id != null
                    ? await _administrativeTemplateService.GetAssignmentsAsync(SelectedAdministrativeTemplate.Id, cancellationToken)
                    : [];
                await _exportService.ExportAdministrativeTemplateAsync(
                    SelectedAdministrativeTemplate, assignments, outputPath, migrationTable, cancellationToken);
            }
            else if (IsEnrollmentConfigurationsCategory && SelectedEnrollmentConfiguration != null)
            {
                StatusText = $"Exporting {SelectedEnrollmentConfiguration.DisplayName}...";
                await _exportService.ExportEnrollmentConfigurationAsync(
                    SelectedEnrollmentConfiguration, outputPath, migrationTable, cancellationToken);
            }
            else if (IsAppProtectionPoliciesCategory && SelectedAppProtectionPolicy != null)
            {
                StatusText = $"Exporting {SelectedAppProtectionPolicy.DisplayName}...";
                await _exportService.ExportAppProtectionPolicyAsync(
                    SelectedAppProtectionPolicy, outputPath, migrationTable, cancellationToken);
            }
            else if (IsManagedDeviceAppConfigurationsCategory && SelectedManagedDeviceAppConfiguration != null)
            {
                StatusText = $"Exporting {SelectedManagedDeviceAppConfiguration.DisplayName}...";
                await _exportService.ExportManagedDeviceAppConfigurationAsync(
                    SelectedManagedDeviceAppConfiguration, outputPath, migrationTable, cancellationToken);
            }
            else if (IsTargetedManagedAppConfigurationsCategory && SelectedTargetedManagedAppConfiguration != null)
            {
                StatusText = $"Exporting {SelectedTargetedManagedAppConfiguration.DisplayName}...";
                await _exportService.ExportTargetedManagedAppConfigurationAsync(
                    SelectedTargetedManagedAppConfiguration, outputPath, migrationTable, cancellationToken);
            }
            else if (IsTermsAndConditionsCategory && SelectedTermsAndConditions != null)
            {
                StatusText = $"Exporting {SelectedTermsAndConditions.DisplayName}...";
                await _exportService.ExportTermsAndConditionsAsync(
                    SelectedTermsAndConditions, outputPath, migrationTable, cancellationToken);
            }
            else if (IsScopeTagsCategory && SelectedScopeTag != null)
            {
                StatusText = $"Exporting {SelectedScopeTag.DisplayName}...";
                await _exportService.ExportScopeTagAsync(
                    SelectedScopeTag, outputPath, migrationTable, cancellationToken);
            }
            else if (IsRoleDefinitionsCategory && SelectedRoleDefinition != null)
            {
                StatusText = $"Exporting {SelectedRoleDefinition.DisplayName}...";
                await _exportService.ExportRoleDefinitionAsync(
                    SelectedRoleDefinition, outputPath, migrationTable, cancellationToken);
            }
            else if (IsIntuneBrandingCategory && SelectedIntuneBrandingProfile != null)
            {
                StatusText = $"Exporting {SelectedIntuneBrandingProfile.DisplayName}...";
                await _exportService.ExportIntuneBrandingProfileAsync(
                    SelectedIntuneBrandingProfile, outputPath, migrationTable, cancellationToken);
            }
            else if (IsAzureBrandingCategory && SelectedAzureBrandingLocalization != null)
            {
                StatusText = $"Exporting {SelectedAzureBrandingLocalization.Id ?? "branding localization"}...";
                await _exportService.ExportAzureBrandingLocalizationAsync(
                    SelectedAzureBrandingLocalization, outputPath, migrationTable, cancellationToken);
            }
            else
            {
                StatusText = "Nothing selected to export";
                return;
            }

            await _exportService.SaveMigrationTableAsync(migrationTable, outputPath, cancellationToken);
            StatusText = $"Exported to {outputPath}";
        }
        catch (Exception ex)
        {
            SetError($"Export failed: {FormatGraphError(ex)}");
            StatusText = "Export failed";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportAllAsync(CancellationToken cancellationToken)
    {
        ClearError();
        IsBusy = true;

        try
        {
            var outputPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "IntuneExport");

            var migrationTable = new MigrationTable();
            var count = 0;

            // Export device configs
            if (DeviceConfigurations.Any())
            {
                StatusText = "Exporting device configurations...";
                foreach (var config in DeviceConfigurations)
                {
                    await _exportService.ExportDeviceConfigurationAsync(config, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            // Export compliance policies with assignments
            if (CompliancePolicies.Any() && _compliancePolicyService != null)
            {
                StatusText = "Exporting compliance policies...";
                foreach (var policy in CompliancePolicies)
                {
                    var assignments = policy.Id != null
                        ? await _compliancePolicyService.GetAssignmentsAsync(policy.Id, cancellationToken)
                        : [];
                    await _exportService.ExportCompliancePolicyAsync(policy, assignments, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            // Export applications with assignments
            if (Applications.Any() && _applicationService != null)
            {
                StatusText = "Exporting applications...";
                foreach (var app in Applications)
                {
                    var assignments = app.Id != null
                        ? await _applicationService.GetAssignmentsAsync(app.Id, cancellationToken)
                        : [];
                    await _exportService.ExportApplicationAsync(app, assignments, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            // Export endpoint security intents with assignments
            if (EndpointSecurityIntents.Any() && _endpointSecurityService != null)
            {
                StatusText = "Exporting endpoint security intents...";
                foreach (var intent in EndpointSecurityIntents)
                {
                    var assignments = intent.Id != null
                        ? await _endpointSecurityService.GetAssignmentsAsync(intent.Id, cancellationToken)
                        : [];
                    await _exportService.ExportEndpointSecurityIntentAsync(intent, assignments, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            // Export administrative templates with assignments
            if (AdministrativeTemplates.Any() && _administrativeTemplateService != null)
            {
                StatusText = "Exporting administrative templates...";
                foreach (var template in AdministrativeTemplates)
                {
                    var assignments = template.Id != null
                        ? await _administrativeTemplateService.GetAssignmentsAsync(template.Id, cancellationToken)
                        : [];
                    await _exportService.ExportAdministrativeTemplateAsync(template, assignments, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            // Export enrollment configurations
            if (EnrollmentConfigurations.Any())
            {
                StatusText = "Exporting enrollment configurations...";
                foreach (var configuration in EnrollmentConfigurations)
                {
                    await _exportService.ExportEnrollmentConfigurationAsync(configuration, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            // Export app protection policies
            if (AppProtectionPolicies.Any())
            {
                StatusText = "Exporting app protection policies...";
                foreach (var policy in AppProtectionPolicies)
                {
                    await _exportService.ExportAppProtectionPolicyAsync(policy, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            // Export managed device app configurations
            if (ManagedDeviceAppConfigurations.Any())
            {
                StatusText = "Exporting managed device app configurations...";
                foreach (var configuration in ManagedDeviceAppConfigurations)
                {
                    await _exportService.ExportManagedDeviceAppConfigurationAsync(configuration, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            // Export targeted managed app configurations
            if (TargetedManagedAppConfigurations.Any())
            {
                StatusText = "Exporting targeted managed app configurations...";
                foreach (var configuration in TargetedManagedAppConfigurations)
                {
                    await _exportService.ExportTargetedManagedAppConfigurationAsync(configuration, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            // Export terms and conditions
            if (TermsAndConditionsCollection.Any())
            {
                StatusText = "Exporting terms and conditions...";
                foreach (var termsAndConditions in TermsAndConditionsCollection)
                {
                    await _exportService.ExportTermsAndConditionsAsync(termsAndConditions, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            // Export scope tags
            if (ScopeTags.Any())
            {
                StatusText = "Exporting scope tags...";
                foreach (var scopeTag in ScopeTags)
                {
                    await _exportService.ExportScopeTagAsync(scopeTag, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            // Export role definitions
            if (RoleDefinitions.Any())
            {
                StatusText = "Exporting role definitions...";
                foreach (var roleDefinition in RoleDefinitions)
                {
                    await _exportService.ExportRoleDefinitionAsync(roleDefinition, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            // Export Intune branding profiles
            if (IntuneBrandingProfiles.Any())
            {
                StatusText = "Exporting Intune branding profiles...";
                foreach (var profile in IntuneBrandingProfiles)
                {
                    await _exportService.ExportIntuneBrandingProfileAsync(profile, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            // Export Azure branding localizations
            if (AzureBrandingLocalizations.Any())
            {
                StatusText = "Exporting Azure branding localizations...";
                foreach (var localization in AzureBrandingLocalizations)
                {
                    await _exportService.ExportAzureBrandingLocalizationAsync(localization, outputPath, migrationTable, cancellationToken);
                    count++;
                }
            }

            await _exportService.SaveMigrationTableAsync(migrationTable, outputPath, cancellationToken);
            StatusText = $"Exported {count} item(s) to {outputPath}";
        }
        catch (Exception ex)
        {
            SetError($"Export failed: {FormatGraphError(ex)}");
            StatusText = "Export failed";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // --- Import ---

    [RelayCommand]
    private async Task ImportFromFolderAsync(string folderPath, CancellationToken cancellationToken)
    {
        if (_importService == null) return;

        ClearError();
        IsBusy = true;
        StatusText = "Importing...";

        try
        {
            var migrationTable = await _importService.ReadMigrationTableAsync(folderPath, cancellationToken);
            var imported = 0;

            // Import device configurations
            var configs = await _importService.ReadDeviceConfigurationsFromFolderAsync(folderPath, cancellationToken);
            foreach (var config in configs)
            {
                await _importService.ImportDeviceConfigurationAsync(config, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Import compliance policies
            var policies = await _importService.ReadCompliancePoliciesFromFolderAsync(folderPath, cancellationToken);
            foreach (var export in policies)
            {
                await _importService.ImportCompliancePolicyAsync(export, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Import endpoint security intents
            var endpointIntents = await _importService.ReadEndpointSecurityIntentsFromFolderAsync(folderPath, cancellationToken);
            foreach (var export in endpointIntents)
            {
                await _importService.ImportEndpointSecurityIntentAsync(export, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Import administrative templates
            var templates = await _importService.ReadAdministrativeTemplatesFromFolderAsync(folderPath, cancellationToken);
            foreach (var export in templates)
            {
                await _importService.ImportAdministrativeTemplateAsync(export, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Import enrollment configurations
            var enrollmentConfigs = await _importService.ReadEnrollmentConfigurationsFromFolderAsync(folderPath, cancellationToken);
            foreach (var config in enrollmentConfigs)
            {
                await _importService.ImportEnrollmentConfigurationAsync(config, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Import app protection policies
            var appProtectionPolicies = await _importService.ReadAppProtectionPoliciesFromFolderAsync(folderPath, cancellationToken);
            foreach (var policy in appProtectionPolicies)
            {
                await _importService.ImportAppProtectionPolicyAsync(policy, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Import managed device app configurations
            var managedDeviceAppConfigs = await _importService.ReadManagedDeviceAppConfigurationsFromFolderAsync(folderPath, cancellationToken);
            foreach (var configuration in managedDeviceAppConfigs)
            {
                await _importService.ImportManagedDeviceAppConfigurationAsync(configuration, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Import targeted managed app configurations
            var targetedAppConfigs = await _importService.ReadTargetedManagedAppConfigurationsFromFolderAsync(folderPath, cancellationToken);
            foreach (var configuration in targetedAppConfigs)
            {
                await _importService.ImportTargetedManagedAppConfigurationAsync(configuration, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Import terms and conditions
            var termsCollection = await _importService.ReadTermsAndConditionsFromFolderAsync(folderPath, cancellationToken);
            foreach (var termsAndConditions in termsCollection)
            {
                await _importService.ImportTermsAndConditionsAsync(termsAndConditions, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Import scope tags
            var scopeTags = await _importService.ReadScopeTagsFromFolderAsync(folderPath, cancellationToken);
            foreach (var scopeTag in scopeTags)
            {
                await _importService.ImportScopeTagAsync(scopeTag, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Import role definitions
            var roleDefinitions = await _importService.ReadRoleDefinitionsFromFolderAsync(folderPath, cancellationToken);
            foreach (var roleDefinition in roleDefinitions)
            {
                await _importService.ImportRoleDefinitionAsync(roleDefinition, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Import Intune branding profiles
            var intuneBrandingProfiles = await _importService.ReadIntuneBrandingProfilesFromFolderAsync(folderPath, cancellationToken);
            foreach (var profile in intuneBrandingProfiles)
            {
                await _importService.ImportIntuneBrandingProfileAsync(profile, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Import Azure branding localizations
            var azureBrandingLocalizations = await _importService.ReadAzureBrandingLocalizationsFromFolderAsync(folderPath, cancellationToken);
            foreach (var localization in azureBrandingLocalizations)
            {
                await _importService.ImportAzureBrandingLocalizationAsync(localization, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} item(s)...";
            }

            // Save updated migration table
            await _exportService.SaveMigrationTableAsync(migrationTable, folderPath, cancellationToken);
            StatusText = $"Imported {imported} item(s)";

            await RefreshAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            SetError($"Import failed: {FormatGraphError(ex)}");
            StatusText = "Import failed";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // --- Cache helpers ---

    /// <summary>
    /// Attempts to populate all collections from cached data.
    /// Returns how many data types were loaded.
    /// </summary>
    private int TryLoadFromCache(string tenantId)
    {
        if (string.IsNullOrEmpty(tenantId)) return 0;

        var typesLoaded = 0;
        DateTime? oldestCacheTime = null;

        try
        {
            var configs = _cacheService.Get<DeviceConfiguration>(tenantId, CacheKeyDeviceConfigs);
            if (configs != null)
            {
                DeviceConfigurations = new ObservableCollection<DeviceConfiguration>(configs);
                DebugLog.Log("Cache", $"Loaded {configs.Count} device configuration(s) from cache");
                typesLoaded++;
                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyDeviceConfigs);
            }

            var policies = _cacheService.Get<DeviceCompliancePolicy>(tenantId, CacheKeyCompliancePolicies);
            if (policies != null)
            {
                CompliancePolicies = new ObservableCollection<DeviceCompliancePolicy>(policies);
                DebugLog.Log("Cache", $"Loaded {policies.Count} compliance policy(ies) from cache");
                typesLoaded++;
                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyCompliancePolicies);
            }

            var apps = _cacheService.Get<MobileApp>(tenantId, CacheKeyApplications);
            if (apps != null)
            {
                Applications = new ObservableCollection<MobileApp>(apps);
                DebugLog.Log("Cache", $"Loaded {apps.Count} application(s) from cache");
                typesLoaded++;
                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyApplications);
            }

            var settingsPolicies = _cacheService.Get<DeviceManagementConfigurationPolicy>(tenantId, CacheKeySettingsCatalog);
            if (settingsPolicies != null)
            {
                SettingsCatalogPolicies = new ObservableCollection<DeviceManagementConfigurationPolicy>(settingsPolicies);
                DebugLog.Log("Cache", $"Loaded {settingsPolicies.Count} settings catalog policy(ies) from cache");
                typesLoaded++;
                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeySettingsCatalog);
            }

            var endpointSecurityIntents = _cacheService.Get<DeviceManagementIntent>(tenantId, CacheKeyEndpointSecurity);
            if (endpointSecurityIntents != null)
            {
                EndpointSecurityIntents = new ObservableCollection<DeviceManagementIntent>(endpointSecurityIntents);
                _endpointSecurityLoaded = true;
                DebugLog.Log("Cache", $"Loaded {endpointSecurityIntents.Count} endpoint security intent(s) from cache");
                typesLoaded++;
                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyEndpointSecurity);
            }

            var administrativeTemplates = _cacheService.Get<GroupPolicyConfiguration>(tenantId, CacheKeyAdministrativeTemplates);
            if (administrativeTemplates != null)
            {
                AdministrativeTemplates = new ObservableCollection<GroupPolicyConfiguration>(administrativeTemplates);
                _administrativeTemplatesLoaded = true;
                DebugLog.Log("Cache", $"Loaded {administrativeTemplates.Count} administrative template(s) from cache");
                typesLoaded++;
                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyAdministrativeTemplates);
            }

            var enrollmentConfigurations = _cacheService.Get<DeviceEnrollmentConfiguration>(tenantId, CacheKeyEnrollmentConfigurations);
            if (enrollmentConfigurations != null)
            {
                EnrollmentConfigurations = new ObservableCollection<DeviceEnrollmentConfiguration>(enrollmentConfigurations);
                _enrollmentConfigurationsLoaded = true;
                DebugLog.Log("Cache", $"Loaded {enrollmentConfigurations.Count} enrollment configuration(s) from cache");
                typesLoaded++;
                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyEnrollmentConfigurations);
            }

            var appProtectionPolicies = _cacheService.Get<ManagedAppPolicy>(tenantId, CacheKeyAppProtectionPolicies);
            if (appProtectionPolicies != null)
            {
                AppProtectionPolicies = new ObservableCollection<ManagedAppPolicy>(appProtectionPolicies);
                _appProtectionPoliciesLoaded = true;
                DebugLog.Log("Cache", $"Loaded {appProtectionPolicies.Count} app protection policy(ies) from cache");
                typesLoaded++;
                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyAppProtectionPolicies);
            }

            var managedDeviceAppConfigurations = _cacheService.Get<ManagedDeviceMobileAppConfiguration>(tenantId, CacheKeyManagedDeviceAppConfigurations);
            if (managedDeviceAppConfigurations != null)
            {
                ManagedDeviceAppConfigurations = new ObservableCollection<ManagedDeviceMobileAppConfiguration>(managedDeviceAppConfigurations);
                _managedDeviceAppConfigurationsLoaded = true;
                DebugLog.Log("Cache", $"Loaded {managedDeviceAppConfigurations.Count} managed device app configuration(s) from cache");
                typesLoaded++;
                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyManagedDeviceAppConfigurations);
            }

            var targetedManagedAppConfigurations = _cacheService.Get<TargetedManagedAppConfiguration>(tenantId, CacheKeyTargetedManagedAppConfigurations);
            if (targetedManagedAppConfigurations != null)
            {
                TargetedManagedAppConfigurations = new ObservableCollection<TargetedManagedAppConfiguration>(targetedManagedAppConfigurations);
                _targetedManagedAppConfigurationsLoaded = true;
                DebugLog.Log("Cache", $"Loaded {targetedManagedAppConfigurations.Count} targeted managed app configuration(s) from cache");
                typesLoaded++;
                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyTargetedManagedAppConfigurations);
            }

            var termsCollection = _cacheService.Get<TermsAndConditions>(tenantId, CacheKeyTermsAndConditions);
            if (termsCollection != null)
            {
                TermsAndConditionsCollection = new ObservableCollection<TermsAndConditions>(termsCollection);
                _termsAndConditionsLoaded = true;
                DebugLog.Log("Cache", $"Loaded {termsCollection.Count} terms and conditions item(s) from cache");
                typesLoaded++;
                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyTermsAndConditions);
            }

            var scopeTags = _cacheService.Get<RoleScopeTag>(tenantId, CacheKeyScopeTags);
            if (scopeTags != null)
            {
                ScopeTags = new ObservableCollection<RoleScopeTag>(scopeTags);
                _scopeTagsLoaded = true;
                DebugLog.Log("Cache", $"Loaded {scopeTags.Count} scope tag(s) from cache");
                typesLoaded++;
                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyScopeTags);
            }

            var roleDefinitions = _cacheService.Get<RoleDefinition>(tenantId, CacheKeyRoleDefinitions);
            if (roleDefinitions != null)
            {
                RoleDefinitions = new ObservableCollection<RoleDefinition>(roleDefinitions);
                _roleDefinitionsLoaded = true;
                DebugLog.Log("Cache", $"Loaded {roleDefinitions.Count} role definition(s) from cache");
                typesLoaded++;
                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyRoleDefinitions);
            }

            var intuneBrandingProfiles = _cacheService.Get<IntuneBrandingProfile>(tenantId, CacheKeyIntuneBrandingProfiles);
            if (intuneBrandingProfiles != null)
            {
                IntuneBrandingProfiles = new ObservableCollection<IntuneBrandingProfile>(intuneBrandingProfiles);
                _intuneBrandingProfilesLoaded = true;
                DebugLog.Log("Cache", $"Loaded {intuneBrandingProfiles.Count} Intune branding profile(s) from cache");
                typesLoaded++;
                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyIntuneBrandingProfiles);
            }

            var azureBrandingLocalizations = _cacheService.Get<OrganizationalBrandingLocalization>(tenantId, CacheKeyAzureBrandingLocalizations);
            if (azureBrandingLocalizations != null)
            {
                AzureBrandingLocalizations = new ObservableCollection<OrganizationalBrandingLocalization>(azureBrandingLocalizations);
                _azureBrandingLocalizationsLoaded = true;
                DebugLog.Log("Cache", $"Loaded {azureBrandingLocalizations.Count} Azure branding localization(s) from cache");
                typesLoaded++;
                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyAzureBrandingLocalizations);
            }

            var conditionalAccessPolicies = _cacheService.Get<ConditionalAccessPolicy>(tenantId, CacheKeyConditionalAccess);
            if (conditionalAccessPolicies != null)
            {
                ConditionalAccessPolicies = new ObservableCollection<ConditionalAccessPolicy>(conditionalAccessPolicies);
                _conditionalAccessLoaded = true;
                DebugLog.Log("Cache", $"Loaded {conditionalAccessPolicies.Count} conditional access policy(ies) from cache");
                typesLoaded++;
                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyConditionalAccess);
            }

            var assignmentFilters = _cacheService.Get<DeviceAndAppManagementAssignmentFilter>(tenantId, CacheKeyAssignmentFilters);
            if (assignmentFilters != null)
            {
                AssignmentFilters = new ObservableCollection<DeviceAndAppManagementAssignmentFilter>(assignmentFilters);
                _assignmentFiltersLoaded = true;
                DebugLog.Log("Cache", $"Loaded {assignmentFilters.Count} assignment filter(s) from cache");
                typesLoaded++;
                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyAssignmentFilters);
            }

            var policySets = _cacheService.Get<PolicySet>(tenantId, CacheKeyPolicySets);
            if (policySets != null)
            {
                PolicySets = new ObservableCollection<PolicySet>(policySets);
                _policySetsLoaded = true;
                DebugLog.Log("Cache", $"Loaded {policySets.Count} policy set(s) from cache");
                typesLoaded++;
                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyPolicySets);
            }

            if (typesLoaded > 0)
            {
                var totalItems = DeviceConfigurations.Count + CompliancePolicies.Count + Applications.Count + SettingsCatalogPolicies.Count + EndpointSecurityIntents.Count + AdministrativeTemplates.Count + EnrollmentConfigurations.Count + AppProtectionPolicies.Count + ManagedDeviceAppConfigurations.Count + TargetedManagedAppConfigurations.Count + TermsAndConditionsCollection.Count + ScopeTags.Count + RoleDefinitions.Count + IntuneBrandingProfiles.Count + AzureBrandingLocalizations.Count + ConditionalAccessPolicies.Count + AssignmentFilters.Count + PolicySets.Count;
                var ageText = FormatCacheAge(oldestCacheTime);
                CacheStatusText = oldestCacheTime.HasValue
                    ? $"Cache: {oldestCacheTime.Value.ToLocalTime():MMM dd, h:mm tt}"
                    : "";
                StatusText = $"Loaded {totalItems} item(s) from cache ({ageText})";
                ApplyFilter();
            }
            else
            {
                DebugLog.Log("Cache", "No cached data found");
            }

            // If all primary overview types loaded, also populate Overview dashboard from cache
            if (typesLoaded >= 4)
            {
                var cachedAssignments = _cacheService.Get<AppAssignmentRow>(tenantId, CacheKeyAppAssignments);
                if (cachedAssignments != null && cachedAssignments.Count > 0)
                {
                    AppAssignmentRows = new ObservableCollection<AppAssignmentRow>(cachedAssignments);
                    _appAssignmentsLoaded = true;
                    DebugLog.Log("Cache", $"Loaded {cachedAssignments.Count} app assignment row(s) from cache for dashboard");
                }

                Overview.Update(
                    ActiveProfile,
                    (IReadOnlyList<DeviceConfiguration>)DeviceConfigurations,
                    (IReadOnlyList<DeviceCompliancePolicy>)CompliancePolicies,
                    (IReadOnlyList<MobileApp>)Applications,
                    (IReadOnlyList<AppAssignmentRow>)AppAssignmentRows);
                DebugLog.Log("Cache", "Updated Overview dashboard from cache");
            }
        }
        catch (Exception ex)
        {
            DebugLog.LogError($"Failed to load from cache: {ex.Message}", ex);
        }

        return typesLoaded;
    }

    private void UpdateOldestCacheTime(ref DateTime? oldest, string tenantId, string dataType)
    {
        var meta = _cacheService.GetMetadata(tenantId, dataType);
        if (meta != null)
        {
            if (oldest == null || meta.Value.CachedAt < oldest.Value)
                oldest = meta.Value.CachedAt;
        }
    }

    private static string FormatCacheAge(DateTime? cachedAtUtc)
    {
        if (cachedAtUtc == null) return "unknown age";
        var age = DateTime.UtcNow - cachedAtUtc.Value;
        if (age.TotalMinutes < 1) return "just now";
        if (age.TotalMinutes < 60) return $"{(int)age.TotalMinutes}m ago";
        if (age.TotalHours < 24) return $"{(int)age.TotalHours}h {age.Minutes}m ago";
        return $"{(int)age.TotalDays}d ago";
    }

    /// <summary>
    /// Tries to load a lazy-loaded view (app assignments, groups) from cache.
    /// Invokes the onLoaded callback with the data if found. Returns true if cache hit.
    /// </summary>
    private bool TryLoadLazyCacheEntry<T>(string cacheKey, Action<List<T>> onLoaded)
    {
        var tenantId = ActiveProfile?.TenantId;
        if (string.IsNullOrEmpty(tenantId)) return false;

        try
        {
            var cached = _cacheService.Get<T>(tenantId, cacheKey);
            if (cached != null && cached.Count > 0)
            {
                DebugLog.Log("Cache", $"Loaded {cached.Count} {cacheKey} row(s) from cache");
                onLoaded(cached);
                return true;
            }
        }
        catch (Exception ex)
        {
            DebugLog.LogError($"Failed to load {cacheKey} from cache: {ex.Message}", ex);
        }

        return false;
    }

    /// <summary>
    /// Saves all current collections to the cache.
    /// </summary>
    private void SaveToCache(string tenantId)
    {
        if (string.IsNullOrEmpty(tenantId)) return;

        CacheStatusText = $"Cache: {DateTime.Now:MMM dd, h:mm tt}";

        try
        {
            if (DeviceConfigurations.Count > 0)
                _cacheService.Set(tenantId, CacheKeyDeviceConfigs, DeviceConfigurations.ToList());

            if (CompliancePolicies.Count > 0)
                _cacheService.Set(tenantId, CacheKeyCompliancePolicies, CompliancePolicies.ToList());

            if (Applications.Count > 0)
                _cacheService.Set(tenantId, CacheKeyApplications, Applications.ToList());

            if (SettingsCatalogPolicies.Count > 0)
                _cacheService.Set(tenantId, CacheKeySettingsCatalog, SettingsCatalogPolicies.ToList());

            if (EndpointSecurityIntents.Count > 0)
                _cacheService.Set(tenantId, CacheKeyEndpointSecurity, EndpointSecurityIntents.ToList());

            if (AdministrativeTemplates.Count > 0)
                _cacheService.Set(tenantId, CacheKeyAdministrativeTemplates, AdministrativeTemplates.ToList());

            if (EnrollmentConfigurations.Count > 0)
                _cacheService.Set(tenantId, CacheKeyEnrollmentConfigurations, EnrollmentConfigurations.ToList());

            if (AppProtectionPolicies.Count > 0)
                _cacheService.Set(tenantId, CacheKeyAppProtectionPolicies, AppProtectionPolicies.ToList());

            if (ManagedDeviceAppConfigurations.Count > 0)
                _cacheService.Set(tenantId, CacheKeyManagedDeviceAppConfigurations, ManagedDeviceAppConfigurations.ToList());

            if (TargetedManagedAppConfigurations.Count > 0)
                _cacheService.Set(tenantId, CacheKeyTargetedManagedAppConfigurations, TargetedManagedAppConfigurations.ToList());

            if (TermsAndConditionsCollection.Count > 0)
                _cacheService.Set(tenantId, CacheKeyTermsAndConditions, TermsAndConditionsCollection.ToList());

            if (ScopeTags.Count > 0)
                _cacheService.Set(tenantId, CacheKeyScopeTags, ScopeTags.ToList());

            if (RoleDefinitions.Count > 0)
                _cacheService.Set(tenantId, CacheKeyRoleDefinitions, RoleDefinitions.ToList());

            if (IntuneBrandingProfiles.Count > 0)
                _cacheService.Set(tenantId, CacheKeyIntuneBrandingProfiles, IntuneBrandingProfiles.ToList());

            if (AzureBrandingLocalizations.Count > 0)
                _cacheService.Set(tenantId, CacheKeyAzureBrandingLocalizations, AzureBrandingLocalizations.ToList());

            if (ConditionalAccessPolicies.Count > 0)
                _cacheService.Set(tenantId, CacheKeyConditionalAccess, ConditionalAccessPolicies.ToList());

            if (AssignmentFilters.Count > 0)
                _cacheService.Set(tenantId, CacheKeyAssignmentFilters, AssignmentFilters.ToList());

            if (PolicySets.Count > 0)
                _cacheService.Set(tenantId, CacheKeyPolicySets, PolicySets.ToList());

            DebugLog.Log("Cache", "Saved data to disk cache");
        }
        catch (Exception ex)
        {
            DebugLog.LogError($"Failed to save to cache: {ex.Message}", ex);
        }
    }

    // --- Disconnect ---

    [RelayCommand]
    private void Disconnect()
    {
        DisconnectInternal();
        CurrentView = LoginViewModel;
        LoginViewModel.PopulateSavedProfiles();
        LoginViewModel.SelectActiveProfile();
    }

    private void DisconnectInternal()
    {
        IsConnected = false;
        ActiveProfile = null;
        WindowTitle = "Intune Commander";
        StatusText = "Not connected";
        SelectedCategory = null;
        DeviceConfigurations.Clear();
        SelectedConfiguration = null;
        CompliancePolicies.Clear();
        SelectedCompliancePolicy = null;
        Applications.Clear();
        SelectedApplication = null;
        AppAssignmentRows.Clear();
        SelectedAppAssignmentRow = null;
        _appAssignmentsLoaded = false;
        _conditionalAccessLoaded = false;
        _endpointSecurityLoaded = false;
        _administrativeTemplatesLoaded = false;
        _enrollmentConfigurationsLoaded = false;
        _appProtectionPoliciesLoaded = false;
        _managedDeviceAppConfigurationsLoaded = false;
        _targetedManagedAppConfigurationsLoaded = false;
        _termsAndConditionsLoaded = false;
        _scopeTagsLoaded = false;
        _roleDefinitionsLoaded = false;
        _intuneBrandingProfilesLoaded = false;
        _azureBrandingLocalizationsLoaded = false;
        _assignmentFiltersLoaded = false;
        _policySetsLoaded = false;
        SettingsCatalogPolicies.Clear();
        SelectedSettingsCatalogPolicy = null;
        EndpointSecurityIntents.Clear();
        SelectedEndpointSecurityIntent = null;
        AdministrativeTemplates.Clear();
        SelectedAdministrativeTemplate = null;
        EnrollmentConfigurations.Clear();
        SelectedEnrollmentConfiguration = null;
        AppProtectionPolicies.Clear();
        SelectedAppProtectionPolicy = null;
        ManagedDeviceAppConfigurations.Clear();
        SelectedManagedDeviceAppConfiguration = null;
        TargetedManagedAppConfigurations.Clear();
        SelectedTargetedManagedAppConfiguration = null;
        TermsAndConditionsCollection.Clear();
        SelectedTermsAndConditions = null;
        ScopeTags.Clear();
        SelectedScopeTag = null;
        RoleDefinitions.Clear();
        SelectedRoleDefinition = null;
        IntuneBrandingProfiles.Clear();
        SelectedIntuneBrandingProfile = null;
        AzureBrandingLocalizations.Clear();
        SelectedAzureBrandingLocalization = null;
        ConditionalAccessPolicies.Clear();
        SelectedConditionalAccessPolicy = null;
        AssignmentFilters.Clear();
        SelectedAssignmentFilter = null;
        PolicySets.Clear();
        SelectedPolicySet = null;
        DynamicGroupRows.Clear();
        SelectedDynamicGroupRow = null;
        _dynamicGroupsLoaded = false;
        AssignedGroupRows.Clear();
        SelectedAssignedGroupRow = null;
        _assignedGroupsLoaded = false;
        SelectedItemAssignments.Clear();
        SelectedItemTypeName = "";
        _graphClient = null;
        _configProfileService = null;
        _compliancePolicyService = null;
        _applicationService = null;
        _groupService = null;
        _settingsCatalogService = null;
        _conditionalAccessPolicyService = null;
        _assignmentFilterService = null;
        _policySetService = null;
        _endpointSecurityService = null;
        _administrativeTemplateService = null;
        _enrollmentConfigurationService = null;
        _appProtectionPolicyService = null;
        _managedAppConfigurationService = null;
        _termsAndConditionsService = null;
        _scopeTagService = null;
        _roleDefinitionService = null;
        _intuneBrandingService = null;
        _azureBrandingService = null;
        _importService = null;
        _groupNameCache.Clear();
        CacheStatusText = "";
    }
}

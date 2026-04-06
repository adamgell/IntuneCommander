using System.Text.Json;
using Intune.Commander.Core.Services;
using Intune.Commander.DesktopReact.Models;

namespace Intune.Commander.DesktopReact.Services;

public class TenantAdminBridgeService
{
    private readonly AuthBridgeService _authBridge;

    private IScopeTagService? _scopeTagService;
    private IRoleDefinitionService? _roleDefinitionService;
    private IIntuneBrandingService? _intuneBrandingService;
    private IAzureBrandingService? _azureBrandingService;
    private ITermsAndConditionsService? _termsAndConditionsService;
    private ITermsOfUseService? _termsOfUseService;
    private IAdmxFileService? _admxFileService;
    private IReusablePolicySettingService? _reusableSettingService;
    private INotificationTemplateService? _notificationService;
    private IPolicySetService? _policySetService;

    public TenantAdminBridgeService(AuthBridgeService authBridge)
    {
        _authBridge = authBridge;
    }

    private Microsoft.Graph.Beta.GraphServiceClient GetClient() =>
        _authBridge.GraphClient ?? throw new InvalidOperationException("Not connected — authenticate first");

    private IScopeTagService GetScopeTagService() => _scopeTagService ??= new ScopeTagService(GetClient());
    private IRoleDefinitionService GetRoleDefinitionService() => _roleDefinitionService ??= new RoleDefinitionService(GetClient());
    private IIntuneBrandingService GetIntuneBrandingService() => _intuneBrandingService ??= new IntuneBrandingService(GetClient());
    private IAzureBrandingService GetAzureBrandingService() => _azureBrandingService ??= new AzureBrandingService(GetClient());
    private ITermsAndConditionsService GetTermsAndConditionsService() => _termsAndConditionsService ??= new TermsAndConditionsService(GetClient());
    private ITermsOfUseService GetTermsOfUseService() => _termsOfUseService ??= new TermsOfUseService(GetClient());
    private IAdmxFileService GetAdmxFileService() => _admxFileService ??= new AdmxFileService(GetClient());
    private IReusablePolicySettingService GetReusableSettingService() => _reusableSettingService ??= new ReusablePolicySettingService(GetClient());
    private INotificationTemplateService GetNotificationService() => _notificationService ??= new NotificationTemplateService(GetClient());
    private IPolicySetService GetPolicySetService() => _policySetService ??= new PolicySetService(GetClient());

    public void Reset()
    {
        _scopeTagService = null;
        _roleDefinitionService = null;
        _intuneBrandingService = null;
        _azureBrandingService = null;
        _termsAndConditionsService = null;
        _termsOfUseService = null;
        _admxFileService = null;
        _reusableSettingService = null;
        _notificationService = null;
        _policySetService = null;
    }

    // ── Scope Tags ─────────────────────────────────────────────────────

    public async Task<object> ListScopeTagsAsync()
    {
        var items = await GetScopeTagService().ListScopeTagsAsync();
        return items.Select(t => new TenantAdminListItem(
            t.Id ?? "", t.DisplayName ?? "", t.Description, "scopeTags", null)).ToArray();
    }

    public async Task<object> GetScopeTagDetailAsync(JsonElement? payload)
    {
        var id = GetRequiredId(payload);
        var tag = await GetScopeTagService().GetScopeTagAsync(id);
        if (tag is null) throw new KeyNotFoundException($"Scope tag {id} not found");
        return new ScopeTagDetail(tag.Id ?? "", tag.DisplayName ?? "", tag.Description, tag.Id == "0");
    }

    // ── Role Definitions ───────────────────────────────────────────────

    public async Task<object> ListRolesAsync()
    {
        var items = await GetRoleDefinitionService().ListRoleDefinitionsAsync();
        return items.Select(r => new TenantAdminListItem(
            r.Id ?? "", r.DisplayName ?? "", r.Description, "roles", null)).ToArray();
    }

    public async Task<object> GetRoleDetailAsync(JsonElement? payload)
    {
        var id = GetRequiredId(payload);
        var role = await GetRoleDefinitionService().GetRoleDefinitionAsync(id);
        if (role is null) throw new KeyNotFoundException($"Role definition {id} not found");

        var permissions = role.RolePermissions?
            .SelectMany(rp => rp.ResourceActions ?? [])
            .SelectMany(ra => ra.AllowedResourceActions ?? [])
            .Distinct().ToArray() ?? [];

        var assignments = role.RoleAssignments?.Select(a => new RoleAssignmentDto(
            a.Id ?? "", a.DisplayName ?? "",
            a.ScopeMembers?.ToArray() ?? [])).ToArray() ?? [];

        return new RoleDefinitionDetail(
            role.Id ?? "", role.DisplayName ?? "", role.Description,
            role.IsBuiltIn ?? false, permissions, assignments);
    }

    // ── Intune Branding ────────────────────────────────────────────────

    public async Task<object> ListIntuneBrandingAsync()
    {
        var items = await GetIntuneBrandingService().ListIntuneBrandingProfilesAsync();
        return items.Select(b => new TenantAdminListItem(
            b.Id ?? "", b.ProfileName ?? b.Id ?? "", b.ProfileDescription, "intuneBranding", null)).ToArray();
    }

    public async Task<object> GetIntuneBrandingDetailAsync(JsonElement? payload)
    {
        var id = GetRequiredId(payload);
        var profile = await GetIntuneBrandingService().GetIntuneBrandingProfileAsync(id);
        if (profile is null) throw new KeyNotFoundException($"Intune branding profile {id} not found");
        return new IntuneBrandingDetail(
            profile.Id ?? "", profile.ProfileName ?? "",
            profile.CompanyPortalBlockedActions?.Count > 0 ? $"{profile.CompanyPortalBlockedActions.Count} blocked actions" : null,
            profile.IsDefaultProfile ?? false,
            profile.ContactITName, profile.ContactITPhoneNumber,
            profile.ContactITEmailAddress, profile.OnlineSupportSiteName,
            profile.OnlineSupportSiteUrl, profile.PrivacyUrl);
    }

    // ── Azure Branding ─────────────────────────────────────────────────

    public async Task<object> ListAzureBrandingAsync()
    {
        var items = await GetAzureBrandingService().ListBrandingLocalizationsAsync();
        return items.Select(b => new TenantAdminListItem(
            b.Id ?? "", b.Id ?? "(default)", null, "azureBranding", null)).ToArray();
    }

    public async Task<object> GetAzureBrandingDetailAsync(JsonElement? payload)
    {
        var id = GetRequiredId(payload);
        var loc = await GetAzureBrandingService().GetBrandingLocalizationAsync(id);
        if (loc is null) throw new KeyNotFoundException($"Azure branding localization {id} not found");
        return new AzureBrandingDetail(loc.Id ?? "", loc.SignInPageText, loc.UsernameHintText,
            loc.LoginPageTextVisibilitySettings?.HideCannotAccessYourAccount?.ToString());
    }

    // ── Terms & Conditions ─────────────────────────────────────────────

    public async Task<object> ListTermsAndConditionsAsync()
    {
        var items = await GetTermsAndConditionsService().ListTermsAndConditionsAsync();
        return items.Select(t => new TenantAdminListItem(
            t.Id ?? "", t.DisplayName ?? "", t.Description, "termsAndConditions",
            t.LastModifiedDateTime?.ToString("o"))).ToArray();
    }

    public async Task<object> GetTermsAndConditionsDetailAsync(JsonElement? payload)
    {
        var id = GetRequiredId(payload);
        var tc = await GetTermsAndConditionsService().GetTermsAndConditionsAsync(id);
        if (tc is null) throw new KeyNotFoundException($"Terms and conditions {id} not found");
        return new TermsAndConditionsDetail(
            tc.Id ?? "", tc.DisplayName ?? "", tc.Description, tc.Title, tc.BodyText,
            tc.AcceptanceStatement, tc.Version ?? 0,
            tc.CreatedDateTime?.ToString("o"), tc.LastModifiedDateTime?.ToString("o"));
    }

    // ── Terms of Use ───────────────────────────────────────────────────

    public async Task<object> ListTermsOfUseAsync()
    {
        var items = await GetTermsOfUseService().ListTermsOfUseAgreementsAsync();
        return items.Select(t => new TenantAdminListItem(
            t.Id ?? "", t.DisplayName ?? "", null, "termsOfUse", null)).ToArray();
    }

    public async Task<object> GetTermsOfUseDetailAsync(JsonElement? payload)
    {
        var id = GetRequiredId(payload);
        var agreement = await GetTermsOfUseService().GetTermsOfUseAgreementAsync(id);
        if (agreement is null) throw new KeyNotFoundException($"Terms of use agreement {id} not found");
        return new TermsOfUseDetail(
            agreement.Id ?? "", agreement.DisplayName ?? "",
            agreement.IsPerDeviceAcceptanceRequired ?? false,
            agreement.IsViewingBeforeAcceptanceRequired ?? false,
            agreement.UserReacceptRequiredFrequency?.ToString());
    }

    // ── ADMX Files ─────────────────────────────────────────────────────

    public async Task<object> ListAdmxFilesAsync()
    {
        var items = await GetAdmxFileService().ListAdmxFilesAsync();
        return items.Select(f => new TenantAdminListItem(
            f.Id ?? "", f.FileName ?? f.Id ?? "", f.Description, "admxFiles",
            f.LastModifiedDateTime?.ToString("o"))).ToArray();
    }

    public async Task<object> GetAdmxFileDetailAsync(JsonElement? payload)
    {
        var id = GetRequiredId(payload);
        var file = await GetAdmxFileService().GetAdmxFileAsync(id);
        if (file is null) throw new KeyNotFoundException($"ADMX file {id} not found");
        return new
        {
            id = file.Id ?? "",
            fileName = file.FileName ?? "",
            description = file.Description,
            languageCodes = file.LanguageCodes?.ToArray() ?? [],
            status = file.Status?.ToString(),
            lastModifiedDateTime = file.LastModifiedDateTime?.ToString("o"),
        };
    }

    // ── Reusable Policy Settings ───────────────────────────────────────

    public async Task<object> ListReusableSettingsAsync()
    {
        var items = await GetReusableSettingService().ListReusablePolicySettingsAsync();
        return items.Select(s => new TenantAdminListItem(
            s.Id ?? "", s.DisplayName ?? "", s.Description, "reusableSettings",
            s.LastModifiedDateTime?.ToString("o"))).ToArray();
    }

    public async Task<object> GetReusableSettingDetailAsync(JsonElement? payload)
    {
        var id = GetRequiredId(payload);
        var setting = await GetReusableSettingService().GetReusablePolicySettingAsync(id);
        if (setting is null) throw new KeyNotFoundException($"Reusable policy setting {id} not found");
        return new ReusablePolicySettingDetail(
            setting.Id ?? "", setting.DisplayName ?? "", setting.Description,
            setting.SettingDefinitionId, setting.SettingInstance?.OdataType,
            setting.CreatedDateTime?.ToString("o"), setting.LastModifiedDateTime?.ToString("o"));
    }

    // ── Notification Templates ─────────────────────────────────────────

    public async Task<object> ListNotificationsAsync()
    {
        var items = await GetNotificationService().ListNotificationTemplatesAsync();
        return items.Select(n => new TenantAdminListItem(
            n.Id ?? "", n.DisplayName ?? "", null, "notifications",
            n.LastModifiedDateTime?.ToString("o"))).ToArray();
    }

    public async Task<object> GetNotificationDetailAsync(JsonElement? payload)
    {
        var id = GetRequiredId(payload);
        var template = await GetNotificationService().GetNotificationTemplateAsync(id);
        if (template is null) throw new KeyNotFoundException($"Notification template {id} not found");
        return new NotificationTemplateDetail(
            template.Id ?? "", template.DisplayName ?? "", template.DefaultLocale,
            template.BrandingOptions?.ToString(), template.LastModifiedDateTime?.ToString("o"));
    }

    // ── Policy Sets ────────────────────────────────────────────────────

    public async Task<object> ListPolicySetsAsync()
    {
        var items = await GetPolicySetService().ListPolicySetsAsync();
        return items.Select(p => new TenantAdminListItem(
            p.Id ?? "", p.DisplayName ?? "", p.Description, "policySets",
            p.LastModifiedDateTime?.ToString("o"))).ToArray();
    }

    public async Task<object> GetPolicySetDetailAsync(JsonElement? payload)
    {
        var id = GetRequiredId(payload);
        var policySet = await GetPolicySetService().GetPolicySetAsync(id);
        if (policySet is null) throw new KeyNotFoundException($"Policy set {id} not found");
        return new PolicySetDetail(
            policySet.Id ?? "", policySet.DisplayName ?? "", policySet.Description,
            policySet.Status?.ToString(),
            policySet.CreatedDateTime?.ToString("o"), policySet.LastModifiedDateTime?.ToString("o"));
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private static string GetRequiredId(JsonElement? payload)
    {
        if (payload is null || !payload.Value.TryGetProperty("id", out var idProp))
            throw new ArgumentException("id is required");
        return idProp.GetString() ?? throw new ArgumentException("id is required");
    }
}

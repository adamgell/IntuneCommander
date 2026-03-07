using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Intune.Commander.Core.Models;
using Intune.Commander.Core.Services;
using Microsoft.Graph.Beta.Models;
using Microsoft.Kiota.Serialization.Json;

namespace Intune.Commander.Desktop.ViewModels;

public partial class BaselineViewModel : ViewModelBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IBaselineService _baselineService;
    private readonly ISettingsCatalogService _settingsCatalogService;
    private readonly IEndpointSecurityService _endpointSecurityService;
    private readonly ICompliancePolicyService _compliancePolicyService;
    private readonly IGroupService _groupService;

    [ObservableProperty]
    private BaselinePolicyType _activeBaselineType = BaselinePolicyType.SettingsCatalog;

    public ObservableCollection<BaselinePolicy> Baselines { get; } = [];

    [ObservableProperty]
    private BaselinePolicy? _selectedBaseline;

    [ObservableProperty]
    private string _categoryFilter = "All";

    public ObservableCollection<string> Categories { get; } = [];

    [ObservableProperty]
    private BaselineComparisonResult? _comparisonResult;

    [ObservableProperty]
    private DeviceManagementConfigurationPolicy? _selectedComparisonPolicy;

    // TODO: ES/Compliance inline editing
    // TODO: Deploy to Existing for ES/Compliance types
    public bool IsCompareAvailable => ActiveBaselineType == BaselinePolicyType.SettingsCatalog;

    public bool IsSettingsCatalogSelected => ActiveBaselineType == BaselinePolicyType.SettingsCatalog;
    public bool IsEndpointSecuritySelected => ActiveBaselineType == BaselinePolicyType.EndpointSecurity;
    public bool IsComplianceSelected => ActiveBaselineType == BaselinePolicyType.Compliance;

    public BaselineViewModel(
        IBaselineService baselineService,
        ISettingsCatalogService settingsCatalogService,
        IEndpointSecurityService endpointSecurityService,
        ICompliancePolicyService compliancePolicyService,
        IGroupService groupService)
    {
        _baselineService = baselineService;
        _settingsCatalogService = settingsCatalogService;
        _endpointSecurityService = endpointSecurityService;
        _compliancePolicyService = compliancePolicyService;
        _groupService = groupService;

        LoadBaselines();
    }

    partial void OnActiveBaselineTypeChanged(BaselinePolicyType value)
    {
        LoadBaselines();
        ComparisonResult = null;
        OnPropertyChanged(nameof(IsCompareAvailable));
        OnPropertyChanged(nameof(IsSettingsCatalogSelected));
        OnPropertyChanged(nameof(IsEndpointSecuritySelected));
        OnPropertyChanged(nameof(IsComplianceSelected));
    }

    [RelayCommand]
    private void SetBaselineType(BaselinePolicyType type) => ActiveBaselineType = type;

    partial void OnCategoryFilterChanged(string value)
    {
        RefreshFilteredBaselines();
    }

    [RelayCommand]
    private async Task DeployAsNewAsync(CancellationToken ct)
    {
        if (SelectedBaseline is null) return;

        IsBusy = true;
        ClearError();

        try
        {
            switch (SelectedBaseline.PolicyType)
            {
                case BaselinePolicyType.SettingsCatalog:
                    await DeploySettingsCatalogAsync(SelectedBaseline, ct);
                    break;
                case BaselinePolicyType.EndpointSecurity:
                    await DeployEndpointSecurityAsync(SelectedBaseline, ct);
                    break;
                case BaselinePolicyType.Compliance:
                    await DeployComplianceAsync(SelectedBaseline, ct);
                    break;
            }

            DebugLog.Log("Baseline", $"Deployed baseline '{SelectedBaseline.Name}' as new policy");
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            SetError($"Deploy failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CompareWithTenantPolicyAsync(CancellationToken ct)
    {
        var tenantPolicyId = SelectedComparisonPolicy?.Id;
        if (SelectedBaseline is null || string.IsNullOrEmpty(tenantPolicyId)) return;
        if (ActiveBaselineType != BaselinePolicyType.SettingsCatalog) return;

        IsBusy = true;
        ClearError();
        ComparisonResult = null;

        try
        {
            var tenantSettings = await _settingsCatalogService.GetPolicySettingsAsync(tenantPolicyId, ct);
            var tenantPolicy = await _settingsCatalogService.GetSettingsCatalogPolicyAsync(tenantPolicyId, ct);

            ComparisonResult = _baselineService.CompareSettingsCatalog(
                SelectedBaseline,
                tenantSettings,
                tenantPolicyId,
                tenantPolicy?.Name);

            DebugLog.Log("Baseline",
                $"Compared '{SelectedBaseline.Name}' with '{tenantPolicy?.Name}': " +
                $"{ComparisonResult.Matching.Count} match, {ComparisonResult.Drifted.Count} drift, " +
                $"{ComparisonResult.Missing.Count} missing, {ComparisonResult.Extra.Count} extra");
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            SetError($"Compare failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void LoadBaselines()
    {
        Categories.Clear();
        Categories.Add("All");

        foreach (var cat in _baselineService.GetCategories(ActiveBaselineType))
            Categories.Add(cat);

        CategoryFilter = "All";
        RefreshFilteredBaselines();
    }

    private void RefreshFilteredBaselines()
    {
        Baselines.Clear();
        SelectedBaseline = null;

        var items = CategoryFilter == "All"
            ? _baselineService.GetBaselinesByType(ActiveBaselineType)
            : _baselineService.GetBaselinesByCategory(CategoryFilter, ActiveBaselineType);

        foreach (var b in items)
            Baselines.Add(b);
    }

    private async Task DeploySettingsCatalogAsync(BaselinePolicy baseline, CancellationToken ct)
    {
        DeviceManagementConfigurationPolicy policy;
        List<DeviceManagementConfigurationSetting> settings;

        if (baseline.RawJson.TryGetProperty("policy", out _))
        {
            // Export wrapper format: { "policy": {...}, "settings": [...] }
            var export = JsonSerializer.Deserialize<SettingsCatalogExport>(
                baseline.RawJson.GetRawText(), JsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize Settings Catalog baseline");
            policy = export.Policy;
            settings = export.Settings;
        }
        else
        {
            // OIB raw format: policy fields at top level with embedded settings
            (policy, settings) = await ParseOibSettingsCatalogBaselineAsync(baseline.RawJson);
        }

        policy.Id = null;
        policy.CreatedDateTime = null;
        policy.LastModifiedDateTime = null;

        var created = await _settingsCatalogService.CreateSettingsCatalogPolicyAsync(policy, ct);

        if (created.Id is not null && settings.Count > 0)
        {
            await _settingsCatalogService.UpdatePolicySettingsAsync(
                created.Id, settings, ct);
        }
    }

    private async Task DeployEndpointSecurityAsync(BaselinePolicy baseline, CancellationToken ct)
    {
        if (baseline.RawJson.TryGetProperty("intent", out _))
        {
            // Export wrapper format
            var export = JsonSerializer.Deserialize<EndpointSecurityExport>(
                baseline.RawJson.GetRawText(), JsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize Endpoint Security baseline");
            export.Intent.Id = null;
            await _endpointSecurityService.CreateEndpointSecurityIntentAsync(export.Intent, ct);
        }
        else
        {
            // OIB ES baselines are Settings Catalog policies — deploy via SC API
            await DeploySettingsCatalogAsync(baseline, ct);
        }
    }

    private async Task DeployComplianceAsync(BaselinePolicy baseline, CancellationToken ct)
    {
        DeviceCompliancePolicy policy;

        if (baseline.RawJson.TryGetProperty("policy", out _))
        {
            // Export wrapper format
            var export = JsonSerializer.Deserialize<CompliancePolicyExport>(
                baseline.RawJson.GetRawText(), JsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize Compliance baseline");
            policy = export.Policy;
        }
        else
        {
            // OIB raw format — use Kiota parser for proper polymorphic deserialization
            policy = await ParseOibModelAsync<DeviceCompliancePolicy>(
                baseline.RawJson, DeviceCompliancePolicy.CreateFromDiscriminatorValue);
        }

        policy.Id = null;
        policy.CreatedDateTime = null;
        policy.LastModifiedDateTime = null;
        policy.Version = null;
        await _compliancePolicyService.CreateCompliancePolicyAsync(policy, ct);
    }

    private static async Task<(DeviceManagementConfigurationPolicy Policy, List<DeviceManagementConfigurationSetting> Settings)>
        ParseOibSettingsCatalogBaselineAsync(JsonElement rawJson)
    {
        var policy = await ParseOibModelAsync<DeviceManagementConfigurationPolicy>(
            rawJson, DeviceManagementConfigurationPolicy.CreateFromDiscriminatorValue);

        // Extract settings — they may be populated via Kiota or need separate parsing
        var settings = policy.Settings?.ToList() ?? [];
        if (settings.Count == 0
            && rawJson.TryGetProperty("settings", out var settingsJson)
            && settingsJson.ValueKind == JsonValueKind.Array
            && settingsJson.GetArrayLength() > 0)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(settingsJson.GetRawText()));
            var node = await new JsonParseNodeFactory()
                .GetRootParseNodeAsync("application/json", stream);
            settings = node.GetCollectionOfObjectValues(
                DeviceManagementConfigurationSetting.CreateFromDiscriminatorValue)?.ToList() ?? [];
        }

        policy.Settings = null;
        return (policy, settings);
    }

    private static async Task<T> ParseOibModelAsync<T>(
        JsonElement rawJson, Microsoft.Kiota.Abstractions.Serialization.ParsableFactory<T> factory)
        where T : Microsoft.Kiota.Abstractions.Serialization.IParsable
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawJson.GetRawText()));
        var node = await new JsonParseNodeFactory()
            .GetRootParseNodeAsync("application/json", stream);
        return node.GetObjectValue(factory)
            ?? throw new InvalidOperationException($"Failed to deserialize OIB baseline as {typeof(T).Name}");
    }
}

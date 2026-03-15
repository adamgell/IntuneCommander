using System.IO;
using System.Text.Json;
using System.Windows;
using Intune.Commander.Core.Models;
using Intune.Commander.Core.Services;
using Intune.Commander.DesktopReact.Bridge;
using Microsoft.Win32;

namespace Intune.Commander.DesktopReact.Services;

public class ProfileBridgeService
{
    private readonly ProfileService _profileService;
    private IBridgeService? _bridge;
    private bool _loaded;

    private static JsonSerializerOptions JsonOptions => BridgeRouter.JsonOptions;

    public ProfileBridgeService(ProfileService profileService)
    {
        _profileService = profileService;
    }

    public void SetBridge(IBridgeService bridge) => _bridge = bridge;

    public async Task<object> LoadAsync()
    {
        if (!_loaded)
        {
            await _profileService.LoadAsync();
            _loaded = true;
        }

        return BuildProfilesPayload();
    }

    public async Task<object> SaveAsync(JsonElement? payload)
    {
        if (payload is null)
            throw new ArgumentException("Profile data is required");

        var profile = JsonSerializer.Deserialize<TenantProfile>(payload.Value.GetRawText(), JsonOptions)
            ?? throw new ArgumentException("Invalid profile data");

        // Check if profile already exists (update vs create)
        var existing = _profileService.Profiles.FirstOrDefault(p => p.Id == profile.Id);
        if (existing is not null)
        {
            // Update existing profile
            _profileService.RemoveProfile(existing.Id);
        }

        if (string.IsNullOrWhiteSpace(profile.Name))
            profile.Name = $"Tenant-{profile.TenantId[..Math.Min(8, profile.TenantId.Length)]}";

        _profileService.AddProfile(profile);
        await _profileService.SaveAsync();

        await NotifyProfilesChanged();
        return BuildProfilesPayload();
    }

    public async Task<object> DeleteAsync(JsonElement? payload)
    {
        if (payload is null)
            throw new ArgumentException("Profile ID is required");

        var profileId = payload.Value.GetProperty("profileId").GetString()
            ?? throw new ArgumentException("Profile ID is required");

        _profileService.RemoveProfile(profileId);
        await _profileService.SaveAsync();

        await NotifyProfilesChanged();
        return BuildProfilesPayload();
    }

    public async Task<object> ImportAsync()
    {
        // Must run file dialog on UI thread
        string? filePath = null;
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Import Profiles"
            };

            if (dialog.ShowDialog() == true)
                filePath = dialog.FileName;
        });

        if (filePath is null)
            return new { profiles = _profileService.Profiles, activeProfileId = _profileService.ActiveProfileId, imported = 0 };

        var json = await File.ReadAllTextAsync(filePath);
        var importedProfiles = JsonSerializer.Deserialize<List<TenantProfile>>(json, JsonOptions)
            ?? throw new InvalidOperationException("Could not parse profiles from file");

        var imported = 0;
        foreach (var profile in importedProfiles)
        {
            // Skip duplicates (same tenant + client ID)
            var isDuplicate = _profileService.Profiles.Any(p =>
                p.TenantId == profile.TenantId && p.ClientId == profile.ClientId);

            if (isDuplicate)
                continue;

            // Generate fresh ID
            profile.Id = Guid.NewGuid().ToString();
            _profileService.AddProfile(profile);
            imported++;
        }

        if (imported > 0)
            await _profileService.SaveAsync();

        await NotifyProfilesChanged();
        return new { profiles = _profileService.Profiles, activeProfileId = _profileService.ActiveProfileId, imported };
    }

    private object BuildProfilesPayload() => new
    {
        profiles = _profileService.Profiles,
        activeProfileId = _profileService.ActiveProfileId
    };

    private async Task NotifyProfilesChanged()
    {
        if (_bridge is not null)
            await _bridge.SendEventAsync("profiles.changed", BuildProfilesPayload());
    }
}

using System.Text.Json;
using IntuneManager.Core.Models;

namespace IntuneManager.Core.Services;

public class ProfileService
{
    private readonly string _profilePath;
    private ProfileStore _store;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ProfileService(string? profilePath = null)
    {
        _profilePath = profilePath ?? GetDefaultProfilePath();
        _store = new ProfileStore();
    }

    public IReadOnlyList<TenantProfile> Profiles => _store.Profiles.AsReadOnly();
    public string? ActiveProfileId => _store.ActiveProfileId;

    public TenantProfile? GetActiveProfile()
    {
        return _store.Profiles.FirstOrDefault(p => p.Id == _store.ActiveProfileId);
    }

    public void SetActiveProfile(string profileId)
    {
        var profile = _store.Profiles.FirstOrDefault(p => p.Id == profileId)
            ?? throw new ArgumentException($"Profile '{profileId}' not found");

        profile.LastUsed = DateTime.UtcNow;
        _store.ActiveProfileId = profileId;
    }

    public TenantProfile AddProfile(TenantProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.Name))
            throw new ArgumentException("Profile name is required");
        if (string.IsNullOrWhiteSpace(profile.TenantId))
            throw new ArgumentException("Tenant ID is required");
        if (string.IsNullOrWhiteSpace(profile.ClientId))
            throw new ArgumentException("Client ID is required");

        _store.Profiles.Add(profile);

        if (_store.ActiveProfileId == null)
            _store.ActiveProfileId = profile.Id;

        return profile;
    }

    public void RemoveProfile(string profileId)
    {
        var profile = _store.Profiles.FirstOrDefault(p => p.Id == profileId);
        if (profile != null)
        {
            _store.Profiles.Remove(profile);
            if (_store.ActiveProfileId == profileId)
                _store.ActiveProfileId = _store.Profiles.FirstOrDefault()?.Id;
        }
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_profilePath))
        {
            _store = new ProfileStore();
            return;
        }

        var json = await File.ReadAllTextAsync(_profilePath, cancellationToken);
        _store = JsonSerializer.Deserialize<ProfileStore>(json, JsonOptions) ?? new ProfileStore();
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(_profilePath);
        if (directory != null)
            Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(_store, JsonOptions);
        await File.WriteAllTextAsync(_profilePath, json, cancellationToken);
    }

    private static string GetDefaultProfilePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "IntuneManager", "profiles.json");
    }
}

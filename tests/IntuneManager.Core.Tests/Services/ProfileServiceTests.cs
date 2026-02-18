using IntuneManager.Core.Models;
using IntuneManager.Core.Services;

namespace IntuneManager.Core.Tests.Services;

public class ProfileServiceTests : IDisposable
{
    private readonly string _tempPath;
    private readonly ProfileService _service;

    public ProfileServiceTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), $"intunemanager-test-{Guid.NewGuid()}", "profiles.json");
        _service = new ProfileService(_tempPath);
    }

    public void Dispose()
    {
        var dir = Path.GetDirectoryName(_tempPath);
        if (dir != null && Directory.Exists(dir))
            Directory.Delete(dir, true);
    }

    [Fact]
    public void AddProfile_ValidProfile_AddsSuccessfully()
    {
        var profile = CreateTestProfile("Test");

        var result = _service.AddProfile(profile);

        Assert.Single(_service.Profiles);
        Assert.Equal("Test", result.Name);
    }

    [Fact]
    public void AddProfile_FirstProfile_SetsAsActive()
    {
        var profile = CreateTestProfile("First");

        _service.AddProfile(profile);

        Assert.Equal(profile.Id, _service.ActiveProfileId);
    }

    [Fact]
    public void AddProfile_EmptyName_Throws()
    {
        var profile = CreateTestProfile("");
        profile.Name = "";

        Assert.Throws<ArgumentException>(() => _service.AddProfile(profile));
    }

    [Fact]
    public void AddProfile_EmptyTenantId_Throws()
    {
        var profile = CreateTestProfile("NoTenant");
        profile.TenantId = "";

        Assert.Throws<ArgumentException>(() => _service.AddProfile(profile));
    }

    [Fact]
    public void AddProfile_EmptyClientId_Throws()
    {
        var profile = CreateTestProfile("NoClient");
        profile.ClientId = "";

        Assert.Throws<ArgumentException>(() => _service.AddProfile(profile));
    }

    [Fact]
    public void SetActiveProfile_ValidId_SetsActive()
    {
        var p1 = _service.AddProfile(CreateTestProfile("One"));
        var p2 = _service.AddProfile(CreateTestProfile("Two"));

        _service.SetActiveProfile(p2.Id);

        Assert.Equal(p2.Id, _service.ActiveProfileId);
    }

    [Fact]
    public void SetActiveProfile_InvalidId_Throws()
    {
        Assert.Throws<ArgumentException>(() => _service.SetActiveProfile("nonexistent"));
    }

    [Fact]
    public void GetActiveProfile_NoProfiles_ReturnsNull()
    {
        Assert.Null(_service.GetActiveProfile());
    }

    [Fact]
    public void RemoveProfile_ExistingProfile_Removes()
    {
        var profile = _service.AddProfile(CreateTestProfile("ToRemove"));

        _service.RemoveProfile(profile.Id);

        Assert.Empty(_service.Profiles);
    }

    [Fact]
    public void RemoveProfile_NonExisting_DoesNothing()
    {
        var p1 = _service.AddProfile(CreateTestProfile("One"));

        _service.RemoveProfile("does-not-exist");

        Assert.Single(_service.Profiles);
        Assert.Equal(p1.Id, _service.ActiveProfileId);
    }

    [Fact]
    public void RemoveProfile_ActiveProfile_SetsNewActive()
    {
        var p1 = _service.AddProfile(CreateTestProfile("One"));
        var p2 = _service.AddProfile(CreateTestProfile("Two"));
        _service.SetActiveProfile(p1.Id);

        _service.RemoveProfile(p1.Id);

        Assert.Equal(p2.Id, _service.ActiveProfileId);
    }

    [Fact]
    public async Task SaveAndLoad_RoundTrips()
    {
        var profile = _service.AddProfile(CreateTestProfile("Persisted"));
        await _service.SaveAsync();

        var loaded = new ProfileService(_tempPath);
        await loaded.LoadAsync();

        Assert.Single(loaded.Profiles);
        Assert.Equal("Persisted", loaded.Profiles[0].Name);
        Assert.Equal(profile.Id, loaded.ActiveProfileId);
    }

    [Fact]
    public async Task Load_NoFile_ReturnsEmpty()
    {
        await _service.LoadAsync();
        Assert.Empty(_service.Profiles);
    }

    [Fact]
    public void Constructor_DefaultPath_HasNoProfiles()
    {
        var service = new ProfileService();

        Assert.Empty(service.Profiles);
    }

    private static TenantProfile CreateTestProfile(string name) => new()
    {
        Name = name,
        TenantId = Guid.NewGuid().ToString(),
        ClientId = Guid.NewGuid().ToString(),
        Cloud = CloudEnvironment.Commercial
    };
}

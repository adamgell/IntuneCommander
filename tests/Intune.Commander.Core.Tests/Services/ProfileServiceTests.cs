using Intune.Commander.Core.Models;
using Intune.Commander.Core.Services;
using NSubstitute;

namespace Intune.Commander.Core.Tests.Services;

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
    public void SetActiveProfile_SetsLastUsed()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var p1 = _service.AddProfile(CreateTestProfile("One"));

        _service.SetActiveProfile(p1.Id);

        Assert.NotNull(p1.LastUsed);
        Assert.True(p1.LastUsed >= before);
        Assert.True(p1.LastUsed <= DateTime.UtcNow.AddSeconds(1));
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

    [Fact]
    public async Task Load_LegacyPath_MigratesToPrimaryPath()
    {
        // Arrange — write a profile to the "legacy" location only
        var legacyDir = Path.Combine(Path.GetTempPath(), $"intunemanager-legacy-{Guid.NewGuid():N}");
        var legacyPath = Path.Combine(legacyDir, "profiles.json");
        var newPath = Path.Combine(Path.GetTempPath(), $"intunemanager-new-{Guid.NewGuid():N}", "profiles.json");
        try
        {
            Directory.CreateDirectory(legacyDir);
            var legacyService = new ProfileService(legacyPath);
            legacyService.AddProfile(CreateTestProfile("LegacyUser"));
            await legacyService.SaveAsync();

            // Act — create service pointing at new path with legacy fallback
            var service = new ProfileService(newPath, legacyProfilePath: legacyPath);
            await service.LoadAsync();

            // Profiles loaded from legacy location
            Assert.Single(service.Profiles);
            Assert.Equal("LegacyUser", service.Profiles[0].Name);

            // New path was written (migration happened)
            Assert.True(File.Exists(newPath));

            // Legacy file still present (we don't delete it)
            Assert.True(File.Exists(legacyPath));

            // Subsequent load from new path works without legacy
            var reloaded = new ProfileService(newPath);
            await reloaded.LoadAsync();
            Assert.Single(reloaded.Profiles);
            Assert.Equal("LegacyUser", reloaded.Profiles[0].Name);
        }
        finally
        {
            try { Directory.Delete(legacyDir, true); } catch { }
            var newDir = Path.GetDirectoryName(newPath);
            if (newDir != null) try { Directory.Delete(newDir, true); } catch { }
        }
    }

    [Fact]
    public async Task Load_NoLegacyPath_ReturnsEmpty()
    {
        // When neither new nor legacy path exists, should get empty store
        var service = new ProfileService(_tempPath, legacyProfilePath: null);
        await service.LoadAsync();
        Assert.Empty(service.Profiles);
    }

    private static TenantProfile CreateTestProfile(string name) => new()
    {
        Name = name,
        TenantId = Guid.NewGuid().ToString(),
        ClientId = Guid.NewGuid().ToString(),
        Cloud = CloudEnvironment.Commercial
    };

    // ── Encryption interaction tests ──────────────────────────────────────────

    [Fact]
    public async Task SaveAsync_WithEncryptionService_CallsEncryptAndWritesMarker()
    {
        var encryption = Substitute.For<IProfileEncryptionService>();
        encryption.Encrypt(Arg.Any<string>()).Returns("encrypted-payload");

        var path = Path.Combine(Path.GetTempPath(), $"enc-test-{Guid.NewGuid():N}.json");
        try
        {
            var svc = new ProfileService(path, encryption: encryption);
            svc.AddProfile(CreateTestProfile("Enc"));

            await svc.SaveAsync();

            encryption.Received(1).Encrypt(Arg.Any<string>());
            var written = await File.ReadAllTextAsync(path);
            Assert.StartsWith("INTUNEMANAGER_ENC:", written);
            Assert.Contains("encrypted-payload", written);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public async Task LoadAsync_WithEncryptionService_CallsDecryptForEncryptedFile()
    {
        var encryption = Substitute.For<IProfileEncryptionService>();
        encryption.Encrypt(Arg.Any<string>()).Returns("encrypted-payload");
        // Decrypt should return valid JSON so load succeeds
        var store = new ProfileStore();
        store.Profiles.Add(CreateTestProfile("DecTest"));
        var json = System.Text.Json.JsonSerializer.Serialize(store);
        encryption.Decrypt("encrypted-payload").Returns(json);

        var path = Path.Combine(Path.GetTempPath(), $"dec-test-{Guid.NewGuid():N}.json");
        try
        {
            var svc = new ProfileService(path, encryption: encryption);
            svc.AddProfile(CreateTestProfile("DecTest"));
            await svc.SaveAsync();

            var loader = new ProfileService(path, encryption: encryption);
            await loader.LoadAsync();

            encryption.Received(1).Decrypt("encrypted-payload");
            Assert.Single(loader.Profiles);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public async Task LoadAsync_PlaintextFileWithEncryptionEnabled_LoadsWithoutDecrypt()
    {
        var encryption = Substitute.For<IProfileEncryptionService>();

        // Write a plaintext profiles file (no marker)
        var store = new ProfileStore();
        store.Profiles.Add(CreateTestProfile("Plain"));
        var json = System.Text.Json.JsonSerializer.Serialize(store);
        var path = Path.Combine(Path.GetTempPath(), $"plain-test-{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(path, json);

            var svc = new ProfileService(path, encryption: encryption);
            await svc.LoadAsync();

            // Decrypt should NOT be called for a plaintext file
            encryption.DidNotReceive().Decrypt(Arg.Any<string>());
            Assert.Single(svc.Profiles);
            Assert.Equal("Plain", svc.Profiles[0].Name);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}

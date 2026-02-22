using Intune.Commander.Core.Models;

namespace Intune.Commander.Core.Tests.Models;

/// <summary>
/// Tests for simple model/DTO classes that don't have dedicated test files.
/// </summary>
public class CacheEntryTests
{
    [Fact]
    public void CacheEntry_DefaultValues_AreCorrect()
    {
        var entry = new CacheEntry();

        Assert.Equal("", entry.Id);
        Assert.Equal("", entry.TenantId);
        Assert.Equal("", entry.DataType);
        Assert.Equal("", entry.JsonData);
        Assert.Equal(default, entry.CachedAtUtc);
        Assert.Equal(default, entry.ExpiresAtUtc);
        Assert.Equal(0, entry.ItemCount);
    }

    [Fact]
    public void CacheEntry_CanSetAllProperties()
    {
        var now = DateTime.UtcNow;
        var expires = now.AddHours(24);

        var entry = new CacheEntry
        {
            Id = "tenant1:DeviceConfigurations",
            TenantId = "tenant1",
            DataType = "DeviceConfigurations",
            JsonData = "[{\"id\":\"cfg1\"}]",
            CachedAtUtc = now,
            ExpiresAtUtc = expires,
            ItemCount = 42
        };

        Assert.Equal("tenant1:DeviceConfigurations", entry.Id);
        Assert.Equal("tenant1", entry.TenantId);
        Assert.Equal("DeviceConfigurations", entry.DataType);
        Assert.Equal("[{\"id\":\"cfg1\"}]", entry.JsonData);
        Assert.Equal(now, entry.CachedAtUtc);
        Assert.Equal(expires, entry.ExpiresAtUtc);
        Assert.Equal(42, entry.ItemCount);
    }

    [Fact]
    public void CacheEntry_IsExpired_WhenExpiresAtIsInPast()
    {
        var entry = new CacheEntry
        {
            ExpiresAtUtc = DateTime.UtcNow.AddHours(-1)
        };

        Assert.True(entry.ExpiresAtUtc < DateTime.UtcNow);
    }

    [Fact]
    public void CacheEntry_IsNotExpired_WhenExpiresAtIsInFuture()
    {
        var entry = new CacheEntry
        {
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
        };

        Assert.True(entry.ExpiresAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public void CacheEntry_ItemCount_CanBeZeroOrPositive()
    {
        var entryZero = new CacheEntry { ItemCount = 0 };
        var entryPos = new CacheEntry { ItemCount = 1000 };

        Assert.Equal(0, entryZero.ItemCount);
        Assert.Equal(1000, entryPos.ItemCount);
    }
}

public class ProfileStoreTests
{
    [Fact]
    public void ProfileStore_DefaultProfiles_IsEmptyList()
    {
        var store = new ProfileStore();

        Assert.NotNull(store.Profiles);
        Assert.Empty(store.Profiles);
    }

    [Fact]
    public void ProfileStore_DefaultActiveProfileId_IsNull()
    {
        var store = new ProfileStore();

        Assert.Null(store.ActiveProfileId);
    }

    [Fact]
    public void ProfileStore_CanAddProfiles()
    {
        var store = new ProfileStore();
        store.Profiles.Add(new TenantProfile
        {
            Name = "Test",
            TenantId = "tenant-1",
            ClientId = "client-1"
        });

        Assert.Single(store.Profiles);
        Assert.Equal("Test", store.Profiles[0].Name);
    }

    [Fact]
    public void ProfileStore_CanSetActiveProfileId()
    {
        var store = new ProfileStore
        {
            ActiveProfileId = "profile-abc"
        };

        Assert.Equal("profile-abc", store.ActiveProfileId);
    }

    [Fact]
    public void ProfileStore_CanHoldMultipleProfiles()
    {
        var store = new ProfileStore
        {
            Profiles =
            [
                new TenantProfile { Name = "Profile1", TenantId = "t1", ClientId = "c1" },
                new TenantProfile { Name = "Profile2", TenantId = "t2", ClientId = "c2" },
                new TenantProfile { Name = "Profile3", TenantId = "t3", ClientId = "c3" }
            ]
        };

        Assert.Equal(3, store.Profiles.Count);
    }

    [Fact]
    public void ProfileStore_ActiveProfileIdNullable_AcceptsNull()
    {
        var store = new ProfileStore
        {
            ActiveProfileId = "some-id"
        };
        store.ActiveProfileId = null;

        Assert.Null(store.ActiveProfileId);
    }
}

public class GroupAssignedObjectTests
{
    [Fact]
    public void GroupAssignedObject_DefaultValues_AreEmpty()
    {
        var obj = new GroupAssignedObject();

        Assert.Equal("", obj.ObjectId);
        Assert.Equal("", obj.DisplayName);
        Assert.Equal("", obj.ObjectType);
        Assert.Equal("", obj.Category);
        Assert.Equal("", obj.Platform);
        Assert.Equal("", obj.AssignmentIntent);
        Assert.False(obj.IsExclusion);
    }

    [Fact]
    public void GroupAssignedObject_CanSetAllProperties()
    {
        var obj = new GroupAssignedObject
        {
            ObjectId = "obj-123",
            DisplayName = "My Policy",
            ObjectType = "DeviceConfiguration",
            Category = "Configuration",
            Platform = "Windows",
            AssignmentIntent = "Required",
            IsExclusion = true
        };

        Assert.Equal("obj-123", obj.ObjectId);
        Assert.Equal("My Policy", obj.DisplayName);
        Assert.Equal("DeviceConfiguration", obj.ObjectType);
        Assert.Equal("Configuration", obj.Category);
        Assert.Equal("Windows", obj.Platform);
        Assert.Equal("Required", obj.AssignmentIntent);
        Assert.True(obj.IsExclusion);
    }

    [Fact]
    public void GroupAssignedObject_IsExclusionDefaultsFalse()
    {
        var obj = new GroupAssignedObject { ObjectId = "x" };

        Assert.False(obj.IsExclusion);
    }
}

public class MigrationEntryTests
{
    [Fact]
    public void MigrationEntry_ExportedAt_DefaultsToUtcNow()
    {
        var before = DateTime.UtcNow;
        var entry = new MigrationEntry
        {
            ObjectType = "DeviceConfiguration",
            OriginalId = "id-1",
            Name = "Test"
        };
        var after = DateTime.UtcNow;

        Assert.True(entry.ExportedAt >= before);
        Assert.True(entry.ExportedAt <= after);
    }

    [Fact]
    public void MigrationEntry_NewId_IsNullByDefault()
    {
        var entry = new MigrationEntry
        {
            ObjectType = "Policy",
            OriginalId = "orig-id",
            Name = "Test Policy"
        };

        Assert.Null(entry.NewId);
    }

    [Fact]
    public void MigrationEntry_NewId_CanBeSet()
    {
        var entry = new MigrationEntry
        {
            ObjectType = "Policy",
            OriginalId = "orig-id",
            Name = "Test",
            NewId = "new-id-xyz"
        };

        Assert.Equal("new-id-xyz", entry.NewId);
    }

    [Fact]
    public void MigrationEntry_RequiredProperties_CanBeSet()
    {
        var entry = new MigrationEntry
        {
            ObjectType = "CompliancePolicy",
            OriginalId = "cp-001",
            Name = "My Compliance"
        };

        Assert.Equal("CompliancePolicy", entry.ObjectType);
        Assert.Equal("cp-001", entry.OriginalId);
        Assert.Equal("My Compliance", entry.Name);
    }
}

public class TenantProfileTests
{
    [Fact]
    public void TenantProfile_Id_DefaultsToNewGuid()
    {
        var profile = new TenantProfile
        {
            Name = "Test",
            TenantId = "t1",
            ClientId = "c1"
        };

        Assert.True(Guid.TryParse(profile.Id, out _));
    }

    [Fact]
    public void TenantProfile_TwoInstances_HaveDifferentDefaultIds()
    {
        var p1 = new TenantProfile { Name = "A", TenantId = "t1", ClientId = "c1" };
        var p2 = new TenantProfile { Name = "B", TenantId = "t2", ClientId = "c2" };

        Assert.NotEqual(p1.Id, p2.Id);
    }

    [Fact]
    public void TenantProfile_AuthMethod_DefaultsToInteractive()
    {
        var profile = new TenantProfile
        {
            Name = "Test",
            TenantId = "t1",
            ClientId = "c1"
        };

        Assert.Equal(AuthMethod.Interactive, profile.AuthMethod);
    }

    [Fact]
    public void TenantProfile_ClientSecret_IsNullByDefault()
    {
        var profile = new TenantProfile
        {
            Name = "Test",
            TenantId = "t1",
            ClientId = "c1"
        };

        Assert.Null(profile.ClientSecret);
    }

    [Fact]
    public void TenantProfile_LastUsed_IsNullByDefault()
    {
        var profile = new TenantProfile
        {
            Name = "Test",
            TenantId = "t1",
            ClientId = "c1"
        };

        Assert.Null(profile.LastUsed);
    }

    [Fact]
    public void TenantProfile_CanSetAllProperties()
    {
        var lastUsed = DateTime.UtcNow;
        var profile = new TenantProfile
        {
            Id = "my-id",
            Name = "Production",
            TenantId = "tenant-abc",
            ClientId = "client-xyz",
            Cloud = CloudEnvironment.GCCHigh,
            AuthMethod = AuthMethod.ClientSecret,
            ClientSecret = "secret-value",
            LastUsed = lastUsed
        };

        Assert.Equal("my-id", profile.Id);
        Assert.Equal("Production", profile.Name);
        Assert.Equal("tenant-abc", profile.TenantId);
        Assert.Equal("client-xyz", profile.ClientId);
        Assert.Equal(CloudEnvironment.GCCHigh, profile.Cloud);
        Assert.Equal(AuthMethod.ClientSecret, profile.AuthMethod);
        Assert.Equal("secret-value", profile.ClientSecret);
        Assert.Equal(lastUsed, profile.LastUsed);
    }
}

using IntuneManager.Core.Models;

namespace IntuneManager.Core.Tests.Models;

public class ValidationTests
{
    [Fact]
    public void GuidTryParse_ValidGuidWithHyphens_ReturnsTrue()
    {
        var result = Guid.TryParse("12345678-1234-1234-1234-123456789abc", out _);
        Assert.True(result);
    }

    [Fact]
    public void GuidTryParse_ValidGuidWithoutHyphens_ReturnsTrue()
    {
        var result = Guid.TryParse("12345678123412341234123456789abc", out _);
        Assert.True(result);
    }

    [Fact]
    public void GuidTryParse_NonGuidString_ReturnsFalse()
    {
        var result = Guid.TryParse("not-a-guid", out _);
        Assert.False(result);
    }

    [Fact]
    public void GuidTryParse_EmptyString_ReturnsFalse()
    {
        var result = Guid.TryParse("", out _);
        Assert.False(result);
    }

    [Fact]
    public void GuidTryParse_ShortGuid_ReturnsFalse()
    {
        var result = Guid.TryParse("12345678-1234-1234-1234", out _);
        Assert.False(result);
    }

    [Fact]
    public void GuidTryParse_InvalidHexCharacters_ReturnsFalse()
    {
        var result = Guid.TryParse("12345678-1234-1234-1234-123456789xyz", out _);
        Assert.False(result);
    }

    [Fact]
    public void CloudEnvironment_HasAllExpectedValues()
    {
        var values = Enum.GetValues<CloudEnvironment>();
        Assert.Equal(4, values.Length);
        Assert.Contains(CloudEnvironment.Commercial, values);
        Assert.Contains(CloudEnvironment.GCC, values);
        Assert.Contains(CloudEnvironment.GCCHigh, values);
        Assert.Contains(CloudEnvironment.DoD, values);
    }

    [Fact]
    public void TenantProfile_DefaultCloudIsCommercial()
    {
        var profile = new TenantProfile
        {
            Name = "Test",
            TenantId = Guid.NewGuid().ToString(),
            ClientId = Guid.NewGuid().ToString()
        };

        Assert.Equal(CloudEnvironment.Commercial, profile.Cloud);
    }

    [Fact]
    public void TenantProfile_CloudCanBeSetToAllValues()
    {
        var profile = new TenantProfile
        {
            Name = "Test",
            TenantId = Guid.NewGuid().ToString(),
            ClientId = Guid.NewGuid().ToString()
        };

        foreach (var cloud in Enum.GetValues<CloudEnvironment>())
        {
            profile.Cloud = cloud;
            Assert.Equal(cloud, profile.Cloud);
        }
    }
}

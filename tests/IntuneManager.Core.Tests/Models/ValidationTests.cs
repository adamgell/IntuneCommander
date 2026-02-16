using IntuneManager.Core.Models;

namespace IntuneManager.Core.Tests.Models;

public class ValidationTests
{
    [Theory]
    [InlineData("12345678-1234-1234-1234-123456789abc", true)]
    [InlineData("12345678123412341234123456789abc", true)] // No hyphens — Guid.TryParse accepts this
    [InlineData("not-a-guid", false)]
    [InlineData("", false)]
    [InlineData("12345678-1234-1234-1234", false)]
    [InlineData("12345678-1234-1234-1234-123456789xyz", false)]
    public void GuidTryParse_ValidatesCorrectly(string input, bool expected)
    {
        var result = Guid.TryParse(input, out _);
        Assert.Equal(expected, result);
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

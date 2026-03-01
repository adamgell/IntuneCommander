using Intune.Commander.Core.Models;

namespace Intune.Commander.Core.Tests.Models;

public class WellKnownAppRegistryTests
{
    [Fact]
    public void Apps_LoadedFromEmbeddedResource_NotEmpty()
    {
        Assert.NotEmpty(WellKnownAppRegistry.Apps);
    }

    [Fact]
    public void Apps_ContainsKnownEntry_MicrosoftGraph()
    {
        Assert.True(WellKnownAppRegistry.Apps.ContainsKey("00000003-0000-0000-c000-000000000000"));
        Assert.Equal("Microsoft Graph", WellKnownAppRegistry.Apps["00000003-0000-0000-c000-000000000000"]);
    }

    [Fact]
    public void Apps_ContainsKnownEntry_AzurePurview()
    {
        // First entry in the JSON
        Assert.True(WellKnownAppRegistry.Apps.ContainsKey("73c2949e-da2d-457a-9607-fcc665198967"));
        Assert.Equal("Azure Purview", WellKnownAppRegistry.Apps["73c2949e-da2d-457a-9607-fcc665198967"]);
    }

    [Fact]
    public void Apps_CaseInsensitiveLookup()
    {
        var upper = "00000003-0000-0000-C000-000000000000";
        var lower = "00000003-0000-0000-c000-000000000000";
        Assert.True(WellKnownAppRegistry.Apps.ContainsKey(upper));
        Assert.True(WellKnownAppRegistry.Apps.ContainsKey(lower));
    }

    [Fact]
    public void Apps_HasLargeEntryCount()
    {
        // MicrosoftApps.json has 4000+ entries
        Assert.True(WellKnownAppRegistry.Apps.Count > 1000,
            $"Expected > 1000 entries but found {WellKnownAppRegistry.Apps.Count}");
    }

    [Fact]
    public void Resolve_KnownAppId_ReturnsDisplayName()
    {
        var name = WellKnownAppRegistry.Resolve("00000003-0000-0000-c000-000000000000");
        Assert.Equal("Microsoft Graph", name);
    }

    [Fact]
    public void Resolve_UnknownId_ReturnsOriginalId()
    {
        var unknown = "this-is-not-a-known-app-id";
        Assert.Equal(unknown, WellKnownAppRegistry.Resolve(unknown));
    }

    [Fact]
    public void Resolve_NullOrEmpty_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, WellKnownAppRegistry.Resolve(null));
        Assert.Equal(string.Empty, WellKnownAppRegistry.Resolve(""));
    }
}

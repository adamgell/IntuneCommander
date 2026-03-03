using Intune.Commander.CLI.Helpers;
using Intune.Commander.Core.Models;
using Intune.Commander.Core.Services;

namespace Intune.Commander.CLI.Tests;

public sealed class ProfileResolverTests : IDisposable
{
    private readonly string _tempFile = Path.Combine(Path.GetTempPath(), $"profiles-{Guid.NewGuid()}.json");
    private readonly string? _originalTenant = Environment.GetEnvironmentVariable("IC_TENANT_ID");
    private readonly string? _originalClient = Environment.GetEnvironmentVariable("IC_CLIENT_ID");
    private readonly string? _originalSecret = Environment.GetEnvironmentVariable("IC_CLIENT_SECRET");
    private readonly string? _originalCloud = Environment.GetEnvironmentVariable("IC_CLOUD");

    [Fact]
    public async Task ResolveAsync_UsesEnvVars_WhenNoProfileNameProvided()
    {
        Environment.SetEnvironmentVariable("IC_TENANT_ID", "tenant-id");
        Environment.SetEnvironmentVariable("IC_CLIENT_ID", "client-id");
        Environment.SetEnvironmentVariable("IC_CLIENT_SECRET", "secret");
        Environment.SetEnvironmentVariable("IC_CLOUD", "GCC");

        var profileService = new ProfileService(_tempFile);

        var result = await ProfileResolver.ResolveAsync(profileService, null, null, null, null, null, CancellationToken.None);

        Assert.Equal("tenant-id", result.TenantId);
        Assert.Equal("client-id", result.ClientId);
        Assert.Equal("secret", result.ClientSecret);
        Assert.Equal(CloudEnvironment.GCC, result.Cloud);
        Assert.Equal(AuthMethod.ClientSecret, result.AuthMethod);
    }

    [Fact]
    public async Task ResolveAsync_LoadsProfileByName()
    {
        var profileService = new ProfileService(_tempFile);
        profileService.AddProfile(new TenantProfile
        {
            Name = "Contoso",
            TenantId = "tenant",
            ClientId = "client",
            Cloud = CloudEnvironment.Commercial,
            AuthMethod = AuthMethod.DeviceCode
        });
        await profileService.SaveAsync();

        var result = await ProfileResolver.ResolveAsync(profileService, "Contoso", null, null, null, null, CancellationToken.None);

        Assert.Equal("Contoso", result.Name);
        Assert.Equal("tenant", result.TenantId);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("IC_TENANT_ID", _originalTenant);
        Environment.SetEnvironmentVariable("IC_CLIENT_ID", _originalClient);
        Environment.SetEnvironmentVariable("IC_CLIENT_SECRET", _originalSecret);
        Environment.SetEnvironmentVariable("IC_CLOUD", _originalCloud);

        if (File.Exists(_tempFile))
            File.Delete(_tempFile);
    }
}

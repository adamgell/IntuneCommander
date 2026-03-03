using Intune.Commander.Core.Models;
using Intune.Commander.Core.Services;

namespace Intune.Commander.CLI.Helpers;

public static class ProfileResolver
{
    private const string TenantEnv = "IC_TENANT_ID";
    private const string ClientEnv = "IC_CLIENT_ID";
    private const string SecretEnv = "IC_CLIENT_SECRET";
    private const string CloudEnv = "IC_CLOUD";

    public static async Task<TenantProfile> ResolveAsync(
        ProfileService profileService,
        string? profileName,
        string? tenantId,
        string? clientId,
        string? secret,
        string? cloud,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(profileName))
        {
            await profileService.LoadAsync(cancellationToken);
            var profile = profileService.Profiles.FirstOrDefault(
                p => string.Equals(p.Name, profileName, StringComparison.OrdinalIgnoreCase));

            return profile ?? throw new InvalidOperationException($"Profile \"{profileName}\" was not found.");
        }

        tenantId ??= Environment.GetEnvironmentVariable(TenantEnv);
        clientId ??= Environment.GetEnvironmentVariable(ClientEnv);
        secret ??= Environment.GetEnvironmentVariable(SecretEnv);
        cloud ??= Environment.GetEnvironmentVariable(CloudEnv);

        if (string.IsNullOrWhiteSpace(tenantId))
            throw new InvalidOperationException("Tenant ID is required (use --tenant-id or IC_TENANT_ID).");

        if (string.IsNullOrWhiteSpace(clientId))
            throw new InvalidOperationException("Client ID is required (use --client-id or IC_CLIENT_ID).");

        var parsedCloud = ParseCloud(cloud);

        return new TenantProfile
        {
            Name = "CLI",
            TenantId = tenantId,
            ClientId = clientId,
            Cloud = parsedCloud,
            AuthMethod = string.IsNullOrWhiteSpace(secret) ? AuthMethod.DeviceCode : AuthMethod.ClientSecret,
            ClientSecret = secret
        };
    }

    private static CloudEnvironment ParseCloud(string? cloud)
    {
        if (string.IsNullOrWhiteSpace(cloud))
            return CloudEnvironment.Commercial;

        return cloud.Trim().ToLowerInvariant() switch
        {
            "commercial" => CloudEnvironment.Commercial,
            "gcc" => CloudEnvironment.GCC,
            "gcchigh" or "gcc-high" => CloudEnvironment.GCCHigh,
            "dod" => CloudEnvironment.DoD,
            _ => throw new InvalidOperationException($"Unsupported cloud \"{cloud}\".")
        };
    }
}

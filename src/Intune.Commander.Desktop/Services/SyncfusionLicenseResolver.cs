using System;
using System.Linq;
using System.Reflection;

namespace Intune.Commander.Desktop.Services;

/// <summary>
/// Resolves the Syncfusion license key using two sources in priority order:
/// 1. SYNCFUSION_LICENSE_KEY environment variable (local development / CI override).
/// 2. AssemblyMetadataAttribute baked into the binary at publish time via the
///    SyncfusionLicenseKey MSBuild property (released .exe — no user action required).
/// </summary>
public static class SyncfusionLicenseResolver
{
    private const string EnvVar = "SYNCFUSION_LICENSE_KEY";
    private const string MetadataKey = "SyncfusionLicenseKey";

    public static string? ResolveLicenseKey()
    {
        var fromEnv = Environment.GetEnvironmentVariable(EnvVar);
        if (!string.IsNullOrWhiteSpace(fromEnv))
            return fromEnv.Trim();

        var fromAssembly = Assembly.GetExecutingAssembly()
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == MetadataKey)
            ?.Value;

        return string.IsNullOrWhiteSpace(fromAssembly) ? null : fromAssembly.Trim();
    }
}
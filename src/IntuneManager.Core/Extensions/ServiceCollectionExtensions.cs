using IntuneManager.Core.Auth;
using IntuneManager.Core.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace IntuneManager.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIntuneManagerCore(this IServiceCollection services)
    {
        // DataProtection — keys stored in the user's local app data folder
        var keysPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "IntuneManager", "keys");

        services.AddDataProtection()
            .SetApplicationName("IntuneManager")
            .PersistKeysToFileSystem(new DirectoryInfo(keysPath));

        services.AddSingleton<IProfileEncryptionService, ProfileEncryptionService>();

        services.AddSingleton<IAuthenticationProvider, InteractiveBrowserAuthProvider>();
        services.AddSingleton<IntuneGraphClientFactory>();

        // ProfileService depends on IProfileEncryptionService, so resolve it via factory
        services.AddSingleton<ProfileService>(sp =>
            new ProfileService(encryption: sp.GetRequiredService<IProfileEncryptionService>()));

        services.AddTransient<IExportService, ExportService>();

        // Cache — singleton LiteDB-backed cache with encrypted storage
        services.AddSingleton<ICacheService>(sp =>
            new CacheService(sp.GetRequiredService<IDataProtectionProvider>()));

        return services;
    }
}

using IntuneManager.Core.Auth;
using IntuneManager.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace IntuneManager.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIntuneManagerCore(this IServiceCollection services)
    {
        services.AddSingleton<IAuthenticationProvider, CompositeAuthenticationProvider>();
        services.AddSingleton<IntuneGraphClientFactory>();
        services.AddSingleton<ProfileService>();
        services.AddTransient<IExportService, ExportService>();

        return services;
    }
}

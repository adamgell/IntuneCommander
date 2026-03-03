using Intune.Commander.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Intune.Commander.CLI.Helpers;

internal static class CliServices
{
    public static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddIntuneCommanderCore();
        return services.BuildServiceProvider();
    }
}

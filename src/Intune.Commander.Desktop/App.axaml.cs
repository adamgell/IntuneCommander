using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Intune.Commander.Core.Extensions;
using Intune.Commander.Desktop.Services;
using Intune.Commander.Desktop.ViewModels;
using Intune.Commander.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using SukiUI.Theme.Shadcn;
using Syncfusion.Licensing;
using System;

namespace Intune.Commander.Desktop;

public partial class App : Application
{
    public static ServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        var syncfusionLicense = SyncfusionLicenseResolver.ResolveLicenseKey();
        if (!string.IsNullOrEmpty(syncfusionLicense))
        {
            SyncfusionLicenseProvider.RegisterLicense(syncfusionLicense);
        }

        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();

            var services = new ServiceCollection();
            services.AddIntuneCommanderCore();
            services.AddTransient<MainWindowViewModel>();
            Services = services.BuildServiceProvider();

            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>(),
            };

            Shadcn.Configure(this, ThemeVariant.Dark);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}

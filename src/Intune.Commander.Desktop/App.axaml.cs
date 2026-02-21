using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Classic.Avalonia.Theme;
using Intune.Commander.Core.Extensions;
using Intune.Commander.Core.Services;
using Intune.Commander.Desktop.Models;
using Intune.Commander.Desktop.Services;
using Intune.Commander.Desktop.ViewModels;
using Intune.Commander.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using Syncfusion.Licensing;

namespace Intune.Commander.Desktop;

public partial class App : Application
{
    public static ServiceProvider? Services { get; private set; }
    public static AppTheme CurrentTheme { get; private set; } = AppTheme.Fluent;

    // Injected directly onto app.Resources (flat, not in ThemeDictionaries) when Classic is active.
    // These cover Fluent-only SystemControl* keys that Classic theme does not define at all.
    private static readonly Dictionary<string, object> _classicFlatResources = new()
    {
        ["SystemControlBackgroundChromeMediumBrush"]    = new SolidColorBrush(Color.Parse("#C0C0C0")),
        ["SystemControlBackgroundChromeMediumLowBrush"] = new SolidColorBrush(Color.Parse("#D4D0C8")),
        ["SystemControlBackgroundAltHighBrush"]         = new SolidColorBrush(Colors.White),
        ["SystemControlForegroundBaseMediumLowBrush"]   = new SolidColorBrush(Color.Parse("#808080")),
        ["SystemControlForegroundBaseHighBrush"]        = new SolidColorBrush(Colors.Black),
        ["SystemControlHighlightListLowBrush"]          = new SolidColorBrush(Color.FromArgb(30, 0, 0, 128)),
        ["SystemAccentColor"]                           = Color.Parse("#000080"),
    };

    // Injected INTO ThemeDictionaries[Light] and [Dark] when Classic is active.
    // MUST be here because Avalonia resolves ThemeDictionaries entries BEFORE flat
    // MergedDictionaries, so AppNavTextBrush etc. can only be overridden this way.
    private static readonly Dictionary<string, object> _classicThemeResources = new()
    {
        ["AppNavTextBrush"]         = new SolidColorBrush(Colors.Black),
        ["AppTextSecondaryBrush"]   = new SolidColorBrush(Color.Parse("#444444")),
        ["AppErrorTextBrush"]       = new SolidColorBrush(Color.Parse("#CC0000")),
        ["AppErrorBackgroundBrush"] = new SolidColorBrush(Color.FromArgb(26, 255, 0, 0)),
    };

    // Saved originals from ThemeDictionaries so we can restore on switching back to Fluent.
    private static readonly Dictionary<ThemeVariant, Dictionary<string, object?>> _savedThemeValues = new()
    {
        [ThemeVariant.Light] = [],
        [ThemeVariant.Dark]  = [],
    };

    public static void ApplyTheme(AppTheme theme)
    {
        CurrentTheme = theme;
        var app = Application.Current!;

        // ── 1. Swap the main theme style ─────────────────────────────────────────
        var themeIndex = app.Styles
            .Select((s, i) => new { s, i })
            .FirstOrDefault(x => x.s is ClassicTheme || x.s is FluentTheme)
            ?.i ?? -1;

        IStyle newTheme = theme == AppTheme.Classic ? new ClassicTheme() : new FluentTheme();
        if (themeIndex >= 0)
            app.Styles[themeIndex] = newTheme;
        else
        {
            app.Styles.Insert(0, newTheme);
            themeIndex = 0;
        }

        // ── 2. Swap the DataGrid theme ────────────────────────────────────────────
        const string classicDataGrid = "avares://Classic.Avalonia.Theme.DataGrid/Classic.axaml";
        const string fluentDataGrid  = "avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml";
        var dataGridIndex = app.Styles
            .Select((s, i) => new { s, i })
            .FirstOrDefault(x =>
            {
                if (x.s is not StyleInclude si || si.Source == null) return false;
                var src = si.Source.ToString();
                return src.Contains("Classic.Avalonia.Theme.DataGrid", StringComparison.OrdinalIgnoreCase)
                    || src.Contains("Avalonia.Controls.DataGrid/Themes", StringComparison.OrdinalIgnoreCase);
            })
            ?.i ?? -1;

        var newDataGrid = new StyleInclude(new Uri("avares://Intune.Commander.Desktop"))
        {
            Source = new Uri(theme == AppTheme.Classic ? classicDataGrid : fluentDataGrid)
        };

        if (dataGridIndex >= 0)
            app.Styles[dataGridIndex] = newDataGrid;
        else
            app.Styles.Insert(themeIndex + 1, newDataGrid);

        // ── 3. Patch / restore ThemeDictionaries entries ──────────────────────────
        // Avalonia resolves ThemeDictionaries[Light/Dark] BEFORE flat MergedDictionaries,
        // so App-specific brushes (AppNavTextBrush etc.) MUST be set inside the theme dicts.
        var appResources = (ResourceDictionary)app.Resources;
        foreach (var variant in new[] { ThemeVariant.Light, ThemeVariant.Dark })
        {
            if (!appResources.ThemeDictionaries.TryGetValue(variant, out var provider)
                || provider is not ResourceDictionary themeDict)
                continue;

            if (theme == AppTheme.Classic)
            {
                foreach (var kv in _classicThemeResources)
                {
                    themeDict.TryGetValue(kv.Key, out var original);
                    _savedThemeValues[variant][kv.Key] = original;
                    themeDict[kv.Key] = kv.Value;
                }
            }
            else
            {
                foreach (var kv in _savedThemeValues[variant])
                {
                    if (kv.Value is null)
                        themeDict.Remove(kv.Key);
                    else
                        themeDict[kv.Key] = kv.Value;
                }
                _savedThemeValues[variant].Clear();
            }
        }

        // ── 4. Inject / remove flat SystemControl* resources ──────────────────────
        // These Fluent-only keys don't exist in Classic at all; any location wins for them.
        if (theme == AppTheme.Classic)
        {
            foreach (var kv in _classicFlatResources)
                appResources[kv.Key] = kv.Value;
        }
        else
        {
            foreach (var key in _classicFlatResources.Keys)
                appResources.Remove(key);
        }

        AppSettingsService.Save(new AppSettings { Theme = theme });
    }

    public override void Initialize()
    {
        // Register Syncfusion license using key from environment variable (see project documentation for licensing details)
        var syncfusionLicense = Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY");
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

            // Apply saved theme (replaces default FluentTheme from AXAML if Classic was saved)
            var savedSettings = AppSettingsService.Load();
            if (savedSettings.Theme != AppTheme.Fluent)
                ApplyTheme(savedSettings.Theme);
            else
                CurrentTheme = AppTheme.Fluent;

            var services = new ServiceCollection();
            services.AddIntuneCommanderCore();
            services.AddTransient<MainWindowViewModel>();
            Services = services.BuildServiceProvider();

            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>(),
            };
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

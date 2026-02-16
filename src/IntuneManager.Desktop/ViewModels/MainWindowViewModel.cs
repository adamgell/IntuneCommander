using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IntuneManager.Core.Auth;
using IntuneManager.Core.Models;
using IntuneManager.Core.Services;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace IntuneManager.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ProfileService _profileService;
    private readonly IntuneGraphClientFactory _graphClientFactory;
    private readonly IExportService _exportService;

    private GraphServiceClient? _graphClient;
    private IIntuneService? _intuneService;
    private IImportService? _importService;

    [ObservableProperty]
    private ViewModelBase? _currentView;

    [ObservableProperty]
    private string _windowTitle = "IntuneManager";

    [ObservableProperty]
    private TenantProfile? _activeProfile;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private ObservableCollection<DeviceConfiguration> _deviceConfigurations = [];

    [ObservableProperty]
    private DeviceConfiguration? _selectedConfiguration;

    [ObservableProperty]
    private string _statusText = "Not connected";

    /// <summary>
    /// Profile selected in the toolbar switcher. Triggers switch confirmation.
    /// </summary>
    [ObservableProperty]
    private TenantProfile? _selectedSwitchProfile;

    public ObservableCollection<TenantProfile> SwitcherProfiles { get; } = [];

    /// <summary>
    /// Raised when the user picks a different profile and we need a confirm dialog.
    /// The view subscribes to this, shows the dialog, and calls ConfirmSwitchProfile / CancelSwitchProfile.
    /// </summary>
    public event Func<TenantProfile, Task<bool>>? SwitchProfileRequested;

    public LoginViewModel LoginViewModel { get; }

    public MainWindowViewModel(
        ProfileService profileService,
        IntuneGraphClientFactory graphClientFactory,
        IExportService exportService)
    {
        _profileService = profileService;
        _graphClientFactory = graphClientFactory;
        _exportService = exportService;

        LoginViewModel = new LoginViewModel(profileService, graphClientFactory);
        LoginViewModel.LoginSucceeded += OnLoginSucceeded;

        CurrentView = LoginViewModel;

        // Load profiles asynchronously — never block the UI thread
        _ = LoadProfilesAsync();
    }

    private async Task LoadProfilesAsync()
    {
        try
        {
            await _profileService.LoadAsync();
            LoginViewModel.PopulateSavedProfiles();
            LoginViewModel.SelectActiveProfile();
        }
        catch (Exception ex)
        {
            SetError($"Failed to load profiles: {ex.Message}");
        }
    }

    // Design-time constructor
    public MainWindowViewModel()
    {
        _profileService = null!;
        _graphClientFactory = null!;
        _exportService = null!;
        LoginViewModel = null!;
    }

    private async void OnLoginSucceeded(object? sender, TenantProfile profile)
    {
        await ConnectToProfile(profile);
    }

    private async Task ConnectToProfile(TenantProfile profile)
    {
        ClearError();
        IsBusy = true;
        StatusText = $"Connecting to {profile.Name}...";

        try
        {
            ActiveProfile = profile;
            IsConnected = true;
            WindowTitle = $"IntuneManager - {profile.Name}";
            CurrentView = null; // Show main content instead of login

            _graphClient = await _graphClientFactory.CreateClientAsync(profile);
            _intuneService = new IntuneService(_graphClient);
            _importService = new ImportService(_intuneService);

            RefreshSwitcherProfiles();
            SelectedSwitchProfile = profile;

            StatusText = $"Connected to {profile.Name}";
            await RefreshAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            SetError($"Connection failed: {ex.Message}");
            StatusText = "Connection failed";
            DisconnectInternal();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RefreshSwitcherProfiles()
    {
        SwitcherProfiles.Clear();
        foreach (var p in _profileService.Profiles)
            SwitcherProfiles.Add(p);
    }

    partial void OnSelectedSwitchProfileChanged(TenantProfile? value)
    {
        // Only trigger switch if connected and user picked a *different* profile
        if (value is null || !IsConnected || value.Id == ActiveProfile?.Id)
            return;

        _ = RequestSwitchAsync(value);
    }

    private async Task RequestSwitchAsync(TenantProfile target)
    {
        if (SwitchProfileRequested is not null)
        {
            var confirmed = await SwitchProfileRequested.Invoke(target);
            if (confirmed)
            {
                DisconnectInternal();
                await ConnectToProfile(target);
                target.LastUsed = DateTime.UtcNow;
                _profileService.SetActiveProfile(target.Id);
                await _profileService.SaveAsync();
            }
            else
            {
                // Revert selection silently
                SelectedSwitchProfile = ActiveProfile;
            }
        }
    }

    [RelayCommand]
    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        if (_intuneService == null) return;

        ClearError();
        IsBusy = true;
        StatusText = "Loading device configurations...";

        try
        {
            var configs = await _intuneService.ListDeviceConfigurationsAsync(cancellationToken);
            DeviceConfigurations = new ObservableCollection<DeviceConfiguration>(configs);
            StatusText = $"Loaded {configs.Count} device configuration(s)";
        }
        catch (Exception ex)
        {
            SetError($"Failed to load configurations: {ex.Message}");
            StatusText = "Error loading configurations";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportSelectedAsync(CancellationToken cancellationToken)
    {
        if (SelectedConfiguration == null) return;

        ClearError();
        IsBusy = true;
        StatusText = $"Exporting {SelectedConfiguration.DisplayName}...";

        try
        {
            var outputPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "IntuneExport");

            var migrationTable = new MigrationTable();
            await _exportService.ExportDeviceConfigurationAsync(
                SelectedConfiguration, outputPath, migrationTable, cancellationToken);
            await _exportService.SaveMigrationTableAsync(migrationTable, outputPath, cancellationToken);

            StatusText = $"Exported to {outputPath}";
        }
        catch (Exception ex)
        {
            SetError($"Export failed: {ex.Message}");
            StatusText = "Export failed";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportAllAsync(CancellationToken cancellationToken)
    {
        if (!DeviceConfigurations.Any()) return;

        ClearError();
        IsBusy = true;
        StatusText = "Exporting all configurations...";

        try
        {
            var outputPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "IntuneExport");

            await _exportService.ExportDeviceConfigurationsAsync(
                DeviceConfigurations, outputPath, cancellationToken);

            StatusText = $"Exported {DeviceConfigurations.Count} configuration(s) to {outputPath}";
        }
        catch (Exception ex)
        {
            SetError($"Export failed: {ex.Message}");
            StatusText = "Export failed";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ImportFromFolderAsync(string folderPath, CancellationToken cancellationToken)
    {
        if (_importService == null) return;

        ClearError();
        IsBusy = true;
        StatusText = "Importing configurations...";

        try
        {
            var configs = await _importService.ReadDeviceConfigurationsFromFolderAsync(folderPath, cancellationToken);
            var migrationTable = await _importService.ReadMigrationTableAsync(folderPath, cancellationToken);

            var imported = 0;
            foreach (var config in configs)
            {
                await _importService.ImportDeviceConfigurationAsync(config, migrationTable, cancellationToken);
                imported++;
                StatusText = $"Imported {imported} of {configs.Count}...";
            }

            // Save updated migration table
            await _exportService.SaveMigrationTableAsync(migrationTable, folderPath, cancellationToken);

            StatusText = $"Imported {imported} configuration(s)";

            // Refresh the list
            await RefreshAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            SetError($"Import failed: {ex.Message}");
            StatusText = "Import failed";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Disconnect()
    {
        DisconnectInternal();
        CurrentView = LoginViewModel;
        LoginViewModel.PopulateSavedProfiles();
        LoginViewModel.SelectActiveProfile();
    }

    private void DisconnectInternal()
    {
        IsConnected = false;
        ActiveProfile = null;
        WindowTitle = "IntuneManager";
        StatusText = "Not connected";
        DeviceConfigurations.Clear();
        SelectedConfiguration = null;
        _graphClient = null;
        _intuneService = null;
        _importService = null;
    }
}

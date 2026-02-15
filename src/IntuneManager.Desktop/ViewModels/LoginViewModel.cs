using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IntuneManager.Core.Auth;
using IntuneManager.Core.Models;
using IntuneManager.Core.Services;

namespace IntuneManager.Desktop.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly ProfileService _profileService;
    private readonly IntuneGraphClientFactory _graphClientFactory;

    [ObservableProperty]
    private string _tenantId = string.Empty;

    [ObservableProperty]
    private string _clientId = string.Empty;

    [ObservableProperty]
    private string _profileName = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public event EventHandler<TenantProfile>? LoginSucceeded;

    public LoginViewModel(ProfileService profileService, IntuneGraphClientFactory graphClientFactory)
    {
        _profileService = profileService;
        _graphClientFactory = graphClientFactory;
    }

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync(CancellationToken cancellationToken)
    {
        ClearError();
        IsBusy = true;
        StatusMessage = "Authenticating...";

        try
        {
            var profile = new TenantProfile
            {
                Name = string.IsNullOrWhiteSpace(ProfileName) ? $"Tenant-{TenantId[..8]}" : ProfileName,
                TenantId = TenantId.Trim(),
                ClientId = ClientId.Trim(),
                Cloud = CloudEnvironment.Commercial,
                AuthMethod = AuthMethod.Interactive
            };

            // Test the connection by creating a client and making a request
            var client = await _graphClientFactory.CreateClientAsync(profile, cancellationToken);
            await client.DeviceManagement.GetAsync(cancellationToken: cancellationToken);

            StatusMessage = "Connected successfully!";
            profile.LastUsed = DateTime.UtcNow;

            // Save profile
            _profileService.AddProfile(profile);
            await _profileService.SaveAsync(cancellationToken);

            LoginSucceeded?.Invoke(this, profile);
        }
        catch (Exception ex)
        {
            SetError($"Authentication failed: {ex.Message}");
            StatusMessage = string.Empty;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanLogin()
    {
        return !IsBusy
            && !string.IsNullOrWhiteSpace(TenantId)
            && !string.IsNullOrWhiteSpace(ClientId);
    }

    partial void OnTenantIdChanged(string value) => LoginCommand.NotifyCanExecuteChanged();
    partial void OnClientIdChanged(string value) => LoginCommand.NotifyCanExecuteChanged();
}

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
    private string _clientSecret = string.Empty;

    [ObservableProperty]
    private int _authMethodIndex = 0;

    [ObservableProperty]
    private string _profileName = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public bool IsClientSecretMode => AuthMethodIndex == 1;

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
            var authMethod = AuthMethodIndex == 1 ? AuthMethod.ClientSecret : AuthMethod.Interactive;

            var profile = new TenantProfile
            {
                Name = string.IsNullOrWhiteSpace(ProfileName) ? $"Tenant-{TenantId[..8]}" : ProfileName,
                TenantId = TenantId.Trim(),
                ClientId = ClientId.Trim(),
                ClientSecret = authMethod == AuthMethod.ClientSecret ? ClientSecret.Trim() : null,
                Cloud = CloudEnvironment.Commercial,
                AuthMethod = authMethod
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
        var baseRequirements = !IsBusy
            && !string.IsNullOrWhiteSpace(TenantId)
            && !string.IsNullOrWhiteSpace(ClientId);

        // If client secret mode, also require the secret
        if (AuthMethodIndex == 1)
        {
            return baseRequirements && !string.IsNullOrWhiteSpace(ClientSecret);
        }

        return baseRequirements;
    }

    partial void OnTenantIdChanged(string value) => LoginCommand.NotifyCanExecuteChanged();
    partial void OnClientIdChanged(string value) => LoginCommand.NotifyCanExecuteChanged();
    partial void OnClientSecretChanged(string value) => LoginCommand.NotifyCanExecuteChanged();
    partial void OnAuthMethodIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsClientSecretMode));
        LoginCommand.NotifyCanExecuteChanged();
    }
}

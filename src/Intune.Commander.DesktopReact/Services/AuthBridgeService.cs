using System.Text.Json;
using Azure.Identity;
using Intune.Commander.Core.Auth;
using Intune.Commander.Core.Models;
using Intune.Commander.Core.Services;
using Intune.Commander.DesktopReact.Bridge;
using Microsoft.Graph.Beta;

namespace Intune.Commander.DesktopReact.Services;

public class AuthBridgeService
{
    private readonly IntuneGraphClientFactory _graphClientFactory;
    private readonly ProfileService _profileService;
    private readonly ShellStateBridgeService _shellState;
    private IBridgeService? _bridge;

    private GraphServiceClient? _graphClient;

    private static JsonSerializerOptions JsonOptions => BridgeRouter.JsonOptions;

    public AuthBridgeService(
        IntuneGraphClientFactory graphClientFactory,
        ProfileService profileService,
        ShellStateBridgeService shellState)
    {
        _graphClientFactory = graphClientFactory;
        _profileService = profileService;
        _shellState = shellState;
    }

    public void SetBridge(IBridgeService bridge) => _bridge = bridge;

    public GraphServiceClient? GraphClient => _graphClient;

    public async Task<object> ConnectAsync(JsonElement? payload)
    {
        if (payload is null)
            throw new ArgumentException("Profile data is required");

        var profile = JsonSerializer.Deserialize<TenantProfile>(payload.Value.GetRawText(), JsonOptions)
            ?? throw new ArgumentException("Invalid profile data");

        await _shellState.UpdateAsync(isBusy: true, statusText: $"Connecting to {profile.Name}...", clearError: true);

        try
        {
            // Device code callback to push events to React
            Task DeviceCodeCallback(DeviceCodeInfo info, CancellationToken ct)
            {
                _bridge?.SendEventAsync("deviceCode.received", new
                {
                    userCode = info.UserCode,
                    verificationUrl = info.VerificationUri.AbsoluteUri,
                    message = info.Message
                });
                return Task.CompletedTask;
            }

            var client = await _graphClientFactory.CreateClientAsync(
                profile,
                profile.AuthMethod == AuthMethod.DeviceCode ? DeviceCodeCallback : null);

            // Clear device code after auth completes
            if (profile.AuthMethod == AuthMethod.DeviceCode)
                await (_bridge?.SendEventAsync("deviceCode.cleared", new { }) ?? Task.CompletedTask);

            // Test the connection
            await _shellState.UpdateAsync(statusText: "Verifying connection...");
            await client.DeviceManagement.GetAsync();

            _graphClient = client;

            // Update profile as active
            profile.LastUsed = DateTime.UtcNow;

            // Ensure the profile is saved
            var existing = _profileService.Profiles.FirstOrDefault(p => p.Id == profile.Id);
            if (existing is null)
            {
                if (string.IsNullOrWhiteSpace(profile.Name))
                    profile.Name = $"Tenant-{profile.TenantId[..Math.Min(8, profile.TenantId.Length)]}";

                _profileService.AddProfile(profile);
            }

            _profileService.SetActiveProfile(profile.Id);
            await _profileService.SaveAsync();

            await _shellState.UpdateAsync(
                isConnected: true,
                isBusy: false,
                statusText: $"Connected to {profile.Name}",
                activeProfile: profile);

            return new { success = true };
        }
        catch (Exception ex)
        {
            _graphClient = null;

            // Clear device code on failure too
            await (_bridge?.SendEventAsync("deviceCode.cleared", new { }) ?? Task.CompletedTask);

            await _shellState.UpdateAsync(
                isConnected: false,
                isBusy: false,
                statusText: "Connection failed",
                errorMessage: ex.Message);

            throw;
        }
    }

    public async Task<object> DisconnectAsync()
    {
        _graphClient = null;

        await _shellState.UpdateAsync(
            isConnected: false,
            isBusy: false,
            statusText: "Disconnected",
            activeProfile: null,
            clearError: true);

        return new { success = true };
    }
}

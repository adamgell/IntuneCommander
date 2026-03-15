using Intune.Commander.Core.Models;
using Intune.Commander.DesktopReact.Bridge;

namespace Intune.Commander.DesktopReact.Services;

public class ShellStateBridgeService
{
    private IBridgeService? _bridge;

    public bool IsConnected { get; private set; }
    public bool IsBusy { get; private set; }
    public string StatusText { get; private set; } = "Ready";
    public string? ErrorMessage { get; private set; }
    public TenantProfile? ActiveProfile { get; private set; }

    public void SetBridge(IBridgeService bridge) => _bridge = bridge;

    public object GetState() => new
    {
        isConnected = IsConnected,
        isBusy = IsBusy,
        statusText = StatusText,
        errorMessage = ErrorMessage,
        activeProfile = ActiveProfile
    };

    public async Task UpdateAsync(
        bool? isConnected = null,
        bool? isBusy = null,
        string? statusText = null,
        string? errorMessage = null,
        TenantProfile? activeProfile = null,
        bool clearError = false)
    {
        if (isConnected.HasValue) IsConnected = isConnected.Value;
        if (isBusy.HasValue) IsBusy = isBusy.Value;
        if (statusText is not null) StatusText = statusText;
        if (clearError) ErrorMessage = null;
        else if (errorMessage is not null) ErrorMessage = errorMessage;
        if (activeProfile is not null) ActiveProfile = activeProfile;

        if (_bridge is not null)
            await _bridge.SendEventAsync("state.updated", GetState());
    }
}

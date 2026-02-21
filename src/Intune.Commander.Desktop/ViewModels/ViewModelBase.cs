using CommunityToolkit.Mvvm.ComponentModel;
using Intune.Commander.Desktop.Services;

namespace Intune.Commander.Desktop.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    protected static DebugLogService DebugLog => DebugLogService.Instance;

    protected void ClearError() => ErrorMessage = null;

    protected void SetError(string message)
    {
        ErrorMessage = message;
        DebugLog.LogError(message);
    }
}

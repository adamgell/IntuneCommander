using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IntuneManager.Desktop.Services;

namespace IntuneManager.Desktop.ViewModels;

public partial class DebugLogViewModel : ObservableObject
{
    public ObservableCollection<string> LogEntries => DebugLogService.Instance.Entries;

    [RelayCommand]
    private void ClearLog()
    {
        DebugLogService.Instance.Clear();
    }
}

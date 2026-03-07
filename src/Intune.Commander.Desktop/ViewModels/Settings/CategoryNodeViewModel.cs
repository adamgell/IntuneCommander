using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Intune.Commander.Desktop.ViewModels.Settings;

public partial class CategoryNodeViewModel : ObservableObject
{
    [ObservableProperty]
    private string? _displayName;

    public ObservableCollection<CategoryNodeViewModel> Children { get; } = [];

    public ObservableCollection<SettingViewModelBase> Settings { get; } = [];
}

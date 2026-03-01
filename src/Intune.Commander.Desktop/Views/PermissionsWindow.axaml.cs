using Avalonia.Controls;
using Intune.Commander.Desktop.ViewModels;
using SukiUI.Controls;

namespace Intune.Commander.Desktop.Views;

public partial class PermissionsWindow : SukiWindow
{
    public PermissionsWindow()
    {
        InitializeComponent();
    }

    public PermissionsWindow(MainWindowViewModel vm) : this()
    {
        DataContext = vm;
    }
}

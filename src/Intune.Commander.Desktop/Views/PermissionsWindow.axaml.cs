using Avalonia.Controls;
using Intune.Commander.Desktop.ViewModels;

namespace Intune.Commander.Desktop.Views;

public partial class PermissionsWindow : Window
{
    public PermissionsWindow(MainWindowViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}

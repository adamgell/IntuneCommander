using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Intune.Commander.Desktop.Views.Controls.Details;

public partial class SettingsPolicyEditorPanel : UserControl
{
    public SettingsPolicyEditorPanel() => InitializeComponent();

    private void OnCloseEditorClick(object? sender, RoutedEventArgs e)
    {
        // Close the parent window (SettingsPolicyEditorWindow)
        var window = this.FindAncestorOfType<Window>();
        window?.Close();
    }
}

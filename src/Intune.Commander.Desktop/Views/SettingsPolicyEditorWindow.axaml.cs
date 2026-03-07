using System;
using SukiUI.Controls;

namespace Intune.Commander.Desktop.Views;

public partial class SettingsPolicyEditorWindow : SukiWindow
{
    public SettingsPolicyEditorWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Raised when the window is closed so the VM can clean up subscriptions.
    /// </summary>
    public event Action? EditorClosed;

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        EditorClosed?.Invoke();
    }
}

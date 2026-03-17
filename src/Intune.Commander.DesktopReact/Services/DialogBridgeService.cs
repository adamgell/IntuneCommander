using System.Windows;
using Microsoft.Win32;

namespace Intune.Commander.DesktopReact.Services;

/// <summary>
/// Bridge service for native file/folder picker dialogs.
/// All dialogs are marshaled to the UI thread via Dispatcher.
/// </summary>
public class DialogBridgeService
{
    /// <summary>
    /// Shows a native folder picker dialog and returns the selected path.
    /// Returns { path: string | null, cancelled: bool }.
    /// </summary>
    public async Task<object> PickFolderAsync()
    {
        string? folderPath = null;

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            // Use OpenFolderDialog (WPF .NET 8+)
            var dialog = new OpenFolderDialog
            {
                Title = "Select Folder",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
                folderPath = dialog.FolderName;
        });

        return new { path = folderPath, cancelled = folderPath is null };
    }

    /// <summary>
    /// Shows a native file open dialog and returns the selected path.
    /// Accepts optional filter and title via payload.
    /// Returns { path: string | null, cancelled: bool }.
    /// </summary>
    public async Task<object> PickFileAsync(string? filter = null, string? title = null)
    {
        string? filePath = null;

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var dialog = new OpenFileDialog
            {
                Filter = filter ?? "All files (*.*)|*.*",
                Title = title ?? "Select File"
            };

            if (dialog.ShowDialog() == true)
                filePath = dialog.FileName;
        });

        return new { path = filePath, cancelled = filePath is null };
    }

    /// <summary>
    /// Shows a native save file dialog and returns the selected path.
    /// Accepts optional filter, title, and default filename via payload.
    /// Returns { path: string | null, cancelled: bool }.
    /// </summary>
    public async Task<object> SaveFileAsync(string? filter = null, string? title = null, string? defaultFileName = null)
    {
        string? filePath = null;

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var dialog = new SaveFileDialog
            {
                Filter = filter ?? "All files (*.*)|*.*",
                Title = title ?? "Save File",
                FileName = defaultFileName ?? ""
            };

            if (dialog.ShowDialog() == true)
                filePath = dialog.FileName;
        });

        return new { path = filePath, cancelled = filePath is null };
    }
}

using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using IntuneManager.Core.Models;
using IntuneManager.Desktop.ViewModels;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace IntuneManager.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        var importButton = this.FindControl<Button>("ImportButton");
        if (importButton != null)
        {
            importButton.Click += OnImportClick;
        }

        if (DataContext is MainWindowViewModel vm)
        {
            vm.SwitchProfileRequested += OnSwitchProfileRequested;
        }
    }

    private async Task<bool> OnSwitchProfileRequested(TenantProfile target)
    {
        var box = MessageBoxManager.GetMessageBoxStandard(
            "Switch Profile",
            $"Switch to \"{target.Name}\"?\nYou will be disconnected from the current tenant.",
            ButtonEnum.YesNo,
            MsBox.Avalonia.Enums.Icon.Info);

        var result = await box.ShowWindowDialogAsync(this);
        return result == ButtonResult.Yes;
    }

    private async void OnImportClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Import Folder",
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            var folderPath = folders[0].Path.LocalPath;
            if (DataContext is MainWindowViewModel vm)
            {
                await vm.ImportFromFolderCommand.ExecuteAsync(folderPath);
            }
        }
    }
}

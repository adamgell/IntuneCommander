using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Intune.Commander.Desktop.Models;
using Intune.Commander.Desktop.ViewModels;
using SukiUI.Controls;

namespace Intune.Commander.Desktop.Views;

public partial class GroupPickerWindow : SukiWindow
{
    public GroupPickerWindow()
    {
        InitializeComponent();
    }

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is GroupPickerViewModel vm && vm.SearchGroupsCommand.CanExecute(null))
        {
            vm.SearchGroupsCommand.Execute(null);
        }
    }

    private void OnAddGroupClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is GroupSelectionItem item && DataContext is GroupPickerViewModel vm)
            vm.AddGroupCommand.Execute(item);
    }

    private void OnRemoveGroupClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is GroupSelectionItem item && DataContext is GroupPickerViewModel vm)
            vm.RemoveGroupCommand.Execute(item);
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}

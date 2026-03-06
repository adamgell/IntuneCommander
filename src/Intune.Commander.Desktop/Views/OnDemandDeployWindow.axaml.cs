using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Intune.Commander.Desktop.ViewModels;
using Microsoft.Graph.Beta.Models;
using SukiUI.Controls;

namespace Intune.Commander.Desktop.Views;

public partial class OnDemandDeployWindow : SukiWindow
{
    public OnDemandDeployWindow()
    {
        InitializeComponent();
    }

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is OnDemandDeployViewModel vm && vm.SearchDevicesCommand.CanExecute(null))
        {
            vm.SearchDevicesCommand.Execute(null);
        }
    }

    private void OnAddDeviceClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is ManagedDevice device && DataContext is OnDemandDeployViewModel vm)
            vm.AddDeviceCommand.Execute(device);
    }

    private void OnRemoveDeviceClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is ManagedDevice device && DataContext is OnDemandDeployViewModel vm)
            vm.RemoveDeviceCommand.Execute(device);
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (DataContext is OnDemandDeployViewModel vm)
        {
            vm.StopMonitoringCommand.Execute(null);
        }

        base.OnClosing(e);
    }
}

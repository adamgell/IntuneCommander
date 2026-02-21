using System.Collections.Generic;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Intune.Commander.Desktop.ViewModels;

namespace Intune.Commander.Desktop.Views;

public partial class GroupLookupWindow : Window
{
    private readonly Dictionary<string, Button> _filterButtons = new();
    private static readonly IBrush ActiveBrush = new SolidColorBrush(Color.Parse("#3366FF"), 0.45);
    private static readonly IBrush InactiveBrush = Brushes.Transparent;

    public GroupLookupWindow()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // Map category keys to their buttons
        var configBtn = this.FindControl<Button>("FilterConfigBtn");
        var complianceBtn = this.FindControl<Button>("FilterComplianceBtn");
        var appBtn = this.FindControl<Button>("FilterAppBtn");
        var allBtn = this.FindControl<Button>("FilterAllBtn");

        if (configBtn != null) _filterButtons["Device Configuration"] = configBtn;
        if (complianceBtn != null) _filterButtons["Compliance Policy"] = complianceBtn;
        if (appBtn != null) _filterButtons["Application"] = appBtn;
        if (allBtn != null) _filterButtons[""] = allBtn; // empty = show all

        if (DataContext is GroupLookupViewModel vm)
        {
            vm.PropertyChanged += OnVmPropertyChanged;
            UpdateFilterButtonStyles(vm.ActiveFilter);
        }
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GroupLookupViewModel.ActiveFilter) && sender is GroupLookupViewModel vm)
        {
            UpdateFilterButtonStyles(vm.ActiveFilter);
        }
    }

    private void UpdateFilterButtonStyles(string? activeFilter)
    {
        foreach (var (key, btn) in _filterButtons)
        {
            var isActive = string.IsNullOrEmpty(activeFilter)
                ? key == ""       // "Total" button active when no filter
                : key == activeFilter;

            btn.Background = isActive ? ActiveBrush : InactiveBrush;
            btn.FontWeight = isActive ? FontWeight.Bold : FontWeight.Normal;
        }
    }

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is GroupLookupViewModel vm && vm.SearchGroupsCommand.CanExecute(null))
        {
            vm.SearchGroupsCommand.Execute(null);
        }
    }
}

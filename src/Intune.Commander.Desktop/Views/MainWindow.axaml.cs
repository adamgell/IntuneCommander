using System;
using System.Collections.ObjectModel;
using System.IO;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Avalonia.Controls.Templates;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Intune.Commander.Core.Models;
using Intune.Commander.Desktop.Converters;
using Intune.Commander.Desktop.Models;

using Intune.Commander.Desktop.ViewModels;
using SukiUI.MessageBox;
using SukiUI.Controls;

namespace Intune.Commander.Desktop.Views;

public partial class MainWindow : SukiWindow
{
    private DataGrid? _mainDataGrid;
    private MainWindowViewModel? _vm;
    private bool _pendingGridRebuild;
    private CheckBox? _headerCheckBox;
    private bool _suppressHeaderCheckBoxSync;

    /// <summary>
    /// Returns the DataGrid, performing a lazy visual-tree search on first
    /// call (or after it becomes null). The grid lives inside a UserControl
    /// whose template is not stamped until the connected DockPanel first
    /// becomes visible, so we cannot find it reliably during OnLoaded.
    /// </summary>
    private DataGrid? GetMainDataGrid()
    {
        if (_mainDataGrid != null) return _mainDataGrid;
        _mainDataGrid = this.GetVisualDescendants()
                            .OfType<DataGrid>()
                            .FirstOrDefault(g => g.Name == "MainDataGrid");
        return _mainDataGrid;
    }
    private DebugLogWindow? _debugLogWindow;
    private PermissionsWindow? _permissionsWindow;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        // Ensure all child windows are closed and the process exits cleanly.
        // SukiUI may throw during shutdown when its SukiEffect disposes —
        // this is a known framework issue and is harmless at exit time.
        try
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.Shutdown();
        }
        catch (InvalidOperationException)
        {
            // SukiEffect.EnsureDisposed() can throw if effects are already
            // disposed during the shutdown sequence. Safe to ignore.
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // Named controls now live inside UserControl child name-scopes.
        // We use GetVisualDescendants() to cross those boundaries. The
        // DataGrid search is deferred to GetMainDataGrid() because the
        // connected panel's template isn't stamped until IsConnected=true.

        var importMenuItem = this.GetVisualDescendants()
                                 .OfType<MenuItem>()
                                 .FirstOrDefault(m => m.Name == "ImportMenuItem");
        if (importMenuItem != null)
            importMenuItem.Click += OnImportClick;

        var groupLookupButton = this.GetVisualDescendants()
                                    .OfType<Button>()
                                    .FirstOrDefault(b => b.Name == "GroupLookupButton");
        if (groupLookupButton != null)
            groupLookupButton.Click += OnGroupLookupClick;

        var columnChooserButton = this.GetVisualDescendants()
                                      .OfType<Button>()
                                      .FirstOrDefault(b => b.Name == "ColumnChooserButton");
        if (columnChooserButton != null)
            columnChooserButton.Click += OnColumnChooserClick;

        AttachViewModelIfAvailable("Loaded");
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        // Ctrl+Shift+L — open debug log (F12 is reserved for Avalonia DevTools)
        if (e.Key == Key.L && e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift))
        {
            OnDebugLogLinkPressed(this, null!);
            e.Handled = true;
        }
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        AttachViewModelIfAvailable("DataContextChanged");
    }

    private void AttachViewModelIfAvailable(string reason)
    {
        if (_vm != null)
        {
            _vm.SwitchProfileRequested -= OnSwitchProfileRequested;
            _vm.CopyDetailsRequested -= OnCopyDetailsRequested;
            _vm.ViewRawJsonRequested -= OnViewRawJsonRequested;
            _vm.SaveFileRequested -= OnSaveFileRequested;
            _vm.OpenAfterExportRequested -= OnOpenAfterExportRequested;
            _vm.OpenOnDemandDeployRequested = null;
            _vm.PropertyChanged -= OnViewModelPropertyChanged;
            _vm = null;
        }

        if (DataContext is MainWindowViewModel vm)
        {
            _vm = vm;
            vm.SwitchProfileRequested += OnSwitchProfileRequested;
            vm.CopyDetailsRequested += OnCopyDetailsRequested;
            vm.ViewRawJsonRequested += OnViewRawJsonRequested;
            vm.SaveFileRequested += OnSaveFileRequested;
            vm.OpenAfterExportRequested += OnOpenAfterExportRequested;
            vm.OpenOnDemandDeployRequested = OnOpenOnDemandDeployRequested;
            vm.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainWindowViewModel.ActiveColumns)
            or nameof(MainWindowViewModel.IsDeviceConfigCategory)
            or nameof(MainWindowViewModel.IsCompliancePolicyCategory)
            or nameof(MainWindowViewModel.IsApplicationCategory)
            or nameof(MainWindowViewModel.IsAppAssignmentsCategory)
            or nameof(MainWindowViewModel.IsSettingsCatalogCategory)
            or nameof(MainWindowViewModel.IsEndpointSecurityCategory)
            or nameof(MainWindowViewModel.IsAdministrativeTemplatesCategory)
            or nameof(MainWindowViewModel.IsEnrollmentConfigurationsCategory)
            or nameof(MainWindowViewModel.IsAppProtectionPoliciesCategory)
            or nameof(MainWindowViewModel.IsManagedDeviceAppConfigurationsCategory)
            or nameof(MainWindowViewModel.IsTargetedManagedAppConfigurationsCategory)
            or nameof(MainWindowViewModel.IsTermsAndConditionsCategory)
            or nameof(MainWindowViewModel.IsScopeTagsCategory)
            or nameof(MainWindowViewModel.IsRoleDefinitionsCategory)
            or nameof(MainWindowViewModel.IsIntuneBrandingCategory)
            or nameof(MainWindowViewModel.IsAzureBrandingCategory)
            or nameof(MainWindowViewModel.IsConditionalAccessCategory)
            or nameof(MainWindowViewModel.IsAssignmentFiltersCategory)
            or nameof(MainWindowViewModel.IsPolicySetsCategory)
            or nameof(MainWindowViewModel.IsAutopilotProfilesCategory)
            or nameof(MainWindowViewModel.IsDeviceHealthScriptsCategory)
            or nameof(MainWindowViewModel.IsMacCustomAttributesCategory)
            or nameof(MainWindowViewModel.IsFeatureUpdatesCategory)
            or nameof(MainWindowViewModel.IsNamedLocationsCategory)
            or nameof(MainWindowViewModel.IsAuthenticationStrengthsCategory)
            or nameof(MainWindowViewModel.IsAuthenticationContextsCategory)
            or nameof(MainWindowViewModel.IsTermsOfUseCategory)
            or nameof(MainWindowViewModel.IsDeviceManagementScriptsCategory)
            or nameof(MainWindowViewModel.IsDeviceShellScriptsCategory)
            or nameof(MainWindowViewModel.IsComplianceScriptsCategory)
            or nameof(MainWindowViewModel.IsAppleDepCategory)
            or nameof(MainWindowViewModel.IsDeviceCategoriesCategory)
            or nameof(MainWindowViewModel.IsQualityUpdatesCategory)
            or nameof(MainWindowViewModel.IsDriverUpdatesCategory)
            or nameof(MainWindowViewModel.IsAdmxFilesCategory)
            or nameof(MainWindowViewModel.IsReusablePolicySettingsCategory)
            or nameof(MainWindowViewModel.IsNotificationTemplatesCategory)
            or nameof(MainWindowViewModel.IsDynamicGroupsCategory)
            or nameof(MainWindowViewModel.IsAssignedGroupsCategory)
            or nameof(MainWindowViewModel.IsVppTokensCategory)
            or nameof(MainWindowViewModel.IsCloudPcProvisioningCategory)
            or nameof(MainWindowViewModel.IsCloudPcUserSettingsCategory)
            or nameof(MainWindowViewModel.IsRoleAssignmentsCategory)
            or nameof(MainWindowViewModel.IsOverviewCategory))
        {
            // Multiple category properties fire in sequence during a single
            // category change.  Defer the rebuild to avoid repeated
            // clear-and-bind cycles that can momentarily pair new data with
            // old columns (or vice-versa), producing binding errors.
            if (!_pendingGridRebuild)
            {
                _pendingGridRebuild = true;
                Dispatcher.UIThread.Post(() =>
                {
                    _pendingGridRebuild = false;
                    RebuildDataGridColumns();
                }, DispatcherPriority.Render);
            }
        }
        else if (e.PropertyName == nameof(MainWindowViewModel.CheckedItemCount))
        {
            SyncHeaderCheckBox();
        }
        else if (e.PropertyName == nameof(MainWindowViewModel.IsDetailPaneVisible))
        {
            UpdateDetailPaneRowHeight();
        }
        else if (e.PropertyName?.StartsWith("Filtered") == true)
        {
            _vm?.RefreshActiveItemsSource();
        }
    }

    private void RebuildDataGridColumns()
    {
        var grid = GetMainDataGrid();
        if (grid == null || _vm == null) return;

        grid.Columns.Clear();
        var columns = _vm.ActiveColumns;
        if (columns == null) return;

        // Add checkbox column for multi-select (skip for Overview)
        if (!_vm.IsOverviewCategory)
        {
            _headerCheckBox = new CheckBox
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            _headerCheckBox.IsCheckedChanged += (_, _) =>
            {
                if (_suppressHeaderCheckBoxSync) return;
                if (_headerCheckBox.IsChecked == true)
                    _vm.SelectAllCommand.Execute(null);
                else
                    _vm.DeselectAllCommand.Execute(null);
            };

            var checkColumn = new DataGridTemplateColumn
            {
                Header = _headerCheckBox,
                Width = new DataGridLength(40),
                CanUserResize = false,
                CanUserSort = false,
                CanUserReorder = false,
                CellTemplate = new FuncDataTemplate<SelectableItem>((_, _) =>
                {
                    var cb = new CheckBox
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                    cb.Bind(CheckBox.IsCheckedProperty, new Binding("IsSelected") { Mode = BindingMode.TwoWay });
                    return cb;
                })
            };
            grid.Columns.Add(checkColumn);
        }

        foreach (var col in columns)
        {
            if (!col.IsVisible) continue;

            IBinding binding;

            if (col.BindingPath == "Computed:ODataType")
            {
                binding = new Binding("Item.OdataType") { Converter = ODataTypeConverter.Instance };
            }
            else if (col.BindingPath == "Computed:Platform")
            {
                binding = new Binding("Item.OdataType") { Converter = PlatformConverter.Instance };
            }
            else if (col.BindingPath == "Computed:RoleScopeTags")
            {
                binding = new Binding("Item.RoleScopeTagIds") { Converter = StringListConverter.Instance };
            }
            else
            {
                binding = new Binding($"Item.{col.BindingPath}");
            }

            var dgCol = new DataGridTextColumn
            {
                Header = col.Header,
                Binding = binding,
                Width = col.IsStar
                    ? new DataGridLength(1, DataGridLengthUnitType.Star)
                    : new DataGridLength(col.Width)
            };

            grid.Columns.Add(dgCol);
        }
    }

    /// <summary>
    /// Syncs the header checkbox state with the current checked item count.
    /// Uses a guard flag to prevent feedback loops.
    /// </summary>
    private void SyncHeaderCheckBox()
    {
        if (_headerCheckBox == null || _vm == null) return;

        _suppressHeaderCheckBoxSync = true;
        try
        {
            var total = _vm.ActiveItemsSource.Count;
            var checkedCount = _vm.CheckedItemCount;

            if (checkedCount == 0)
            {
                _headerCheckBox.IsThreeState = false;
                _headerCheckBox.IsChecked = false;
            }
            else if (checkedCount == total)
            {
                _headerCheckBox.IsThreeState = false;
                _headerCheckBox.IsChecked = true;
            }
            else
            {
                // Temporarily enable three-state to allow indeterminate,
                // then disable so user clicks only toggle between checked/unchecked
                _headerCheckBox.IsThreeState = true;
                _headerCheckBox.IsChecked = null;
                _headerCheckBox.IsThreeState = false;
            }
        }
        finally
        {
            _suppressHeaderCheckBoxSync = false;
        }
    }

    private void UpdateDetailPaneRowHeight()
    {
        if (_vm == null) return;

        // Find the content Grid that has 3 RowDefinitions (ItemList, Splitter, DetailPane)
        var contentGrid = this.GetVisualDescendants()
            .OfType<Grid>()
            .FirstOrDefault(g => g.RowDefinitions.Count == 3);

        if (contentGrid == null) return;

        // The detail pane row is the third one (index 2), named "DetailPaneRow"
        var detailRow = contentGrid.RowDefinitions[2];

        if (_vm.IsDetailPaneVisible)
        {
            detailRow.Height = new GridLength(2, GridUnitType.Star);
            detailRow.MinHeight = 100;
        }
        else
        {
            detailRow.Height = new GridLength(0);
            detailRow.MinHeight = 0;
        }
    }

    // --- Column chooser flyout ---

    private void OnColumnChooserClick(object? sender, RoutedEventArgs e)
    {
        if (_vm?.ActiveColumns == null || sender is not Button btn) return;

        var popup = new Avalonia.Controls.Primitives.Popup
        {
            PlacementTarget = btn,
            Placement = PlacementMode.BottomEdgeAlignedRight,
            IsLightDismissEnabled = true
        };

        var stack = new StackPanel { Spacing = 4, Margin = new Thickness(8) };

        foreach (var col in _vm.ActiveColumns)
        {
            var cb = new CheckBox
            {
                Content = col.Header,
                IsChecked = col.IsVisible,
                Tag = col
            };
            cb.IsCheckedChanged += (_, _) =>
            {
                if (cb.Tag is DataGridColumnConfig config)
                {
                    config.IsVisible = cb.IsChecked == true;
                    RebuildDataGridColumns();
                }
            };
            stack.Children.Add(cb);
        }

        var popupBorder = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(4),
            Child = stack
        };
        popupBorder.Bind(Border.BackgroundProperty, this.GetResourceObservable("SystemControlBackgroundAltHighBrush"));
        popupBorder.Bind(Border.BorderBrushProperty, this.GetResourceObservable("SystemControlForegroundBaseMediumBrush"));
        popup.Child = popupBorder;

        // Avalonia popups need to be in the visual tree
        if (this.Content is Panel panel)
        {
            panel.Children.Add(popup);
            popup.Open();
            popup.Closed += (_, _) =>
            {
                panel.Children.Remove(popup);
            };
        }
    }

    private void OnAssignmentReportClick(object? sender, RoutedEventArgs e)
    {
        if (_vm == null) return;

        var reportVm = _vm.CreateAssignmentReportViewModel();
        if (reportVm == null) return;

        var window = new AssignmentReportWindow
        {
            DataContext = reportVm
        };
        window.Show(this);
    }

    private void OnGroupLookupClick(object? sender, RoutedEventArgs e)
    {
        if (_vm == null) return;

        var lookupVm = _vm.CreateGroupLookupViewModel();
        if (lookupVm == null) return;

        var window = new GroupLookupWindow
        {
            DataContext = lookupVm
        };
        window.Show(this); // non-modal so the user can still browse
    }

    private Task OnOpenOnDemandDeployRequested(Microsoft.Graph.Beta.Models.DeviceHealthScript script)
    {
        if (_vm == null) return Task.CompletedTask;

        var deployVm = _vm.CreateOnDemandDeployViewModel(script);
        if (deployVm == null) return Task.CompletedTask;

        var window = new OnDemandDeployWindow
        {
            DataContext = deployVm
        };
        window.Show(this);
        return Task.CompletedTask;
    }

    private async Task<bool> OnSwitchProfileRequested(TenantProfile target)
    {
        var result = await SukiMessageBox.ShowDialog(this, new SukiMessageBoxHost
        {
            Header = "Switch Profile",
            Content = $"Switch to \"{target.Name}\"?\nYou will be disconnected from the current tenant.",
            ActionButtonsPreset = SukiMessageBoxButtons.YesNo
        });
        return result is SukiMessageBoxResult.Yes;
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

    private async void OnCopyDetailsRequested(string text)
    {
        try
        {
            var clipboard = GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
                await clipboard.SetTextAsync(text);
        }
        catch { /* clipboard not available */ }
    }

    private void OnViewRawJsonRequested(string title, string json)
    {
        var window = new RawJsonWindow(title, json);
        window.Show(this);
    }

    private void OnDebugLogLinkPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_debugLogWindow == null || !_debugLogWindow.IsVisible)
        {
            _debugLogWindow = new DebugLogWindow();
            _debugLogWindow.Closed += (_, _) => _debugLogWindow = null;
            _debugLogWindow.Show();
        }
        else
        {
            _debugLogWindow.Activate();
        }
    }

    private void OnPermissionsButtonClick(object? sender, RoutedEventArgs e)
    {
        if (_permissionsWindow == null || !_permissionsWindow.IsVisible)
        {
            if (_vm == null) return;
            _permissionsWindow = new PermissionsWindow(_vm);
            _permissionsWindow.Closed += (_, _) => _permissionsWindow = null;
            _permissionsWindow.Show(this);
        }
        else
        {
            _permissionsWindow.Activate();
        }
    }

    private void OnCheckForUpdatesClick(object? sender, RoutedEventArgs e)
    {
        OpenUrl("https://github.com/adamgell/IntuneCommander/releases");
    }

    private async void OnAboutClick(object? sender, RoutedEventArgs e)
    {
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        var versionText = version != null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "dev";

        await SukiMessageBox.ShowDialog(this, new SukiMessageBoxHost
        {
            Header = "About",
            Content = $"Intune Commander {versionText}\n\n" +
                "A .NET 8 / Avalonia desktop app for managing\n" +
                "Microsoft Intune configurations across clouds.\n\n" +
                "https://github.com/adamgell/IntuneCommander",
            ActionButtonsPreset = SukiMessageBoxButtons.OK
        });
    }

    private void OnNavItemClick(object? sender, RoutedEventArgs e)
    {
        if (_vm == null) return;
        if (sender is Button { Tag: string categoryName })
            _vm.ActivateCategoryByName(categoryName);
    }

    private static void OpenUrl(string url)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                Process.Start("open", url);
            else
                Process.Start("xdg-open", url);
        }
        catch { /* best effort */ }
    }

    private async Task<bool> OnOpenAfterExportRequested(string filePath)
    {
        var result = await SukiMessageBox.ShowDialog(this, new SukiMessageBoxHost
        {
            Header = "Export Complete",
            Content = $"PowerPoint exported successfully.\n\nWould you like to open {Path.GetFileName(filePath)}?",
            ActionButtonsPreset = SukiMessageBoxButtons.YesNo
        });
        return result is SukiMessageBoxResult.Yes;
    }

    private async Task<string?> OnSaveFileRequested(string defaultFileName, string filter)
    {
        var topLevel = GetTopLevel(this);
        if (topLevel == null) return null;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save File",
            SuggestedFileName = defaultFileName,
            FileTypeChoices = filter.Contains("pptx")
                ? new[] { new FilePickerFileType("PowerPoint Presentation") { Patterns = new[] { "*.pptx" } } }
                : null
        });

        return file?.Path.LocalPath;
    }
}

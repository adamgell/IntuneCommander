using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Intune.Commander.Core.Models;
using Intune.Commander.Core.Services;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Desktop.ViewModels;

public partial class OnDemandDeployViewModel : ViewModelBase
{
    private readonly IDeviceService _deviceService;
    private readonly IDeviceHealthScriptService _healthScriptService;
    private readonly DeviceHealthScript _script;
    private readonly ObservableCollection<OnDemandDeploymentRecord> _globalDeployments;
    private CancellationTokenSource? _monitoringCts;

    [ObservableProperty] private string _searchQuery = "";
    [ObservableProperty] private string _statusText = "Search for devices to target";
    [ObservableProperty] private bool _isSearching;
    [ObservableProperty] private bool _isDeploying;
    [ObservableProperty] private bool _deploymentComplete;
    [ObservableProperty] private bool _isMonitoring;
    [ObservableProperty] private double _deployProgress;
    [ObservableProperty] private string _deployProgressText = "";
    [ObservableProperty] private string _monitoringStatusText = "";
    [ObservableProperty] private int _succeededCount;
    [ObservableProperty] private int _failedCount;

    public string ScriptName => _script.DisplayName ?? "Unknown Script";
    public ObservableCollection<ManagedDevice> SearchResults { get; } = [];
    public ObservableCollection<ManagedDevice> TargetDevices { get; } = [];
    public ObservableCollection<OnDemandDeploymentRecord> DeploymentResults { get; } = [];
    public ObservableCollection<DeviceHealthScriptDeviceState> MonitoringStates { get; } = [];

    public bool CanDeploy => TargetDevices.Count > 0 && !IsDeploying;

    public OnDemandDeployViewModel(
        IDeviceService deviceService,
        IDeviceHealthScriptService healthScriptService,
        DeviceHealthScript script,
        ObservableCollection<OnDemandDeploymentRecord> globalDeployments)
    {
        _deviceService = deviceService;
        _healthScriptService = healthScriptService;
        _script = script;
        _globalDeployments = globalDeployments;

        TargetDevices.CollectionChanged += (_, _) => OnPropertyChanged(nameof(CanDeploy));
    }

    [RelayCommand]
    private async Task SearchDevicesAsync(CancellationToken cancellationToken)
    {
        ClearError();
        IsSearching = true;
        SearchResults.Clear();

        try
        {
            StatusText = "Searching devices...";
            var devices = await _deviceService.SearchDevicesAsync(SearchQuery?.Trim() ?? "", cancellationToken);
            foreach (var d in devices)
                SearchResults.Add(d);
            StatusText = devices.Count == 0
                ? "No devices found"
                : $"Found {devices.Count} device(s)";
        }
        catch (Exception ex)
        {
            SetError($"Search failed: {ex.Message}");
            StatusText = "Search failed";
        }
        finally { IsSearching = false; }
    }

    [RelayCommand]
    private void AddDevice(ManagedDevice device)
    {
        if (device.Id != null && TargetDevices.All(d => d.Id != device.Id))
            TargetDevices.Add(device);
    }

    [RelayCommand]
    private void RemoveDevice(ManagedDevice device)
    {
        TargetDevices.Remove(device);
    }

    [RelayCommand]
    private async Task DeployToAllAsync(CancellationToken cancellationToken)
    {
        if (TargetDevices.Count == 0 || _script.Id == null) return;

        IsDeploying = true;
        DeploymentComplete = false;
        OnPropertyChanged(nameof(CanDeploy));
        DeploymentResults.Clear();
        DeployProgress = 0;
        SucceededCount = 0;
        FailedCount = 0;
        var total = TargetDevices.Count;
        var completed = 0;

        try
        {
            foreach (var device in TargetDevices.ToList())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var record = new OnDemandDeploymentRecord
                {
                    ScriptId = _script.Id!,
                    ScriptName = _script.DisplayName ?? "Unknown",
                    DeviceId = device.Id ?? "",
                    DeviceName = device.DeviceName ?? "Unknown",
                    DispatchedAt = DateTimeOffset.UtcNow
                };

                try
                {
                    DeployProgressText = $"Deploying to {device.DeviceName} ({completed + 1}/{total})...";
                    await _healthScriptService.InitiateOnDemandRemediationAsync(
                        device.Id!, _script.Id!, cancellationToken);
                    record.Succeeded = true;
                    SucceededCount++;
                }
                catch (Exception ex)
                {
                    record.Succeeded = false;
                    record.ErrorMessage = ex.Message;
                    FailedCount++;
                }

                DeploymentResults.Add(record);
                _globalDeployments.Add(record);
                completed++;
                DeployProgress = (double)completed / total * 100;
            }

            StatusText = $"Deployment complete: {SucceededCount}/{total} succeeded";
        }
        catch (OperationCanceledException)
        {
            StatusText = $"Deployment cancelled after {completed}/{total}";
        }
        finally
        {
            IsDeploying = false;
            DeploymentComplete = true;
            DeployProgressText = "";
            OnPropertyChanged(nameof(CanDeploy));
        }

        // Auto-start monitoring after deployment
        _ = StartMonitoringAsync();
    }

    [RelayCommand]
    private async Task StartMonitoringAsync()
    {
        if (_script.Id == null || IsMonitoring) return;

        StopMonitoringInternal();
        _monitoringCts = new CancellationTokenSource();
        var ct = _monitoringCts.Token;
        IsMonitoring = true;
        MonitoringStatusText = "Starting monitoring...";

        var targetDeviceIds = TargetDevices
            .Where(d => d.Id != null)
            .Select(d => d.Id!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        try
        {
            // Poll every 10 seconds for up to 5 minutes
            for (var i = 0; i < 30 && !ct.IsCancellationRequested; i++)
            {
                try
                {
                    MonitoringStatusText = "Refreshing device states...";
                    var refreshedAt = DateTime.Now;
                    var allStates = await _healthScriptService.GetDeviceRunStatesAsync(
                        _script.Id, ct);

                    // Filter to target devices only
                    var targetStates = allStates
                        .Where(s => s.ManagedDevice?.Id != null &&
                                    targetDeviceIds.Contains(s.ManagedDevice.Id))
                        .ToList();

                    MonitoringStates.Clear();
                    foreach (var state in targetStates)
                        MonitoringStates.Add(state);

                    // Update the "ago" text every second during the wait
                    for (var s = 0; s < 10 && !ct.IsCancellationRequested; s++)
                    {
                        var elapsed = (int)(DateTime.Now - refreshedAt).TotalSeconds;
                        var ago = elapsed switch
                        {
                            0 => "just now",
                            1 => "1 second ago",
                            _ => $"{elapsed} seconds ago"
                        };
                        MonitoringStatusText = $"Monitoring {targetStates.Count}/{targetDeviceIds.Count} devices — last refresh: {ago}";
                        await Task.Delay(TimeSpan.FromSeconds(1), ct);
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    MonitoringStatusText = $"Refresh failed: {ex.Message}";
                    await Task.Delay(TimeSpan.FromSeconds(10), ct);
                }
            }

            MonitoringStatusText = "Monitoring ended (timeout)";
        }
        catch (OperationCanceledException)
        {
            MonitoringStatusText = "Monitoring stopped";
        }
        finally
        {
            StopMonitoringInternal();
            IsMonitoring = false;
        }
    }

    [RelayCommand]
    private void StopMonitoring()
    {
        StopMonitoringInternal();
        IsMonitoring = false;
        MonitoringStatusText = "Monitoring stopped";
    }

    private void StopMonitoringInternal()
    {
        _monitoringCts?.Cancel();
        _monitoringCts?.Dispose();
        _monitoringCts = null;
    }
}

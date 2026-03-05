using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Desktop.ViewModels;

public partial class MainWindowViewModel
{
    /// <summary>
    /// Loads run summary and device run states for the currently selected DeviceHealthScript.
    /// </summary>
    private async Task LoadRunSummaryAndDeviceStatesAsync(string scriptId)
    {
        if (_deviceHealthScriptService == null) return;

        var version = ++_loadRunSummaryVersion;
        IsLoadingRunSummary = true;
        SelectedScriptRunSummary = null;
        SelectedScriptDeviceRunStates.Clear();

        try
        {
            var summaryTask = _deviceHealthScriptService.GetRunSummaryAsync(scriptId);
            var statesTask = _deviceHealthScriptService.GetDeviceRunStatesAsync(scriptId);

            await Task.WhenAll(summaryTask, statesTask);

            // Guard: selection may have changed while loading
            if (_loadRunSummaryVersion != version) return;

            SelectedScriptRunSummary = summaryTask.Result;

            foreach (var state in statesTask.Result)
                SelectedScriptDeviceRunStates.Add(state);
        }
        catch (Exception ex)
        {
            DebugLog.LogError($"Failed to load run summary/states: {FormatGraphError(ex)}", ex);
        }
        finally
        {
            // Only clear the loading indicator if this request is still the most recent
            if (_loadRunSummaryVersion == version)
                IsLoadingRunSummary = false;
        }
    }

    /// <summary>
    /// Creates an OnDemandDeployViewModel for the deploy dialog.
    /// </summary>
    public OnDemandDeployViewModel? CreateOnDemandDeployViewModel(DeviceHealthScript script)
    {
        if (_deviceHealthScriptService == null || _deviceService == null) return null;

        return new OnDemandDeployViewModel(
            _deviceService,
            _deviceHealthScriptService,
            script,
            OnDemandDeployments);
    }

    [RelayCommand]
    private async Task OpenOnDemandDeployAsync()
    {
        if (SelectedDeviceHealthScript == null) return;
        if (OpenOnDemandDeployRequested != null)
            await OpenOnDemandDeployRequested.Invoke(SelectedDeviceHealthScript);
    }
}

using System;
using System.Collections.ObjectModel;
using Avalonia.Threading;

namespace IntuneManager.Desktop.Services;

public sealed class DebugLogService
{
    private static readonly Lazy<DebugLogService> _instance = new(() => new DebugLogService());
    public static DebugLogService Instance => _instance.Value;

    public ObservableCollection<string> Entries { get; } = new();

    private const int MaxEntries = 2000;

    private DebugLogService() { }

    public void Log(string message)
    {
        var entry = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";

        if (Dispatcher.UIThread.CheckAccess())
        {
            AddEntry(entry);
        }
        else
        {
            Dispatcher.UIThread.Post(() => AddEntry(entry));
        }
    }

    public void Log(string category, string message)
    {
        Log($"[{category}] {message}");
    }

    public void LogError(string message, Exception? ex = null)
    {
        var entry = ex != null
            ? $"[{DateTime.Now:HH:mm:ss.fff}] ERROR: {message} — {ex.GetType().Name}: {ex.Message}"
            : $"[{DateTime.Now:HH:mm:ss.fff}] ERROR: {message}";

        if (Dispatcher.UIThread.CheckAccess())
        {
            AddEntry(entry);
        }
        else
        {
            Dispatcher.UIThread.Post(() => AddEntry(entry));
        }
    }

    public void Clear()
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            Entries.Clear();
        }
        else
        {
            Dispatcher.UIThread.Post(() => Entries.Clear());
        }
    }

    private void AddEntry(string entry)
    {
        Entries.Add(entry);
        while (Entries.Count > MaxEntries)
            Entries.RemoveAt(0);
    }
}

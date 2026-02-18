using System.Collections.Specialized;
using Avalonia.Controls;
using IntuneManager.Desktop.ViewModels;

namespace IntuneManager.Desktop.Views;

public partial class DebugLogWindow : Window
{
    public DebugLogWindow()
    {
        InitializeComponent();
        DataContext = new DebugLogViewModel();

        // Auto-scroll to bottom when new entries are added
        var listBox = this.FindControl<ListBox>("LogListBox");
        if (listBox != null)
        {
            var vm = (DebugLogViewModel)DataContext;
            vm.LogEntries.CollectionChanged += (_, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add && vm.LogEntries.Count > 0)
                {
                    listBox.ScrollIntoView(vm.LogEntries[^1]);
                }
            };
        }
    }
}

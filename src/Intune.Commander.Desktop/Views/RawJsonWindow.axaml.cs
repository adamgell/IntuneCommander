using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SukiUI.Controls;

namespace Intune.Commander.Desktop.Views;

public partial class RawJsonWindow : SukiWindow
{
    public RawJsonWindow()
    {
        InitializeComponent();
    }

    public RawJsonWindow(string itemTitle, string json) : this()
    {
        Title = $"Raw JSON — {itemTitle}";
        TitleText.Text = itemTitle;
        JsonTextBox.Text = json;
    }

    private async void OnCopyClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var clipboard = GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
                await clipboard.SetTextAsync(JsonTextBox.Text ?? "");

            CopyButton.Content = "✓ Copied!";
            await Task.Delay(1500);
            CopyButton.Content = "📋 Copy to Clipboard";
        }
        catch (Exception)
        {
            CopyButton.Content = "❌ Failed";
            await Task.Delay(1500);
            CopyButton.Content = "📋 Copy to Clipboard";
        }
    }
}

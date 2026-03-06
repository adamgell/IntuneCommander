using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;

namespace Intune.Commander.Desktop.Views.Controls;

public partial class PowerShellCodeBlock : UserControl
{
    private TextEditor? _editor;
    private bool _isReady;

    public static readonly StyledProperty<string> CodeProperty =
        AvaloniaProperty.Register<PowerShellCodeBlock, string>(nameof(Code), defaultValue: "");

    public string Code
    {
        get => GetValue(CodeProperty);
        set => SetValue(CodeProperty, value);
    }

    static PowerShellCodeBlock()
    {
        CodeProperty.Changed.AddClassHandler<PowerShellCodeBlock>(
            (block, _) => block.SyncText());
    }

    public PowerShellCodeBlock()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_isReady) return;

        _editor = this.FindControl<TextEditor>("Editor");
        if (_editor == null) return;

        // Install TextMate with DarkPlus theme for PowerShell syntax highlighting
        try
        {
            var registryOptions = new RegistryOptions(ThemeName.DarkPlus);
            var installation = _editor.InstallTextMate(registryOptions);

            var psLang = registryOptions.GetLanguageByExtension(".ps1");
            if (psLang != null)
                installation.SetGrammar(
                    registryOptions.GetScopeByLanguageId(psLang.Id));
        }
        catch (Exception)
        {
            // Syntax highlighting unavailable — editor still works for plain text
        }

        _isReady = true;
        SyncText();
    }

    private void SyncText()
    {
        if (!_isReady || _editor?.Document == null) return;
        var text = Code ?? "";
        if (_editor.Document.Text != text)
            _editor.Document.Text = text;
    }
}

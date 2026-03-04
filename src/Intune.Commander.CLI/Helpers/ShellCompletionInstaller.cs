namespace Intune.Commander.CLI.Helpers;

/// <summary>
/// Automatically installs shell tab-completion scripts on first run.
/// Supports zsh, bash, and fish. Uses a marker comment to prevent double-installation.
/// </summary>
public static class ShellCompletionInstaller
{
    private const string Marker = "# ic-completion-installed";

    public static void EnsureInstalled()
    {
        try
        {
            var shell = DetectShell();
            if (shell is null) return;

            var (rcFile, script) = shell switch
            {
                "zsh"  => (ZshRcFile(), ZshScript()),
                "bash" => (BashRcFile(), BashScript()),
                "fish" => (FishRcFile(), FishScript()),
                _      => (null, null)
            };

            if (rcFile is null || script is null) return;

            // Already installed?
            if (File.Exists(rcFile) && File.ReadAllText(rcFile).Contains(Marker))
                return;

            // Create parent directory if needed (fish config dir may not exist)
            Directory.CreateDirectory(Path.GetDirectoryName(rcFile)!);

            File.AppendAllText(rcFile, Environment.NewLine + script + Environment.NewLine);

            Console.Error.WriteLine($"[ic] Tab completion installed in {rcFile}");
            Console.Error.WriteLine($"[ic] Run 'source {rcFile}' or open a new terminal to enable it.");
        }
        catch
        {
            // Never break the normal command flow
        }
    }

    private static string? DetectShell()
    {
        var shellPath = Environment.GetEnvironmentVariable("SHELL");
        if (shellPath is null) return null;
        return Path.GetFileName(shellPath).ToLowerInvariant() switch
        {
            "zsh"  => "zsh",
            "bash" => "bash",
            "fish" => "fish",
            _      => null
        };
    }

    private static string ZshRcFile() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".zshrc");

    private static string BashRcFile() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".bashrc");

    private static string FishRcFile() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config", "fish", "completions", "ic.fish");

    private static string BinaryName()
    {
        var path = Environment.ProcessPath;
        return path is not null ? Path.GetFileNameWithoutExtension(path) : "ic";
    }

    private static string ZshScript()
    {
        var bin = BinaryName();
        return $$"""
            {{Marker}}
            _{{bin}}() {
                local -a completions
                IFS=$'\n' completions=("${(@f)$({{bin}} [complete] -- "${words[1,$CURRENT]}" 2>/dev/null)}")
                compadd -a completions
            }
            compdef _{{bin}} {{bin}}
            """;
    }

    private static string BashScript()
    {
        var bin = BinaryName();
        return $$"""
            {{Marker}}
            _{{bin}}_completions() {
                local completions
                completions=$({{bin}} [complete] -- "${COMP_LINE}" 2>/dev/null)
                COMPREPLY=($(compgen -W "${completions}" -- "${COMP_WORDS[COMP_CWORD]}"))
            }
            complete -F _{{bin}}_completions {{bin}}
            """;
    }

    private static string FishScript()
    {
        var bin = BinaryName();
        // Fish: one file per command in ~/.config/fish/completions/
        return $$"""
            {{Marker}}
            complete -c {{bin}} -f -a "({{bin}} [complete] -- (commandline -cp) 2>/dev/null)"
            """;
    }
}

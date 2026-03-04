using System.CommandLine;

namespace Intune.Commander.CLI.Commands;

public static class CompletionCommand
{
    public static Command Build()
    {
        var command = new Command("completion", "Output shell completion script for ic");

        var shellArg = new Argument<string>("shell", "Shell to generate completions for");
        shellArg.AddCompletions("zsh", "bash", "fish");
        command.AddArgument(shellArg);

        var installOption = new Option<bool>("--install", "Append the script to the appropriate shell rc file automatically");
        command.AddOption(installOption);

        command.SetHandler((string shell, bool install) =>
        {
            var binaryName = Path.GetFileNameWithoutExtension(
                Environment.ProcessPath ?? "ic");

            var script = shell.ToLowerInvariant() switch
            {
                "zsh" => $$"""
                    # ic zsh completion
                    # Add to ~/.zshrc:  source <(ic completion zsh)
                    _{{binaryName}}() {
                        local -a completions
                        IFS=$'\n' completions=("${(@f)$({{binaryName}} [complete] -- "${words[1,$CURRENT]}" 2>/dev/null)}")
                        compadd -a completions
                    }
                    compdef _{{binaryName}} {{binaryName}}
                    """,

                "bash" => $$"""
                    # ic bash completion
                    # Add to ~/.bashrc:  source <(ic completion bash)
                    _{{binaryName}}_completions() {
                        local completions
                        completions=$({{binaryName}} [complete] -- "${COMP_LINE}" 2>/dev/null)
                        COMPREPLY=($(compgen -W "${completions}" -- "${COMP_WORDS[COMP_CWORD]}"))
                    }
                    complete -F _{{binaryName}}_completions {{binaryName}}
                    """,

                "fish" => $$"""
                    # ic fish completion
                    # Add to ~/.config/fish/config.fish:  ic completion fish | source
                    complete -c {{binaryName}} -f -a "({{binaryName}} [complete] -- (commandline -cp) 2>/dev/null)"
                    """,

                _ => throw new InvalidOperationException($"Unsupported shell \"{shell}\". Supported: zsh, bash, fish")
            };

            if (install)
            {
                var rcFile = shell.ToLowerInvariant() switch
                {
                    "zsh"  => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".zshrc"),
                    "bash" => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".bashrc"),
                    "fish" => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "fish", "config.fish"),
                    _      => throw new InvalidOperationException($"Unsupported shell \"{shell}\"")
                };

                File.AppendAllText(rcFile, Environment.NewLine + script + Environment.NewLine);
                Console.Error.WriteLine($"Completion script appended to {rcFile}");
                Console.Error.WriteLine($"Reload your shell or run: source {rcFile}");
            }
            else
            {
                Console.WriteLine(script);
            }
        }, shellArg, installOption);

        return command;
    }
}

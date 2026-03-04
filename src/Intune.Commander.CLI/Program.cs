using Intune.Commander.CLI.Commands;
using Intune.Commander.CLI.Helpers;
using System.CommandLine;

// Auto-install shell completions on first run (no-op on subsequent runs; skipped during completion queries)
if (!args.Contains("[complete]"))
    ShellCompletionInstaller.EnsureInstalled();

var root = new RootCommand("Intune Commander CLI");
root.AddCommand(ExportCommand.Build());
root.AddCommand(ImportCommand.Build());
root.AddCommand(ListCommand.Build());
root.AddCommand(ProfileCommand.Build());
root.AddCommand(CompletionCommand.Build());

return await root.InvokeAsync(args);

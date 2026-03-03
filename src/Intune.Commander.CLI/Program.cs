using Intune.Commander.CLI.Commands;
using System.CommandLine;

var root = new RootCommand("Intune Commander CLI");
root.AddCommand(ExportCommand.Build());
root.AddCommand(ImportCommand.Build());
root.AddCommand(ListCommand.Build());
root.AddCommand(ProfileCommand.Build());

return await root.InvokeAsync(args);

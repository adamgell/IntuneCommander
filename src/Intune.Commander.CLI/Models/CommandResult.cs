namespace Intune.Commander.CLI.Models;

public sealed class CommandResult
{
    public required string Command { get; init; }
    public required int Count { get; init; }
    public required string Path { get; init; }
    public bool DryRun { get; init; }
}

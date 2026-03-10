using System.CommandLine;
using System.Text.Json;
using Intune.Commander.CLI.Commands;

namespace Intune.Commander.CLI.Tests;

[Collection("Console")]
public sealed class DiffCommandTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"ic-diff-cli-{Guid.NewGuid():N}");
    private readonly string _baselinePath;
    private readonly string _currentPath;

    public DiffCommandTests()
    {
        _baselinePath = Path.Combine(_root, "baseline");
        _currentPath = Path.Combine(_root, "current");
        Directory.CreateDirectory(_baselinePath);
        Directory.CreateDirectory(_currentPath);
    }

    [Fact]
    public async Task Build_JsonOutput_ReturnsDriftReport()
    {
        WritePolicy(_baselinePath, "CompliancePolicies", "PolicyA.json", """{ "displayName": "Old Name" }""");
        WritePolicy(_currentPath, "CompliancePolicies", "PolicyA.json", """{ "displayName": "New Name" }""");

        var result = await InvokeAsync("diff", "--baseline", _baselinePath, "--current", _currentPath, "--format", "json");

        Assert.Equal(0, result.ExitCode);
        Assert.True(string.IsNullOrWhiteSpace(result.StdErr));

        using var document = JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;
        Assert.True(root.GetProperty("driftDetected").GetBoolean());
        Assert.Equal(1, root.GetProperty("summary").GetProperty("low").GetInt32());
    }

    [Fact]
    public async Task Build_FailOnDrift_ReturnsNonZero()
    {
        WritePolicy(_baselinePath, "CompliancePolicies", "PolicyA.json", """{ "displayName": "Old Name" }""");
        WritePolicy(_currentPath, "CompliancePolicies", "PolicyA.json", """{ "displayName": "New Name" }""");

        var result = await InvokeAsync(
            "diff",
            "--baseline", _baselinePath,
            "--current", _currentPath,
            "--format", "json",
            "--fail-on-drift");

        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Build_MissingBaselinePath_FailsCleanly()
    {
        var result = await InvokeAsync("diff", "--baseline", Path.Combine(_root, "missing"), "--current", _currentPath);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Baseline directory not found", result.StdErr);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private static void WritePolicy(string root, string folder, string fileName, string json)
    {
        var path = Path.Combine(root, folder);
        Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, fileName), json);
    }

    private static async Task<CommandInvocationResult> InvokeAsync(params string[] args)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var originalOut = Console.Out;
        var originalErr = Console.Error;

        try
        {
            Console.SetOut(stdout);
            Console.SetError(stderr);

            var root = new RootCommand("test");
            root.AddCommand(DiffCommand.Build());

            var exitCode = await root.InvokeAsync(args);
            return new CommandInvocationResult(exitCode, stdout.ToString(), stderr.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalErr);
        }
    }

    private sealed record CommandInvocationResult(int ExitCode, string StdOut, string StdErr);
}

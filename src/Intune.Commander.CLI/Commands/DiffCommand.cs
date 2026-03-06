using Intune.Commander.CLI.Helpers;
using Intune.Commander.Core.Models;
using Intune.Commander.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.Text;
using System.Text.Json;

namespace Intune.Commander.CLI.Commands;

public static class DiffCommand
{
    public static Command Build()
    {
        var command = new Command("diff", "Compare two export directories and produce a drift report");

        var baseline = new Option<string>("--baseline", "Path to the baseline export directory") { IsRequired = true };
        var current = new Option<string>("--current", "Path to the current export directory") { IsRequired = true };
        var format = new Option<string>("--format", () => "json", "Output format: json, text, or markdown")
            .FromAmong("json", "text", "markdown");
        var output = new Option<string?>("--output", "Write report to a file instead of stdout");
        var minSeverity = new Option<string>("--min-severity", () => "low", "Minimum severity to include: low, medium, high, or critical")
            .FromAmong("low", "medium", "high", "critical");
        var failOnDrift = new Option<bool>("--fail-on-drift", "Exit with code 1 when drift is detected at or above min-severity");

        command.AddOption(baseline);
        command.AddOption(current);
        command.AddOption(format);
        command.AddOption(output);
        command.AddOption(minSeverity);
        command.AddOption(failOnDrift);

        command.SetHandler(async context =>
        {
            var exitCode = await ExecuteAsync(
                context.ParseResult.GetValueForOption(baseline)!,
                context.ParseResult.GetValueForOption(current)!,
                context.ParseResult.GetValueForOption(format) ?? "json",
                context.ParseResult.GetValueForOption(output),
                context.ParseResult.GetValueForOption(minSeverity) ?? "low",
                context.ParseResult.GetValueForOption(failOnDrift),
                context.GetCancellationToken());

            context.ExitCode = exitCode;
        });

        return command;
    }

    private static async Task<int> ExecuteAsync(
        string baselinePath,
        string currentPath,
        string format,
        string? outputPath,
        string minSeverityStr,
        bool failOnDrift,
        CancellationToken cancellationToken)
    {
        var severity = ParseSeverity(minSeverityStr);
        if (severity is null)
        {
            Console.Error.WriteLine($"Invalid --min-severity value: {minSeverityStr}. Expected: low, medium, high, critical.");
            return 1;
        }

        using var provider = CliServices.CreateServiceProvider();
        var detector = provider.GetRequiredService<IDriftDetectionService>();

        DriftReport report;
        try
        {
            report = await detector.CompareAsync(baselinePath, currentPath, severity.Value, cancellationToken: cancellationToken);
        }
        catch (DirectoryNotFoundException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"Failed to parse export JSON: {ex.Message}");
            return 1;
        }

        var normalizedFormat = format.ToLowerInvariant();
        string rendered;

        switch (normalizedFormat)
        {
            case "text":
                rendered = RenderText(report);
                break;
            case "markdown":
                rendered = RenderMarkdown(report);
                break;
            case "json":
                rendered = OutputFormatter.SerializeJson(report);
                break;
            default:
                Console.Error.WriteLine($"Invalid --format value: {format}. Expected: json, text, markdown.");
                return 1;
        }

        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            await File.WriteAllTextAsync(outputPath, rendered, cancellationToken);
            Console.Error.WriteLine($"Report written to {outputPath}");
        }
        else
        {
            Console.WriteLine(rendered);
        }

        return failOnDrift && report.DriftDetected ? 1 : 0;
    }

    internal static string RenderText(DriftReport report)
    {
        var lines = new List<string>
        {
            $"Drift detected: {report.DriftDetected}",
            $"Critical: {report.Summary.Critical}, High: {report.Summary.High}, Medium: {report.Summary.Medium}, Low: {report.Summary.Low}"
        };

        foreach (var change in report.Changes)
            lines.Add($"  [{change.Severity}] {change.ObjectType} '{change.Name}' {change.ChangeType}");

        return string.Join(Environment.NewLine, lines);
    }

    internal static string RenderMarkdown(DriftReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Intune Drift Report");
        sb.AppendLine();
        sb.AppendLine($"- Drift detected: **{report.DriftDetected}**");
        sb.AppendLine($"- Critical: **{report.Summary.Critical}**");
        sb.AppendLine($"- High: **{report.Summary.High}**");
        sb.AppendLine($"- Medium: **{report.Summary.Medium}**");
        sb.AppendLine($"- Low: **{report.Summary.Low}**");
        sb.AppendLine();
        sb.AppendLine("| Object Type | Name | Change | Severity |");
        sb.AppendLine("| --- | --- | --- | --- |");
        foreach (var change in report.Changes)
            sb.AppendLine($"| {change.ObjectType} | {change.Name} | {change.ChangeType} | {change.Severity} |");

        return sb.ToString();
    }

    private static DriftSeverity? ParseSeverity(string value) =>
        value.ToLowerInvariant() switch
        {
            "low" => DriftSeverity.Low,
            "medium" => DriftSeverity.Medium,
            "high" => DriftSeverity.High,
            "critical" => DriftSeverity.Critical,
            _ => null
        };
}

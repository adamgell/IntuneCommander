using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Intune.Commander.Core.Models;
using Intune.Commander.Core.Services;

const string UserAgentProduct = "IntuneCommander";

var exitCode = await RunAsync(args);
return exitCode;

static async Task<int> RunAsync(string[] args)
{
    if (args.Length == 0)
    {
        PrintUsage();
        return 1;
    }

    return args[0].ToLowerInvariant() switch
    {
        "export" => await HandleExportAsync(args[1..]),
        "diff" => await HandleDiffAsync(args[1..]),
        "alert" => await HandleAlertAsync(args[1..]),
        _ => 1
    };
}

static async Task<int> HandleExportAsync(string[] args)
{
    var outputPath = GetOptionValue(args, "--output") ?? Directory.GetCurrentDirectory();
    var normalize = HasFlag(args, "--normalize");

    if (!normalize)
    {
        Console.Error.WriteLine("The CLI currently supports only `ic export --normalize`.");
        return 1;
    }

    var normalizer = new ExportNormalizer();
    await normalizer.NormalizeDirectoryAsync(outputPath);
    Console.WriteLine($"Normalized export folder: {outputPath}");
    return 0;
}

static async Task<int> HandleDiffAsync(string[] args)
{
    var baseline = GetOptionValue(args, "--baseline");
    var current = GetOptionValue(args, "--current");

    if (string.IsNullOrWhiteSpace(baseline) || string.IsNullOrWhiteSpace(current))
    {
        Console.Error.WriteLine("Missing required options: --baseline and --current");
        return 1;
    }

    var format = (GetOptionValue(args, "--format") ?? "json").ToLowerInvariant();
    var outputPath = GetOptionValue(args, "--output");
    var minSeverity = ParseSeverity(GetOptionValue(args, "--min-severity")) ?? DriftSeverity.Low;
    var failOnDrift = HasFlag(args, "--fail-on-drift");

    var detector = new DriftDetectionService(new ExportNormalizer());
    var report = await detector.CompareAsync(baseline, current, minSeverity);

    var rendered = format switch
    {
        "text" => RenderText(report),
        "markdown" => RenderMarkdown(report),
        _ => RenderJson(report)
    };

    if (!string.IsNullOrWhiteSpace(outputPath))
        await File.WriteAllTextAsync(outputPath, rendered);
    else
        Console.WriteLine(rendered);

    return failOnDrift && report.DriftDetected ? 1 : 0;
}

static async Task<int> HandleAlertAsync(string[] args)
{
    if (args.Length == 0)
    {
        Console.Error.WriteLine("Missing alert provider. Expected: teams|slack|github|email");
        return 1;
    }

    var provider = args[0].ToLowerInvariant();
    var reportPath = GetOptionValue(args[1..], "--report");
    if (string.IsNullOrWhiteSpace(reportPath) || !File.Exists(reportPath))
    {
        Console.Error.WriteLine("Missing or invalid --report path.");
        return 1;
    }

    var reportJson = await File.ReadAllTextAsync(reportPath);
    var report = JsonSerializer.Deserialize<DriftReport>(reportJson, CreateJsonOptions());
    if (report is null)
    {
        Console.Error.WriteLine("Unable to read drift report.");
        return 1;
    }

    return provider switch
    {
        "teams" => await SendTeamsAlertAsync(report, args[1..]),
        "slack" => await SendSlackAlertAsync(report, args[1..]),
        "github" => await SendGitHubAlertAsync(report, args[1..]),
        "email" => SendEmailAlert(report, args[1..]),
        _ => 1
    };
}

static async Task<int> SendTeamsAlertAsync(DriftReport report, string[] args)
{
    var webhook = GetOptionValue(args, "--webhook");
    if (string.IsNullOrWhiteSpace(webhook))
    {
        Console.Error.WriteLine("Missing --webhook for teams alert.");
        return 1;
    }

    var payload = new
    {
        @type = "MessageCard",
        @context = "https://schema.org/extensions",
        summary = "Intune drift detected",
        themeColor = report.Summary.Critical > 0 ? "E81123" : "FF8C00",
        title = "Intune drift report",
        text = RenderMarkdown(report)
    };

    using var httpClient = new HttpClient();
    using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
    var response = await httpClient.PostAsync(webhook, content);
    return response.IsSuccessStatusCode ? 0 : 1;
}

static async Task<int> SendSlackAlertAsync(DriftReport report, string[] args)
{
    var webhook = GetOptionValue(args, "--webhook");
    if (string.IsNullOrWhiteSpace(webhook))
    {
        Console.Error.WriteLine("Missing --webhook for slack alert.");
        return 1;
    }

    var payload = new
    {
        text = RenderText(report)
    };

    using var httpClient = new HttpClient();
    using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
    var response = await httpClient.PostAsync(webhook, content);
    return response.IsSuccessStatusCode ? 0 : 1;
}

static async Task<int> SendGitHubAlertAsync(DriftReport report, string[] args)
{
    var repo = GetOptionValue(args, "--repo");
    var token = GetOptionValue(args, "--token");
    if (string.IsNullOrWhiteSpace(repo) || string.IsNullOrWhiteSpace(token))
    {
        Console.Error.WriteLine("Missing --repo or --token for github alert.");
        return 1;
    }

    var segments = repo.Split('/');
    if (segments.Length != 2)
    {
        Console.Error.WriteLine("Invalid --repo format. Expected owner/repo.");
        return 1;
    }

    var payload = new
    {
        title = $"Intune drift detected ({DateTimeOffset.UtcNow:yyyy-MM-dd})",
        body = RenderMarkdown(report)
    };

    using var httpClient = new HttpClient();
    var version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0";
    httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(UserAgentProduct, version));
    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

    var endpoint = $"https://api.github.com/repos/{segments[0]}/{segments[1]}/issues";
    using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
    var response = await httpClient.PostAsync(endpoint, content);
    return response.IsSuccessStatusCode ? 0 : 1;
}

static int SendEmailAlert(DriftReport report, string[] args)
{
    var recipient = GetOptionValue(args, "--to");
    if (string.IsNullOrWhiteSpace(recipient))
    {
        Console.Error.WriteLine("Missing --to for email alert.");
        return 1;
    }

    Console.WriteLine($"Email alert requested for {recipient}.");
    Console.WriteLine(RenderText(report));
    return 0;
}

static string RenderJson(DriftReport report) =>
    JsonSerializer.Serialize(report, CreateJsonOptions());

static JsonSerializerOptions CreateJsonOptions()
{
    var options = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    return options;
}

static string RenderText(DriftReport report)
{
    var lines = new List<string>
    {
        $"Drift detected: {report.DriftDetected}",
        $"Critical: {report.Summary.Critical}, High: {report.Summary.High}, Medium: {report.Summary.Medium}, Low: {report.Summary.Low}"
    };

    foreach (var change in report.Changes)
        lines.Add($"- [{change.Severity}] {change.ObjectType} '{change.Name}' {change.ChangeType}");

    return string.Join(Environment.NewLine, lines);
}

static string RenderMarkdown(DriftReport report)
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

static string? GetOptionValue(IReadOnlyList<string> args, string option)
{
    for (var i = 0; i < args.Count - 1; i++)
    {
        if (string.Equals(args[i], option, StringComparison.OrdinalIgnoreCase))
            return args[i + 1];
    }

    return null;
}

static bool HasFlag(IReadOnlyList<string> args, string option) =>
    args.Any(arg => string.Equals(arg, option, StringComparison.OrdinalIgnoreCase));

static DriftSeverity? ParseSeverity(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
        return null;

    return value.ToLowerInvariant() switch
    {
        "low" => DriftSeverity.Low,
        "medium" => DriftSeverity.Medium,
        "high" => DriftSeverity.High,
        "critical" => DriftSeverity.Critical,
        _ => null
    };
}

static void PrintUsage()
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  ic export --output <path> --normalize");
    Console.WriteLine("  ic diff --baseline <path> --current <path> [--format json|text|markdown] [--output <file>] [--min-severity low|medium|high|critical] [--fail-on-drift]");
    Console.WriteLine("  ic alert teams  --webhook <url> --report <file>");
    Console.WriteLine("  ic alert slack  --webhook <url> --report <file>");
    Console.WriteLine("  ic alert github --repo owner/repo --token <token> --report <file>");
    Console.WriteLine("  ic alert email  --to <address> --report <file>");
}

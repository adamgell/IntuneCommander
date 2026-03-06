using Intune.Commander.Core.Models;
using System.CommandLine;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Intune.Commander.CLI.Commands;

public static class AlertCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static Command Build()
    {
        var command = new Command("alert", "Send a drift report to an external notification channel");

        command.AddCommand(BuildTeamsCommand());
        command.AddCommand(BuildSlackCommand());
        command.AddCommand(BuildGitHubCommand());
        command.AddCommand(BuildEmailCommand());

        return command;
    }

    private static Command BuildTeamsCommand()
    {
        var command = new Command("teams", "Post drift report to a Microsoft Teams webhook");

        var report = new Option<string>("--report", "Path to drift report JSON") { IsRequired = true };
        var webhook = new Option<string>("--webhook", "Teams incoming webhook URL") { IsRequired = true };

        command.AddOption(report);
        command.AddOption(webhook);

        command.SetHandler(async context =>
        {
            context.ExitCode = await SendTeamsAsync(
                context.ParseResult.GetValueForOption(report)!,
                context.ParseResult.GetValueForOption(webhook)!,
                context.GetCancellationToken());
        });

        return command;
    }

    private static Command BuildSlackCommand()
    {
        var command = new Command("slack", "Post drift report to a Slack webhook");

        var report = new Option<string>("--report", "Path to drift report JSON") { IsRequired = true };
        var webhook = new Option<string>("--webhook", "Slack incoming webhook URL") { IsRequired = true };

        command.AddOption(report);
        command.AddOption(webhook);

        command.SetHandler(async context =>
        {
            context.ExitCode = await SendSlackAsync(
                context.ParseResult.GetValueForOption(report)!,
                context.ParseResult.GetValueForOption(webhook)!,
                context.GetCancellationToken());
        });

        return command;
    }

    private static Command BuildGitHubCommand()
    {
        var command = new Command("github", "Open a GitHub issue with the drift report");

        var report = new Option<string>("--report", "Path to drift report JSON") { IsRequired = true };
        var repo = new Option<string>("--repo", "GitHub repo in owner/repo format") { IsRequired = true };
        var token = new Option<string>("--token", "GitHub personal access token") { IsRequired = true };

        command.AddOption(report);
        command.AddOption(repo);
        command.AddOption(token);

        command.SetHandler(async context =>
        {
            context.ExitCode = await SendGitHubAsync(
                context.ParseResult.GetValueForOption(report)!,
                context.ParseResult.GetValueForOption(repo)!,
                context.ParseResult.GetValueForOption(token)!,
                context.GetCancellationToken());
        });

        return command;
    }

    private static Command BuildEmailCommand()
    {
        var command = new Command("email", "Print drift report for email delivery (placeholder)");

        var report = new Option<string>("--report", "Path to drift report JSON") { IsRequired = true };
        var to = new Option<string>("--to", "Recipient email address") { IsRequired = true };

        command.AddOption(report);
        command.AddOption(to);

        command.SetHandler(async context =>
        {
            context.ExitCode = await SendEmailAsync(
                context.ParseResult.GetValueForOption(report)!,
                context.ParseResult.GetValueForOption(to)!,
                context.GetCancellationToken());
        });

        return command;
    }

    private static async Task<DriftReport?> ReadReportAsync(string reportPath, CancellationToken cancellationToken)
    {
        if (!File.Exists(reportPath))
        {
            Console.Error.WriteLine($"Report file not found: {reportPath}");
            return null;
        }

        var json = await File.ReadAllTextAsync(reportPath, cancellationToken);
        return JsonSerializer.Deserialize<DriftReport>(json, JsonOptions);
    }

    private static async Task<int> SendTeamsAsync(string reportPath, string webhook, CancellationToken cancellationToken)
    {
        var report = await ReadReportAsync(reportPath, cancellationToken);
        if (report is null) return 1;

        var payload = new
        {
            @type = "MessageCard",
            @context = "https://schema.org/extensions",
            summary = "Intune drift detected",
            themeColor = report.Summary.Critical > 0 ? "E81123" : "FF8C00",
            title = "Intune Drift Report",
            text = DiffCommand.RenderMarkdown(report)
        };

        using var httpClient = new HttpClient();
        using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync(webhook, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
            Console.Error.WriteLine($"Teams webhook returned {response.StatusCode}");

        return response.IsSuccessStatusCode ? 0 : 1;
    }

    private static async Task<int> SendSlackAsync(string reportPath, string webhook, CancellationToken cancellationToken)
    {
        var report = await ReadReportAsync(reportPath, cancellationToken);
        if (report is null) return 1;

        var payload = new { text = DiffCommand.RenderText(report) };

        using var httpClient = new HttpClient();
        using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync(webhook, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
            Console.Error.WriteLine($"Slack webhook returned {response.StatusCode}");

        return response.IsSuccessStatusCode ? 0 : 1;
    }

    private static async Task<int> SendGitHubAsync(string reportPath, string repo, string token, CancellationToken cancellationToken)
    {
        var report = await ReadReportAsync(reportPath, cancellationToken);
        if (report is null) return 1;

        var segments = repo.Split('/');
        if (segments.Length != 2)
        {
            Console.Error.WriteLine("Invalid --repo format. Expected owner/repo.");
            return 1;
        }

        var payload = new
        {
            title = $"Intune drift detected ({DateTimeOffset.UtcNow:yyyy-MM-dd})",
            body = DiffCommand.RenderMarkdown(report)
        };

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("IntuneCommander", "1.0"));
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        var endpoint = $"https://api.github.com/repos/{segments[0]}/{segments[1]}/issues";
        using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync(endpoint, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
            Console.Error.WriteLine($"GitHub API returned {response.StatusCode}");

        return response.IsSuccessStatusCode ? 0 : 1;
    }

    private static async Task<int> SendEmailAsync(string reportPath, string to, CancellationToken cancellationToken)
    {
        var report = await ReadReportAsync(reportPath, cancellationToken);
        if (report is null) return 1;

        Console.Error.WriteLine($"Email alert requested for {to} (delivery not implemented — printing report to stdout).");
        Console.WriteLine(DiffCommand.RenderText(report));
        return 0;
    }
}

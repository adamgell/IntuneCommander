using Intune.Commander.CLI.Helpers;
using Intune.Commander.Core.Auth;
using Intune.Commander.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;

namespace Intune.Commander.CLI.Commands;

public static class ListCommand
{
    public static Command Build()
    {
        var command = new Command("list", "List supported Intune object types");

        var typeArgument = new Argument<string>("type");
        var profile = new Option<string?>("--profile");
        var tenantId = new Option<string?>("--tenant-id");
        var clientId = new Option<string?>("--client-id");
        var secret = new Option<string?>("--secret");
        var cloud = new Option<string?>("--cloud");
        var format = new Option<string>("--format", () => "table");

        command.AddArgument(typeArgument);
        command.AddOption(profile);
        command.AddOption(tenantId);
        command.AddOption(clientId);
        command.AddOption(secret);
        command.AddOption(cloud);
        command.AddOption(format);

        command.SetHandler(ExecuteAsync, typeArgument, profile, tenantId, clientId, secret, cloud, format);
        return command;
    }

    private static async Task ExecuteAsync(
        string type,
        string? profile,
        string? tenantId,
        string? clientId,
        string? secret,
        string? cloud,
        string format)
    {
        var cancellationToken = CancellationToken.None;
        using var provider = CliServices.CreateServiceProvider();
        var profileService = provider.GetRequiredService<ProfileService>();
        var graphClientFactory = provider.GetRequiredService<IntuneGraphClientFactory>();

        var resolvedProfile = await ProfileResolver.ResolveAsync(profileService, profile, tenantId, clientId, secret, cloud, cancellationToken);
        var graphClient = await graphClientFactory.CreateClientAsync(resolvedProfile, AuthHelper.DeviceCodeToStderr, cancellationToken);

        var normalizedType = type.Trim().ToLowerInvariant();
        object result = normalizedType switch
        {
            "configurations" => await new ConfigurationProfileService(graphClient).ListDeviceConfigurationsAsync(cancellationToken),
            "compliance" => await new CompliancePolicyService(graphClient).ListCompliancePoliciesAsync(cancellationToken),
            "applications" => await new ApplicationService(graphClient).ListApplicationsAsync(cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported list type \"{type}\". Supported: configurations, compliance, applications")
        };

        if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
        {
            OutputFormatter.WriteJsonToStdout(result);
            return;
        }

        if (!string.Equals(format, "table", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Format must be \"table\" or \"json\".");

        if (result is not System.Collections.IEnumerable enumerable)
        {
            OutputFormatter.WriteJsonToStdout(result);
            return;
        }

        var rows = new List<string[]>();
        foreach (var item in enumerable)
        {
            var itemType = item?.GetType();
            rows.Add([
                itemType?.GetProperty("DisplayName")?.GetValue(item)?.ToString() ?? string.Empty,
                itemType?.GetProperty("Id")?.GetValue(item)?.ToString() ?? string.Empty,
                itemType?.GetProperty("OdataType")?.GetValue(item)?.ToString() ?? string.Empty
            ]);
        }

        OutputFormatter.WriteTable(["DisplayName", "Id", "ODataType"], rows);
    }
}

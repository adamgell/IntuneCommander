using Intune.Commander.CLI.Helpers;
using Intune.Commander.Core.Auth;
using Intune.Commander.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;

namespace Intune.Commander.CLI.Commands;

public static class ProfileCommand
{
    public static Command Build()
    {
        var command = new Command("profile", "Profile management commands");

        var listCommand = new Command("list", "List saved profiles");
        listCommand.SetHandler(ListAsync);

        var testCommand = new Command("test", "Test a saved profile");
        var name = new Option<string>("--name") { IsRequired = true };
        testCommand.AddOption(name);
        testCommand.SetHandler(TestAsync, name);

        command.AddCommand(listCommand);
        command.AddCommand(testCommand);
        return command;
    }

    private static async Task ListAsync()
    {
        using var provider = CliServices.CreateServiceProvider();
        var profileService = provider.GetRequiredService<ProfileService>();

        await profileService.LoadAsync();
        OutputFormatter.WriteJsonToStdout(profileService.Profiles);
    }

    private static async Task TestAsync(string name)
    {
        var cancellationToken = CancellationToken.None;
        using var provider = CliServices.CreateServiceProvider();
        var profileService = provider.GetRequiredService<ProfileService>();
        var graphClientFactory = provider.GetRequiredService<IntuneGraphClientFactory>();

        var profile = await ProfileResolver.ResolveAsync(profileService, name, null, null, null, null, cancellationToken);
        var client = await graphClientFactory.CreateClientAsync(profile, AuthHelper.DeviceCodeToStderr, cancellationToken);

        await client.Organization.GetAsync(config => config.QueryParameters.Top = 1, cancellationToken);

        OutputFormatter.WriteJsonToStdout(new
        {
            profile = name,
            success = true
        });
    }
}

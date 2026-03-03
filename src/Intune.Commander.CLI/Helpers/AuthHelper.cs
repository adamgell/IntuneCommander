using Azure.Identity;

namespace Intune.Commander.CLI.Helpers;

public static class AuthHelper
{
    public static Task DeviceCodeToStderr(DeviceCodeInfo deviceCodeInfo, CancellationToken cancellationToken)
    {
        Console.Error.WriteLine(deviceCodeInfo.Message);
        return Task.CompletedTask;
    }
}

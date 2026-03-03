using System.Text.Json;

namespace Intune.Commander.CLI.Helpers;

public static class OutputFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static string SerializeJson(object value) => JsonSerializer.Serialize(value, JsonOptions);

    public static void WriteJsonToStdout(object value, TextWriter? writer = null) =>
        (writer ?? Console.Out).WriteLine(SerializeJson(value));

    public static void WriteTable(
        IEnumerable<string> headers,
        IEnumerable<IEnumerable<string?>> rows,
        TextWriter? writer = null)
    {
        var targetWriter = writer ?? Console.Out;
        targetWriter.WriteLine(string.Join('\t', headers));
        foreach (var row in rows)
            targetWriter.WriteLine(string.Join('\t', row.Select(c => c ?? string.Empty)));
    }
}

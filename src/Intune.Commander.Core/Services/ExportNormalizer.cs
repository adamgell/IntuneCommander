using System.Text.Json;
using System.Text.Json.Nodes;

namespace Intune.Commander.Core.Services;

public sealed class ExportNormalizer : IExportNormalizer
{
    private static readonly HashSet<string> VolatileFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "id",
        "createdDateTime",
        "lastModifiedDateTime",
        "version"
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task NormalizeDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

        foreach (var file in Directory.EnumerateFiles(directoryPath, "*.json", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.Equals(Path.GetFileName(file), "migration-table.json", StringComparison.OrdinalIgnoreCase))
                continue;

            var content = await File.ReadAllTextAsync(file, cancellationToken);
            var normalized = NormalizeJson(content);
            await File.WriteAllTextAsync(file, normalized, cancellationToken);
        }
    }

    public string NormalizeJson(string json)
    {
        var root = JsonNode.Parse(json) ?? throw new JsonException("JSON content is empty.");
        var normalized = NormalizeNode(root);
        return normalized.ToJsonString(JsonOptions);
    }

    private static JsonNode NormalizeNode(JsonNode node)
    {
        return node switch
        {
            JsonObject obj => NormalizeObject(obj),
            JsonArray arr => NormalizeArray(arr),
            _ => node.DeepClone()
        };
    }

    private static JsonObject NormalizeObject(JsonObject obj)
    {
        var normalized = new JsonObject();

        foreach (var property in obj
                     .Where(p => !VolatileFields.Contains(p.Key))
                     .OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            if (property.Value is null)
                normalized[property.Key] = null;
            else
                normalized[property.Key] = NormalizeNode(property.Value);
        }

        return normalized;
    }

    private static JsonArray NormalizeArray(JsonArray arr)
    {
        var normalizedItems = arr
            .Select(item => item == null ? null : NormalizeNode(item))
            .OrderBy(item => item?.ToJsonString() ?? string.Empty, StringComparer.Ordinal)
            .ToList();

        var normalized = new JsonArray();
        foreach (var item in normalizedItems)
            normalized.Add(item);

        return normalized;
    }
}

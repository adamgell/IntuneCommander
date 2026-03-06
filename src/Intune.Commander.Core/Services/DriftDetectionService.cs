using System.Text.Json;
using System.Text.Json.Nodes;
using Intune.Commander.Core.Models;

namespace Intune.Commander.Core.Services;

public sealed class DriftDetectionService(IExportNormalizer normalizer) : IDriftDetectionService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Represents a single export file with its resolved stable identity.
    /// </summary>
    private sealed record FileEntry(string RelativePath, string AbsolutePath, string? Id, string ObjectType);

    public async Task<DriftReport> CompareAsync(
        string baselinePath,
        string currentPath,
        DriftSeverity minSeverity = DriftSeverity.Low,
        IEnumerable<string>? objectTypes = null,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(baselinePath))
            throw new DirectoryNotFoundException($"Baseline directory not found: {baselinePath}");
        if (!Directory.Exists(currentPath))
            throw new DirectoryNotFoundException($"Current directory not found: {currentPath}");

        var filter = objectTypes?.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var baselineEntries = await BuildFileEntryMapAsync(baselinePath, filter, cancellationToken);
        var currentEntries = await BuildFileEntryMapAsync(currentPath, filter, cancellationToken);
        var allKeys = baselineEntries.Keys.Union(currentEntries.Keys, StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase);

        var changes = new List<DriftChange>();

        foreach (var key in allKeys)
        {
            cancellationToken.ThrowIfCancellationRequested();

            baselineEntries.TryGetValue(key, out var baselineEntry);
            currentEntries.TryGetValue(key, out var currentEntry);

            var representativeEntry = (currentEntry ?? baselineEntry)!;
            var objectType = representativeEntry.ObjectType;
            // Prefer current display name so renames show the new name
            var name = Path.GetFileNameWithoutExtension(representativeEntry.RelativePath);

            if (baselineEntry is null)
            {
                changes.Add(new DriftChange
                {
                    ObjectType = objectType,
                    Name = name,
                    ChangeType = "added",
                    Severity = DriftSeverity.Medium
                });
                continue;
            }

            if (currentEntry is null)
            {
                changes.Add(new DriftChange
                {
                    ObjectType = objectType,
                    Name = Path.GetFileNameWithoutExtension(baselineEntry.RelativePath),
                    ChangeType = "deleted",
                    Severity = DriftSeverity.Critical
                });
                continue;
            }

            var baselineJson = await File.ReadAllTextAsync(baselineEntry.AbsolutePath, cancellationToken);
            var currentJson = await File.ReadAllTextAsync(currentEntry.AbsolutePath, cancellationToken);

            var normalizedBaseline = normalizer.NormalizeJson(baselineJson);
            var normalizedCurrent = normalizer.NormalizeJson(currentJson);

            if (string.Equals(normalizedBaseline, normalizedCurrent, StringComparison.Ordinal))
                continue;

            var fieldChanges = GetFieldChanges(normalizedBaseline, normalizedCurrent);
            var severity = DetermineSeverity(fieldChanges);

            changes.Add(new DriftChange
            {
                ObjectType = objectType,
                Name = name,
                ChangeType = "modified",
                Severity = severity,
                Fields = fieldChanges
            });
        }

        var filteredChanges = changes
            .Where(c => c.Severity >= minSeverity)
            .ToList();

        return new DriftReport
        {
            DriftDetected = filteredChanges.Count > 0,
            Summary = new DriftSummary
            {
                Critical = filteredChanges.Count(c => c.Severity == DriftSeverity.Critical),
                High = filteredChanges.Count(c => c.Severity == DriftSeverity.High),
                Medium = filteredChanges.Count(c => c.Severity == DriftSeverity.Medium),
                Low = filteredChanges.Count(c => c.Severity == DriftSeverity.Low)
            },
            Changes = filteredChanges
        };
    }

    /// <summary>
    /// Builds a map of export files keyed by a stable identity.
    /// Files with an extractable <c>id</c> field are keyed as <c>{objectType}:{id}</c>
    /// so they can be matched across renames (DisplayName changes).
    /// Files without an <c>id</c> fall back to <c>path:{relativePath}</c>.
    /// </summary>
    private static async Task<Dictionary<string, FileEntry>> BuildFileEntryMapAsync(
        string root, HashSet<string>? objectTypesFilter, CancellationToken cancellationToken)
    {
        var files = Directory.EnumerateFiles(root, "*.json", SearchOption.AllDirectories)
            .Where(file => !string.Equals(Path.GetFileName(file), "migration-table.json", StringComparison.OrdinalIgnoreCase));

        var map = new Dictionary<string, FileEntry>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(root, file);
            var objectType = GetObjectType(relative);
            if (objectTypesFilter is not null && !objectTypesFilter.Contains(objectType))
                continue;

            var id = await TryExtractIdAsync(file, cancellationToken);
            // Use a stable id-based key when available so renamed files still match
            var stableKey = id is not null
                ? $"{objectType}:{id}"
                : $"path:{relative}";

            map[stableKey] = new FileEntry(relative, file, id, objectType);
        }

        return map;
    }

    /// <summary>
    /// Reads <paramref name="filePath"/> and returns the value of the <c>id</c> field,
    /// checking both root-level objects (e.g., DeviceConfiguration) and one level of
    /// nesting for export wrappers (e.g., <c>policy.id</c>, <c>application.id</c>).
    /// Returns <c>null</c> if no valid <c>id</c> can be extracted.
    /// </summary>
    private static async Task<string?> TryExtractIdAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            if (JsonNode.Parse(json) is not JsonObject root)
                return null;

            // Root-level id (DeviceConfigurations and similar direct exports)
            if (root["id"]?.GetValue<string>() is { Length: > 0 } rootId)
                return rootId;

            // One level of nesting for export wrappers: policy.id, application.id, script.id, template.id, etc.
            foreach (var prop in root)
            {
                if (prop.Value is JsonObject nested &&
                    nested["id"]?.GetValue<string>() is { Length: > 0 } nestedId)
                    return nestedId;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch { /* Ignore malformed JSON or I/O errors */ }

        return null;
    }

    private static string GetObjectType(string relativePath)
    {
        var firstSegment = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
        return firstSegment switch
        {
            "CompliancePolicies" => "CompliancePolicy",
            "DeviceConfigurations" => "DeviceConfiguration",
            "Applications" => "Application",
            _ => firstSegment
        };
    }

    private static List<DriftFieldChange> GetFieldChanges(string baselineJson, string currentJson)
    {
        var baselineNode = JsonNode.Parse(baselineJson);
        var currentNode = JsonNode.Parse(currentJson);
        var changes = new List<DriftFieldChange>();

        CompareNode(string.Empty, baselineNode, currentNode, changes);
        return changes;
    }

    private static void CompareNode(string path, JsonNode? baseline, JsonNode? current, List<DriftFieldChange> changes)
    {
        if (baseline is null && current is null)
            return;

        if (baseline is null || current is null)
        {
            changes.Add(new DriftFieldChange
            {
                Path = string.IsNullOrEmpty(path) ? "$" : path,
                Baseline = baseline is null ? null : ToObject(baseline),
                Current = current is null ? null : ToObject(current)
            });
            return;
        }

        if (baseline is JsonObject baselineObject && current is JsonObject currentObject)
        {
            var allKeys = baselineObject.Select(k => k.Key)
                .Union(currentObject.Select(k => k.Key), StringComparer.OrdinalIgnoreCase)
                .OrderBy(k => k, StringComparer.OrdinalIgnoreCase);

            foreach (var key in allKeys)
            {
                var childPath = string.IsNullOrEmpty(path) ? key : $"{path}.{key}";
                CompareNode(childPath, baselineObject[key], currentObject[key], changes);
            }

            return;
        }

        if (baseline is JsonArray baselineArray && current is JsonArray currentArray)
        {
            var max = Math.Max(baselineArray.Count, currentArray.Count);
            for (var i = 0; i < max; i++)
            {
                var childPath = $"{path}[{i}]";
                CompareNode(
                    childPath,
                    i < baselineArray.Count ? baselineArray[i] : null,
                    i < currentArray.Count ? currentArray[i] : null,
                    changes);
            }

            return;
        }

        var baselineSerialized = baseline.ToJsonString();
        var currentSerialized = current.ToJsonString();
        if (!string.Equals(baselineSerialized, currentSerialized, StringComparison.Ordinal))
        {
            changes.Add(new DriftFieldChange
            {
                Path = string.IsNullOrEmpty(path) ? "$" : path,
                Baseline = ToObject(baseline),
                Current = ToObject(current)
            });
        }
    }

    private static object? ToObject(JsonNode node) =>
        JsonSerializer.Deserialize<object>(node.ToJsonString(), JsonOptions);

    private static DriftSeverity DetermineSeverity(IEnumerable<DriftFieldChange> fieldChanges)
    {
        var maxSeverity = DriftSeverity.Low;
        foreach (var fieldChange in fieldChanges)
        {
            var severity = ClassifyFieldChange(fieldChange);
            if (severity > maxSeverity)
                maxSeverity = severity;
        }

        return maxSeverity;
    }

    private static DriftSeverity ClassifyFieldChange(DriftFieldChange fieldChange)
    {
        var path = fieldChange.Path.ToLowerInvariant();

        if (path.Contains("assignment"))
            return DriftSeverity.High;

        if (path.Contains("displayname") || path.Contains("description"))
            return DriftSeverity.Low;

        if (path.Contains("password") || path.Contains("mfa") || path.Contains("encryption") || path.Contains("bitlocker"))
            return DriftSeverity.Critical;

        if (path.Contains("isenabled") || path.EndsWith(".state", StringComparison.Ordinal))
        {
            var current = fieldChange.Current?.ToString();
            if (string.Equals(current, "reportOnly", StringComparison.OrdinalIgnoreCase))
                return DriftSeverity.High;
            if (string.Equals(current, "false", StringComparison.OrdinalIgnoreCase))
                return DriftSeverity.Critical;
        }

        return DriftSeverity.Medium;
    }
}

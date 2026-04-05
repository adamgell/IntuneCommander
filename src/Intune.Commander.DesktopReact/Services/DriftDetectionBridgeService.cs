using System.Text.Json;
using Intune.Commander.Core.Models;
using Intune.Commander.Core.Services;

namespace Intune.Commander.DesktopReact.Services;

public class DriftDetectionBridgeService(IDriftDetectionService driftService)
{
    public async Task<object> CompareAsync(JsonElement? payload)
    {
        if (payload is null)
            throw new ArgumentException("Payload is required");

        var p = payload.Value;
        var baselinePath = p.GetProperty("baselinePath").GetString()
            ?? throw new ArgumentException("baselinePath is required");
        var currentPath = p.GetProperty("currentPath").GetString()
            ?? throw new ArgumentException("currentPath is required");

        var minSeverity = DriftSeverity.Low;
        if (p.TryGetProperty("minSeverity", out var sevProp) && sevProp.ValueKind == JsonValueKind.String)
        {
            Enum.TryParse<DriftSeverity>(sevProp.GetString(), true, out minSeverity);
        }

        IEnumerable<string>? objectTypes = null;
        if (p.TryGetProperty("objectTypes", out var typesProp) && typesProp.ValueKind == JsonValueKind.Array)
        {
            objectTypes = typesProp.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.String)
                .Select(e => e.GetString()!)
                .ToList();
        }

        var report = await driftService.CompareAsync(baselinePath, currentPath, minSeverity, objectTypes);
        return report;
    }
}

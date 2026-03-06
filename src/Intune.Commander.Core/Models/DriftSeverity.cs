using System.Text.Json;
using System.Text.Json.Serialization;

namespace Intune.Commander.Core.Models;

[JsonConverter(typeof(DriftSeverityJsonConverter))]
public enum DriftSeverity
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

internal sealed class DriftSeverityJsonConverter : JsonStringEnumConverter<DriftSeverity>
{
    public DriftSeverityJsonConverter() : base(JsonNamingPolicy.CamelCase) { }
}

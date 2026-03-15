using System.Text.Json;
using System.Text.Json.Serialization;

namespace Intune.Commander.DesktopReact.Bridge;

public record BridgeCommand(
    [property: JsonPropertyName("protocol")] string Protocol,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("command")] string Command,
    [property: JsonPropertyName("payload")] JsonElement? Payload);

public record BridgeResponse(
    [property: JsonPropertyName("protocol")] string Protocol,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("payload")] object? Payload,
    [property: JsonPropertyName("error")] string? Error)
{
    public static BridgeResponse Ok(string id, object? payload) =>
        new("ic/1", id, "response", true, payload, null);

    public static BridgeResponse Fail(string id, string error) =>
        new("ic/1", id, "response", false, null, error);
}

public record BridgeEvent(
    [property: JsonPropertyName("protocol")] string Protocol,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("event")] string Event,
    [property: JsonPropertyName("payload")] object Payload)
{
    public static BridgeEvent Create(string eventName, object payload) =>
        new("ic/1", "event", eventName, payload);
}

namespace Intune.Commander.DesktopReact.Models;

public sealed record DeviceSearchResult(
    string Id,
    string DeviceName,
    string? OperatingSystem,
    string? OsVersion,
    string? Model,
    string? Manufacturer,
    string? LastSyncDateTime);

public sealed record DeploymentRecordDto(
    string DeviceId,
    string DeviceName,
    bool Succeeded,
    string? ErrorMessage,
    string DispatchedAt);

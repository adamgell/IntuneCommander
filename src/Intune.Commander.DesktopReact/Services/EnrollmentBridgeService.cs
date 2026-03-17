using System.Text.Json;
using Intune.Commander.Core.Services;
using Intune.Commander.DesktopReact.Models;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.DesktopReact.Services;

public class EnrollmentBridgeService
{
    private readonly AuthBridgeService _authBridge;
    private readonly ICacheService _cache;
    private readonly ShellStateBridgeService _shellState;
    private const string CacheKey = "EnrollmentConfigurations";
    private IEnrollmentConfigurationService? _service;

    public EnrollmentBridgeService(AuthBridgeService authBridge, ICacheService cache, ShellStateBridgeService shellState)
    {
        _authBridge = authBridge;
        _cache = cache;
        _shellState = shellState;
    }

    private IEnrollmentConfigurationService GetService()
    {
        var client = _authBridge.GraphClient ?? throw new InvalidOperationException("Not connected");
        _service ??= new EnrollmentConfigurationService(client);
        return _service;
    }

    public void Reset() => _service = null;
    private string? GetTenantId() => _shellState.ActiveProfile?.TenantId;

    public async Task<object> ListAsync()
    {
        var service = GetService();
        var tenantId = GetTenantId();

        if (tenantId is not null)
        {
            var cached = _cache.Get<DeviceEnrollmentConfiguration>(tenantId, CacheKey);
            if (cached is { Count: > 0 })
                return MapList(cached);
        }

        var configs = await service.ListEnrollmentConfigurationsAsync();
        if (tenantId is not null) _cache.Set(tenantId, CacheKey, configs);
        return MapList(configs);
    }

    private static EnrollmentConfigListItemDto[] MapList(List<DeviceEnrollmentConfiguration> configs)
    {
        return configs.Select(c => new EnrollmentConfigListItemDto(
            Id: c.Id ?? "",
            DisplayName: c.DisplayName ?? "",
            Description: c.Description,
            ConfigurationType: c.GetType().Name
                .Replace("DeviceEnrollment", "")
                .Replace("Configuration", ""),
            Priority: c.Priority ?? 0,
            CreatedDateTime: c.CreatedDateTime?.ToString("o") ?? "",
            LastModifiedDateTime: c.LastModifiedDateTime?.ToString("o") ?? ""
        )).ToArray();
    }

    public async Task<object> GetDetailAsync(JsonElement? payload)
    {
        if (payload is null || !payload.Value.TryGetProperty("id", out var idProp))
            throw new ArgumentException("ID is required");

        var id = idProp.GetString() ?? throw new ArgumentException("ID is required");
        var service = GetService();

        var config = await service.GetEnrollmentConfigurationAsync(id)
            ?? throw new InvalidOperationException($"Config {id} not found");

        var jsonOptions = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        return new EnrollmentConfigDetailDto(
            Id: config.Id ?? "",
            DisplayName: config.DisplayName ?? "",
            Description: config.Description,
            ConfigurationType: config.GetType().Name,
            Priority: config.Priority ?? 0,
            CreatedDateTime: config.CreatedDateTime?.ToString("o") ?? "",
            LastModifiedDateTime: config.LastModifiedDateTime?.ToString("o") ?? "",
            RoleScopeTagIds: (config.RoleScopeTagIds ?? []).ToArray(),
            RawJson: JsonSerializer.Serialize(config, jsonOptions));
    }
}

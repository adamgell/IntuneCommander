using System.Text.Json;
using Intune.Commander.Core.Services;
using Intune.Commander.DesktopReact.Models;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.DesktopReact.Services;

public class VppTokenBridgeService
{
    private readonly AuthBridgeService _authBridge;
    private readonly ICacheService _cache;
    private readonly ShellStateBridgeService _shellState;
    private const string CacheKeyTokens = "VppTokens";
    private const string CacheKeyDetail = "VppTokens_Detail";

    private IVppTokenService? _service;

    public VppTokenBridgeService(
        AuthBridgeService authBridge,
        ICacheService cache,
        ShellStateBridgeService shellState)
    {
        _authBridge = authBridge;
        _cache = cache;
        _shellState = shellState;
    }

    private IVppTokenService GetService()
    {
        var client = _authBridge.GraphClient ?? throw new InvalidOperationException("Not connected");
        _service ??= new VppTokenService(client);
        return _service;
    }

    public void Reset() => _service = null;
    private string? GetTenantId() => _shellState.ActiveProfile?.TenantId;

    public async Task<object> ListAsync()
    {
        var service = GetService();
        var tokens = await GroupResolutionHelper.GetCachedOrFetchAsync(
            _cache,
            GetTenantId(),
            CacheKeyTokens,
            () => service.ListVppTokensAsync());

        return tokens
            .Select(token => new VppTokenListItemDto(
                Id: token.Id ?? "",
                DisplayName: token.DisplayName ?? token.OrganizationName ?? "",
                OrganizationName: token.OrganizationName ?? "",
                AppleId: token.AppleId ?? "",
                State: token.State?.ToString() ?? "",
                ExpirationDateTime: token.ExpirationDateTime?.ToString("o") ?? "",
                LastSyncDateTime: token.LastSyncDateTime?.ToString("o") ?? ""))
            .ToArray();
    }

    public async Task<object> GetDetailAsync(JsonElement? payload)
    {
        if (payload is null || !payload.Value.TryGetProperty("id", out var idProp))
            throw new ArgumentException("ID is required");

        var id = idProp.GetString() ?? throw new ArgumentException("ID is required");
        var tenantId = GetTenantId();

        if (tenantId is not null)
        {
            var cached = _cache.GetSingle<VppTokenDetailDto>(tenantId, $"{CacheKeyDetail}_{id}");
            if (cached is not null)
                return cached;
        }

        var token = await GetService().GetVppTokenAsync(id)
            ?? throw new InvalidOperationException($"VPP token {id} not found");

        var detail = new VppTokenDetailDto(
            Id: token.Id ?? "",
            DisplayName: token.DisplayName ?? token.OrganizationName ?? "",
            OrganizationName: token.OrganizationName ?? "",
            AppleId: token.AppleId ?? "",
            State: token.State?.ToString() ?? "",
            ExpirationDateTime: token.ExpirationDateTime?.ToString("o") ?? "",
            VppTokenAccountType: token.VppTokenAccountType?.ToString() ?? "",
            LastSyncDateTime: token.LastSyncDateTime?.ToString("o") ?? "",
            LastSyncStatus: token.LastSyncStatus?.ToString() ?? "",
            CountryOrRegion: token.CountryOrRegion ?? "",
            LocationName: token.LocationName ?? "",
            AutomaticallyUpdateApps: token.AutomaticallyUpdateApps ?? false,
            RoleScopeTagIds: (token.RoleScopeTagIds ?? []).ToArray());

        if (tenantId is not null)
            _cache.SetSingle(tenantId, $"{CacheKeyDetail}_{id}", detail);

        return detail;
    }
}

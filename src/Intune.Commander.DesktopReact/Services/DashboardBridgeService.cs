using System.Text.Json;
using Intune.Commander.Core.Services;
using Intune.Commander.DesktopReact.Models;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.DesktopReact.Services;

public class DashboardBridgeService
{
    private readonly ICacheService _cache;
    private readonly ShellStateBridgeService _shellState;

    private const string CacheKeyDevices = "ManagedDevices";

    public DashboardBridgeService(
        ICacheService cache,
        ShellStateBridgeService shellState)
    {
        _cache = cache;
        _shellState = shellState;
    }

    private string? GetTenantId() => _shellState.ActiveProfile?.TenantId;

    public Task<object> GetComplianceSummaryAsync(JsonElement? payload)
    {
        var tenantId = GetTenantId();
        if (tenantId is null)
            return Task.FromResult<object>(new ComplianceSummaryDto(0, 0, 0, 0, 0));

        var cached = _cache.Get<ManagedDevice>(tenantId, CacheKeyDevices);
        if (cached is not { Count: > 0 })
            return Task.FromResult<object>(new ComplianceSummaryDto(0, 0, 0, 0, 0));

        int compliant = 0, nonCompliant = 0, inGrace = 0, unknown = 0;
        foreach (var device in cached)
        {
            switch (device.ComplianceState)
            {
                case ComplianceState.Compliant:
                    compliant++;
                    break;
                case ComplianceState.Noncompliant:
                    nonCompliant++;
                    break;
                case ComplianceState.InGracePeriod:
                    inGrace++;
                    break;
                default:
                    unknown++;
                    break;
            }
        }

        return Task.FromResult<object>(new ComplianceSummaryDto(
            compliant, nonCompliant, inGrace, unknown, cached.Count));
    }
}

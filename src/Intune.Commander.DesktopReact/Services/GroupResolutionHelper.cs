using Intune.Commander.Core.Services;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.DesktopReact.Services;

/// <summary>
/// Shared helper for resolving group names and mapping assignment targets,
/// used across multiple bridge services.
/// </summary>
public static class GroupResolutionHelper
{
    /// <summary>
    /// Resolves group IDs to display names using parallel Graph API calls.
    /// </summary>
    public static async Task<Dictionary<string, string>> ResolveGroupNamesAsync(
        IEnumerable<DeviceAndAppManagementAssignmentTarget?> targets,
        Microsoft.Graph.Beta.GraphServiceClient client)
    {
        var groupIds = targets.OfType<GroupAssignmentTarget>().Select(g => g.GroupId)
            .Concat(targets.OfType<ExclusionGroupAssignmentTarget>().Select(g => g.GroupId))
            .Where(id => id is not null)
            .Distinct()
            .ToList();

        var names = new Dictionary<string, string>();
        if (groupIds.Count == 0) return names;

        using var sem = new SemaphoreSlim(5);
        var tasks = groupIds.Select(async gid =>
        {
            await sem.WaitAsync();
            try
            {
                var g = await client.Groups[gid].GetAsync(r => r.QueryParameters.Select = ["displayName"]);
                if (g?.DisplayName is not null)
                    lock (names) names[gid!] = g.DisplayName;
            }
            catch { /* fallback to GUID */ }
            finally { sem.Release(); }
        });

        await Task.WhenAll(tasks);
        return names;
    }

    /// <summary>
    /// Fetches data from cache or calls the fetcher, storing the result in cache.
    /// </summary>
    public static async Task<List<T>> GetCachedOrFetchAsync<T>(
        ICacheService cache, string? tenantId, string cacheKey, Func<Task<List<T>>> fetcher) where T : class
    {
        if (tenantId is not null)
        {
            var cached = cache.Get<T>(tenantId, cacheKey);
            if (cached is { Count: > 0 })
                return cached;
        }

        var data = await fetcher();

        if (tenantId is not null)
            cache.Set(tenantId, cacheKey, data);

        return data;
    }
}

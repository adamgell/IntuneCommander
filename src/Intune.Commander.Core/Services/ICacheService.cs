namespace Intune.Commander.Core.Services;

/// <summary>
/// Provides encrypted, TTL-based caching of Graph API data per tenant.
/// Data is persisted to a LiteDB database on disk.
/// </summary>
public interface ICacheService : IDisposable
{
    /// <summary>
    /// Retrieves cached data for the given tenant and data type.
    /// Returns null if the cache entry is missing or expired.
    /// </summary>
    List<T>? Get<T>(string tenantId, string dataType);

    /// <summary>
    /// Stores data in the cache for the given tenant and data type.
    /// </summary>
    void Set<T>(string tenantId, string dataType, List<T> items, TimeSpan? ttl = null);

    /// <summary>
    /// Removes cached data for a specific tenant and optionally a specific data type.
    /// If dataType is null, removes all cached data for the tenant.
    /// </summary>
    void Invalidate(string tenantId, string? dataType = null);

    /// <summary>
    /// Removes all expired entries from the cache.
    /// </summary>
    int CleanupExpired();

    /// <summary>
    /// Gets cache entry metadata (age, item count) without deserializing the data.
    /// Returns null if entry is missing or expired.
    /// </summary>
    (DateTime CachedAt, int ItemCount)? GetMetadata(string tenantId, string dataType);

    // ─── Async wrappers (offload LiteDB I/O to the thread pool) ───────────

    /// <summary>
    /// Asynchronously retrieves cached data, offloading LiteDB disk I/O to the thread pool.
    /// </summary>
    Task<List<T>?> GetAsync<T>(string tenantId, string dataType);

    /// <summary>
    /// Asynchronously stores data in the cache, offloading LiteDB disk I/O to the thread pool.
    /// </summary>
    Task SetAsync<T>(string tenantId, string dataType, List<T> items, TimeSpan? ttl = null);

    /// <summary>
    /// Asynchronously removes cached data, offloading LiteDB disk I/O to the thread pool.
    /// </summary>
    Task InvalidateAsync(string tenantId, string? dataType = null);

    /// <summary>
    /// Asynchronously gets cache entry metadata, offloading LiteDB disk I/O to the thread pool.
    /// </summary>
    Task<(DateTime CachedAt, int ItemCount)?> GetMetadataAsync(string tenantId, string dataType);
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IntuneManager.Desktop.CategoryLoaders;

/// <summary>
/// Executes any <see cref="ICategoryLoader{T}"/> with standard boilerplate:
/// busy state, status messages, cache read/write, error reporting (via FormatError), and filter refresh.
///
/// Usage in MainWindowViewModel:
/// <code>
/// var items = await CategoryLoadHelper.ExecuteAsync(loader, _loadCtx, cancellationToken);
/// if (items != null)
/// {
///     MyCollection = new ObservableCollection&lt;T&gt;(items);
///     _myLoaded = true;
/// }
/// </code>
/// </summary>
public static class CategoryLoadHelper
{
    /// <summary>
    /// Runs the loader, applying cache and VM state management.
    /// Returns <c>null</c> if the load fails; callers must not set the
    /// <c>_*Loaded</c> flag in that case.
    /// </summary>
    public static async Task<IReadOnlyList<T>?> ExecuteAsync<T>(
        ICategoryLoader<T> loader,
        CategoryLoadContext ctx,
        CancellationToken cancellationToken = default)
    {
        ctx.SetBusy(true);
        ctx.SetStatus($"Loading {loader.CategoryName}...");

        try
        {
            // --- Cache read ---
            if (ctx.TenantId != null)
            {
                var cached = ctx.CacheService.Get<T>(ctx.TenantId, loader.CacheKey);
                if (cached is { Count: > 0 })
                {
                    ctx.ApplyFilter();
                    ctx.SetStatus($"Loaded {cached.Count} {loader.CategoryName.ToLowerInvariant()} from cache");
                    return cached;
                }
            }

            // --- Network fetch ---
            var items = await loader.FetchAsync(cancellationToken);

            // --- Cache write ---
            if (ctx.TenantId != null)
                ctx.CacheService.Set(ctx.TenantId, loader.CacheKey, [.. items]);

            ctx.ApplyFilter();
            ctx.SetStatus($"Loaded {items.Count} {loader.CategoryName.ToLowerInvariant()}");
            return items;
        }
        catch (Exception ex)
        {
            ctx.SetError($"Failed to load {loader.CategoryName}: {ctx.FormatError(ex)}");
            ctx.SetStatus($"Error loading {loader.CategoryName}");
            return null;
        }
        finally
        {
            ctx.SetBusy(false);
        }
    }
}

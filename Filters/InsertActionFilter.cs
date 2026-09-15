using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Gelato.Filters;

public class InsertActionFilter(
    GelatoManager manager,
    IUserManager userManager,
    ILibraryManager libraryManager,
    ILogger<InsertActionFilter> log
) : IAsyncActionFilter, IOrderedFilter
{
    private readonly KeyLock _lock = new();
    public int Order => 1;

    public async Task OnActionExecutionAsync(
        ActionExecutingContext ctx,
        ActionExecutionDelegate next
    )
    {
        if (
            !ctx.IsInsertableAction()
            || !ctx.TryGetRouteGuid(out var guid)
            || !ctx.TryGetUserId(out var userId)
            || userManager.GetUserById(userId) is not { } user
        )
        {
            await next();
            return;
        }

        // Jellyfin 12's web client makes the chosen version the page item when the version
        // dropdown changes (refreshSelectedVersion). A stream row has no metadata of its own and
        // is hidden from every list, so watch progress saved on it never shows up. Hand out the
        // movie/episode instead: the client keeps the chosen version selected as media source.
        if (
            ctx.GetActionName() is "GetItem" or "GetItemLegacy"
            && libraryManager.GetItemById(guid) is { } requested
            && requested.HasStreamTag()
            && manager.FindPrimaryForStream(requested, user) is { } primary
        )
        {
            ctx.ReplaceGuid(primary.Id);
            await next();
            return;
        }

        // Handle local (non-gelato) series: sync or clean tree on demand
        if (libraryManager.GetItemById(guid) is Series localSeries && !localSeries.IsGelato())
        {
            await HandleLocalSeriesAsync(userId, localSeries, ctx.HttpContext.RequestAborted);
            await next();
            return;
        }

        if (manager.GetStremioMeta(guid) is not { } stremioMeta)
        {
            await next();
            return;
        }

        // Get root folder
        var isSeries = stremioMeta.Type == StremioMediaType.Series;
        var root = isSeries
            ? manager.TryGetSeriesFolder(userId)
            : manager.TryGetMovieFolder(userId);
        if (root is null)
        {
            log.LogWarning("No {Type} folder configured", isSeries ? "Series" : "Movie");
            await next();
            return;
        }

        if (manager.IntoBaseItem(stremioMeta) is { } item)
        {
            var existing = manager.FindExistingItem(item, user);
            if (existing is not null)
            {
                log.LogInformation(
                    "Media already exists; redirecting to canonical id {Id}",
                    existing.Id
                );
                ctx.ReplaceGuid(existing.Id);
                await next();
                return;
            }
        }

        // Fetch full metadata. Try the IMDb id first for addons that key on it, then the
        // addon's own id: a tmdb-only meta addon rejects "tt…" ids outright (upstream #207).
        var cfg = GelatoPlugin.Instance!.GetConfig(userId);
        StremioMeta? meta = null;
        foreach (var lookupId in MetaLookupIds(stremioMeta.ImdbId, stremioMeta.Id))
        {
            meta = await cfg.Stremio.GetMetaAsync(lookupId, stremioMeta.Type);
            if (meta is not null)
                break;
        }
        if (meta is null)
        {
            log.LogError(
                "aio meta not found for {Id} {Type}, maybe try aiometadata as meta addon.",
                stremioMeta.Id,
                stremioMeta.Type
            );
            // The item only exists in Gelato's search cache; letting Jellyfin look the id up
            // yields a phantom the client retries forever. Answer plainly instead.
            ctx.Result = new NotFoundResult();
            return;
        }

        // Insert the item
        var baseItem = await InsertMetaAsync(guid, root, meta, user);
        if (baseItem is not null)
        {
            ctx.ReplaceGuid(baseItem.Id);
            manager.RemoveStremioMeta(guid);
        }

        await next();
    }

    /// <summary>
    /// Candidate ids for a metadata lookup, in the order to try them, without repeats.
    /// </summary>
    public static IReadOnlyList<string> MetaLookupIds(string? imdbId, string id)
    {
        var ids = new List<string>(2);
        if (!string.IsNullOrWhiteSpace(imdbId))
            ids.Add(imdbId);
        if (!string.IsNullOrWhiteSpace(id) && !ids.Contains(id))
            ids.Add(id);
        return ids;
    }

    private async Task HandleLocalSeriesAsync(Guid userId, Series series, CancellationToken ct)
    {
        var cfg = GelatoPlugin.Instance!.GetConfig(userId);

        if (cfg.ExtendLocalSeriesTrees)
        {
            var alreadySynced =
                series.Tags?.Contains(GelatoManager.TreeSyncedTag, StringComparer.OrdinalIgnoreCase)
                ?? false;
            if (alreadySynced)
                return;

            if (cfg.Stremio is not { } stremio)
                return;

            log.LogInformation(
                "InsertActionFilter: syncing local series tree for {Name} ({Id})",
                series.Name,
                series.Id
            );

            var meta = await stremio.GetMetaAsync(series).ConfigureAwait(false);
            if (meta is null)
                return;

            await manager
                .SyncSeriesTreesAsync(cfg, meta, ct, existingSeries: series)
                .ConfigureAwait(false);
        }
        else
        {
            // Setting disabled — clean any virtual items that may exist for this series
            manager.CleanVirtualTreeItem(series, ct);
        }
    }

    public async Task<BaseItem?> InsertMetaAsync(
        Guid guid,
        Folder root,
        StremioMeta meta,
        User user
    )
    {
        BaseItem? baseItem = null;
        var created = false;

        await _lock.RunQueuedAsync(
            guid,
            async ct =>
            {
                meta.Guid = guid;
                (baseItem, created) = await manager.InsertMeta(
                    root,
                    meta,
                    user,
                    false,
                    true,
                    meta.Type is StremioMediaType.Series,
                    ct
                );
            }
        );

        if (baseItem is not null && created)
            log.LogInformation("inserted new media: {Name}", baseItem.Name);

        return baseItem;
    }
}

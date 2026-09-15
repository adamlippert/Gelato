using Gelato.Decorators;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using NSubstitute;

namespace Gelato.Tests.Decorators;

/// <summary>
/// /Items/Counts (dashboard and library totals) is served by IItemCountService.GetItemCounts.
/// Gelato's stream-version rows are Movie/Episode items and must not be counted as titles.
/// </summary>
public class ItemCountServiceDecoratorTests
{
    [Fact]
    public void GetItemCounts_ExcludesStreamRows()
    {
        var (svc, inner) = Build();
        InternalItemsQuery? seen = null;
        inner.GetItemCounts(Arg.Do<InternalItemsQuery>(q => seen = q)).Returns(new ItemCounts());

        svc.GetItemCounts(new InternalItemsQuery());

        Assert.NotNull(seen);
        Assert.Contains(GelatoManager.StreamTag, seen!.ExcludeTags);
    }

    [Fact]
    public void GetCount_ExcludesStreamRows()
    {
        var (svc, inner) = Build();
        InternalItemsQuery? seen = null;
        inner.GetCount(Arg.Do<InternalItemsQuery>(q => seen = q)).Returns(0);

        svc.GetCount(new InternalItemsQuery { IncludeItemTypes = [BaseItemKind.Movie] });

        Assert.NotNull(seen);
        Assert.Contains(GelatoManager.StreamTag, seen!.ExcludeTags);
    }

    [Fact]
    public void GetItemCounts_LeavesStreamRowQueriesAlone()
    {
        var (svc, inner) = Build();
        InternalItemsQuery? seen = null;
        inner.GetItemCounts(Arg.Do<InternalItemsQuery>(q => seen = q)).Returns(new ItemCounts());

        svc.GetItemCounts(new InternalItemsQuery { Tags = [GelatoManager.StreamTag] });

        Assert.NotNull(seen);
        Assert.Empty(seen!.ExcludeTags);
    }

    [Fact]
    public void GetItemCounts_LeavesInternalLookupsAlone()
    {
        var (svc, inner) = Build();
        InternalItemsQuery? seen = null;
        inner.GetItemCounts(Arg.Do<InternalItemsQuery>(q => seen = q)).Returns(new ItemCounts());

        // IsDeadPerson = true is Gelato's marker for its own internal queries.
        svc.GetItemCounts(new InternalItemsQuery { IsDeadPerson = true });

        Assert.NotNull(seen);
        Assert.Empty(seen!.ExcludeTags);
    }

    private static (ItemCountServiceDecorator Svc, IItemCountService Inner) Build()
    {
        var inner = Substitute.For<IItemCountService>();
        var repo = Substitute.For<IItemRepository>();
        return (new ItemCountServiceDecorator(inner, repo), inner);
    }
}

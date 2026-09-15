using Gelato.Decorators;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using NSubstitute;

namespace Gelato.Tests.Decorators;

/// <summary>
/// The home screen's per-library "Recently Added" goes through IItemRepository.GetLatestItemList,
/// not GetItemList. Stream-version rows must be excluded there too, or every synced stream
/// shows up as a new title.
/// </summary>
public class GelatoItemRepositoryLatestTests
{
    [Fact]
    public void GetLatestItemList_ExcludesStreamRows_ForLatestMediaRequests()
    {
        var (repo, inner) = Build(actionName: "GetLatestMedia");
        InternalItemsQuery? seen = null;
        inner
            .GetLatestItemList(Arg.Do<InternalItemsQuery>(q => seen = q), Arg.Any<CollectionType>())
            .Returns([]);

        repo.GetLatestItemList(
            new InternalItemsQuery { IncludeItemTypes = [BaseItemKind.Movie] },
            CollectionType.movies
        );

        Assert.NotNull(seen);
        Assert.Contains(GelatoManager.StreamTag, seen!.ExcludeTags);
    }

    [Fact]
    public void GetLatestItemList_LeavesInternalCallsUntouched()
    {
        var (repo, inner) = Build(actionName: null);
        InternalItemsQuery? seen = null;
        inner
            .GetLatestItemList(Arg.Do<InternalItemsQuery>(q => seen = q), Arg.Any<CollectionType>())
            .Returns([]);

        repo.GetLatestItemList(
            new InternalItemsQuery { IncludeItemTypes = [BaseItemKind.Movie] },
            CollectionType.movies
        );

        Assert.NotNull(seen);
        Assert.Empty(seen!.ExcludeTags);
    }

    private static (GelatoItemRepository Repo, IItemRepository Inner) Build(string? actionName)
    {
        var inner = Substitute.For<IItemRepository>();
        var ctx = new DefaultHttpContext();
        if (actionName is not null)
        {
            var descriptor = new ControllerActionDescriptor
            {
                ActionName = actionName,
                ControllerName = "UserLibrary",
            };
            ctx.SetEndpoint(
                new Endpoint(null, new EndpointMetadataCollection(descriptor), actionName)
            );
        }
        var http = Substitute.For<IHttpContextAccessor>();
        http.HttpContext.Returns(ctx);
        return (new GelatoItemRepository(inner, http), inner);
    }
}

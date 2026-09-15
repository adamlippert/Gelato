using Gelato.Filters;

namespace Gelato.Tests.Filters;

/// <summary>
/// Upstream #207: the insert-on-click path looked up metadata by IMDb id whenever one was
/// present and never fell back to the addon's native id, so a tmdb-only meta addon 404'd
/// forever. The lookup must try every distinct candidate, IMDb first for compatibility.
/// </summary>
public class InsertActionFilterTests
{
    [Fact]
    public void MetaLookupIds_TriesImdbThenNativeId()
    {
        Assert.Equal(
            ["tt0111161", "tmdb:278"],
            InsertActionFilter.MetaLookupIds("tt0111161", "tmdb:278")
        );
    }

    [Fact]
    public void MetaLookupIds_WithoutImdb_UsesNativeIdOnly()
    {
        Assert.Equal(["tmdb:278"], InsertActionFilter.MetaLookupIds(null, "tmdb:278"));
        Assert.Equal(["tmdb:278"], InsertActionFilter.MetaLookupIds("   ", "tmdb:278"));
    }

    [Fact]
    public void MetaLookupIds_DoesNotRepeatWhenBothIdsAreTheSame()
    {
        Assert.Equal(["tt0111161"], InsertActionFilter.MetaLookupIds("tt0111161", "tt0111161"));
    }
}

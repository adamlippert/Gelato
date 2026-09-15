using Gelato.Filters;

namespace Gelato.Tests.Filters;

/// <summary>
/// Upstream #187: a client that sends MediaSourceId as the empty GUID made the filter call
/// GetItemById(Guid.Empty), which throws and turns every /Items/{id}/Download into a 400 —
/// including for local items Gelato has nothing to do with.
/// </summary>
public class DownloadFilterTests
{
    [Fact]
    public void TryGetMediaSourceId_ParsesARealGuid()
    {
        var id = Guid.NewGuid();

        Assert.True(DownloadFilter.TryGetMediaSourceId(id.ToString("N"), out var parsed));
        Assert.Equal(id, parsed);
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("00000000000000000000000000000000")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-guid")]
    [InlineData(null)]
    public void TryGetMediaSourceId_RejectsEmptyOrInvalid(string? raw)
    {
        Assert.False(DownloadFilter.TryGetMediaSourceId(raw, out var parsed));
        Assert.Equal(Guid.Empty, parsed);
    }
}

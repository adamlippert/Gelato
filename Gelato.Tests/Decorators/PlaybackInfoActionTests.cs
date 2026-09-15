using Gelato.Decorators;

namespace Gelato.Tests.Decorators;

/// <summary>
/// Upstream #168: the stream URL was only masked for the POST PlaybackInfo action; clients
/// using the GET variant received the raw URL. Both must count as PlaybackInfo.
/// </summary>
public class PlaybackInfoActionTests
{
    [Theory]
    [InlineData("GetPostedPlaybackInfo")]
    [InlineData("GetPlaybackInfo")]
    public void IsPlaybackInfoAction_True_ForBothVariants(string action)
    {
        Assert.True(MediaSourceManagerDecorator.IsPlaybackInfoAction(action));
    }

    [Theory]
    [InlineData("GetVideoStream")]
    [InlineData("GetItem")]
    [InlineData("")]
    [InlineData(null)]
    public void IsPlaybackInfoAction_False_Otherwise(string? action)
    {
        Assert.False(MediaSourceManagerDecorator.IsPlaybackInfoAction(action));
    }
}

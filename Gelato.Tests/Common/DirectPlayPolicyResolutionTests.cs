using Gelato.Config;
using Gelato.Services;

namespace Gelato.Tests.Common;

/// <summary>
/// The single decision both the PlaybackInfo mask and the item-DTO mask consult: may this
/// user receive this path for direct play? Global flag, per-user override and the URL
/// policy all feed in.
/// </summary>
public class DirectPlayPolicyResolutionTests
{
    private const string Remote = "https://cdn.torbox.app/dl/abc/movie.mkv";

    [Fact]
    public void IsAllowed_False_WhenGloballyOff()
    {
        var cfg = new PluginConfiguration { DirectPlay = false };
        Assert.False(DirectPlayPolicy.IsAllowed(cfg, Guid.NewGuid(), Remote));
    }

    [Fact]
    public void IsAllowed_True_WhenGloballyOn_ForRemoteUrl()
    {
        var cfg = new PluginConfiguration { DirectPlay = true };
        Assert.True(DirectPlayPolicy.IsAllowed(cfg, Guid.NewGuid(), Remote));
    }

    [Fact]
    public void IsAllowed_UserOverrideWins()
    {
        var user = Guid.NewGuid();
        var cfg = new PluginConfiguration
        {
            DirectPlay = true,
            UserConfigs = [new UserConfig { UserId = user, DirectPlay = false }],
        };
        Assert.False(DirectPlayPolicy.IsAllowed(cfg, user, Remote));
        Assert.True(DirectPlayPolicy.IsAllowed(cfg, Guid.NewGuid(), Remote));
    }

    [Fact]
    public void IsAllowed_False_ForLoopbackEvenWhenOn()
    {
        var cfg = new PluginConfiguration { DirectPlay = true };
        Assert.False(
            DirectPlayPolicy.IsAllowed(cfg, Guid.Empty, "http://127.0.0.1:8096/gelato/stream?ih=x")
        );
    }

    [Fact]
    public void IsAllowed_False_WhenNoConfiguration()
    {
        Assert.False(DirectPlayPolicy.IsAllowed(null, Guid.Empty, Remote));
    }
}

using Gelato.Services;

namespace Gelato.Tests.Common;

/// <summary>
/// DirectPlayPolicy decides whether a stream URL may be handed to a client for direct play.
/// Loopback URLs (the in-process P2P proxy) and placeholder paths must never qualify: a
/// client cannot reach them, and the proxy endpoint rejects non-loopback callers anyway.
/// </summary>
public class DirectPlayPolicyTests
{
    [Theory]
    [InlineData("https://cdn.torbox.app/dl/abc123/movie.mkv")]
    [InlineData("http://example.com/file.mp4")]
    [InlineData("HTTPS://EXAMPLE.COM/UPPER.MKV")]
    [InlineData("https://10.0.0.5/private-lan.mkv")]
    public void RemoteHttpUrl_IsDirectPlayable(string path)
    {
        Assert.True(DirectPlayPolicy.IsDirectPlayable(path));
    }

    [Theory]
    [InlineData("http://127.0.0.1:8096/gelato/stream?ih=abc&idx=0")]
    [InlineData("http://localhost:8096/gelato/stream?ih=abc")]
    [InlineData("http://[::1]:8096/gelato/stream?ih=abc")]
    [InlineData("http://127.5.5.5/anything")]
    public void LoopbackUrl_IsNeverDirectPlayable(string path)
    {
        Assert.False(DirectPlayPolicy.IsDirectPlayable(path));
    }

    [Theory]
    [InlineData("gelato://stub/0123456789abcdef")]
    [InlineData("/stub")]
    [InlineData("/data/movies/local.mkv")]
    [InlineData("C:\\media\\local.mkv")]
    [InlineData("ftp://example.com/file.mkv")]
    [InlineData("not a url")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void NonRemoteOrUnparseablePath_IsNotDirectPlayable(string? path)
    {
        Assert.False(DirectPlayPolicy.IsDirectPlayable(path));
    }
}

namespace Gelato.Services;

/// <summary>
/// Decides whether a stream path may be handed to a client for direct play instead of being
/// masked so the client proxies through Jellyfin. Only a remote http(s) URL qualifies: the
/// in-process P2P proxy lives on loopback and rejects non-loopback callers, and placeholder
/// or local paths are meaningless to a client.
/// </summary>
public static class DirectPlayPolicy
{
    public static bool IsDirectPlayable(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        if (!Uri.TryCreate(path, UriKind.Absolute, out var uri))
            return false;

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return false;

        return !uri.IsLoopback;
    }
}

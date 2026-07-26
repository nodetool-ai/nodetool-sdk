namespace Nodetool.SDK.Connection;

/// <summary>
/// Derives the matching HTTP API and WebSocket endpoints from one explicit
/// NodeTool server address.
/// </summary>
public static class NodeToolEndpointResolver
{
    public static Uri DeriveApiBaseUrl(Uri serverUrl)
    {
        var source = ValidateAbsolute(serverUrl);
        var scheme = source.Scheme switch
        {
            "ws" => Uri.UriSchemeHttp,
            "wss" => Uri.UriSchemeHttps,
            "http" => Uri.UriSchemeHttp,
            "https" => Uri.UriSchemeHttps,
            _ => throw new ArgumentException(
                $"Unsupported NodeTool URL scheme '{source.Scheme}'.",
                nameof(serverUrl))
        };
        var builder = new UriBuilder(source)
        {
            Scheme = scheme,
            Path = StripWebSocketPath(source.AbsolutePath),
            Query = "",
            Fragment = ""
        };
        return EnsureTrailingSlash(builder.Uri);
    }

    public static Uri DeriveWebSocketUrl(Uri serverUrl)
    {
        var source = ValidateAbsolute(serverUrl);
        var scheme = source.Scheme switch
        {
            "http" => "ws",
            "https" => "wss",
            "ws" => "ws",
            "wss" => "wss",
            _ => throw new ArgumentException(
                $"Unsupported NodeTool URL scheme '{source.Scheme}'.",
                nameof(serverUrl))
        };
        var path = source.AbsolutePath.TrimEnd('/');
        if (!path.EndsWith("/ws", StringComparison.OrdinalIgnoreCase))
            path = $"{path}/ws";

        return new UriBuilder(source)
        {
            Scheme = scheme,
            Path = path,
            Query = "",
            Fragment = ""
        }.Uri;
    }

    private static Uri ValidateAbsolute(Uri value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!value.IsAbsoluteUri || string.IsNullOrWhiteSpace(value.Host))
            throw new ArgumentException(
                "NodeTool endpoint must be an absolute network URL.",
                nameof(value));
        return value;
    }

    private static string StripWebSocketPath(string path)
    {
        var normalized = path.TrimEnd('/');
        if (normalized.EndsWith("/ws", StringComparison.OrdinalIgnoreCase))
            normalized = normalized[..^3];
        return normalized;
    }

    private static Uri EnsureTrailingSlash(Uri value)
    {
        var builder = new UriBuilder(value);
        if (!builder.Path.EndsWith('/'))
            builder.Path += "/";
        return builder.Uri;
    }
}

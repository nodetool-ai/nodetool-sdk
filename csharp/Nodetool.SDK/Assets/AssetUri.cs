namespace Nodetool.SDK.Assets;

/// <summary>
/// Helpers for dealing with asset URIs that may be relative ("/api/...") or absolute.
/// </summary>
public static class AssetUri
{
    /// <summary>
    /// Convert a server-relative URI ("/api/...") into an absolute URI using <paramref name="apiBaseUrl"/>.
    /// Leaves absolute URLs, data URIs, and file URIs unchanged.
    /// </summary>
    public static string ToAbsolute(string uri, Uri? apiBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(uri))
            return uri;

        var s = uri.Trim();

        if (s.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return s;

        if (s.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            return s;

        if (Uri.TryCreate(s, UriKind.Absolute, out _))
            return s;

        if (s.StartsWith("/", StringComparison.Ordinal) && apiBaseUrl != null)
        {
            try
            {
                return new Uri(apiBaseUrl, s).ToString();
            }
            catch
            {
                return s;
            }
        }

        return s;
    }
}



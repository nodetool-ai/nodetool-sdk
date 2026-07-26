namespace Nodetool.SDK.Assets;

/// <summary>
/// Parses NodeTool's canonical asset URI forms without depending on a host.
/// </summary>
public static class AssetReferenceUri
{
    public static bool TryGetAssetKey(string? value, out string key)
    {
        key = "";
        var trimmed = value?.Trim() ?? "";
        if (!trimmed.StartsWith("asset:", StringComparison.OrdinalIgnoreCase))
            return false;

        var encoded = trimmed["asset:".Length..].TrimStart('/');
        if (string.IsNullOrWhiteSpace(encoded))
            return false;

        try
        {
            key = Uri.UnescapeDataString(encoded);
            return !string.IsNullOrWhiteSpace(key);
        }
        catch (UriFormatException)
        {
            return false;
        }
    }
}

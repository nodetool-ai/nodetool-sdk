namespace Nodetool.SDK.Assets;

internal static class AssetTypeUtils
{
    public static string InferAssetTypeFromContentType(string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return "file";

        var ct = contentType.Trim().ToLowerInvariant();
        if (ct.StartsWith("image/")) return "image";
        if (ct.StartsWith("audio/")) return "audio";
        if (ct.StartsWith("video/")) return "video";
        if (ct is "text/csv" or "application/json") return "dataframe";
        if (ct.StartsWith("text/")) return "text";
        return "file";
    }
}



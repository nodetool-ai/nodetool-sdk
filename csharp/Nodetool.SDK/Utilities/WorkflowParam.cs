namespace Nodetool.SDK.Utilities;

/// <summary>
/// Convenience helpers for building workflow params values that Nodetool understands.
/// Values returned are plain dictionaries (msgpack map / JSON object compatible).
/// </summary>
public static class WorkflowParam
{
    public static async Task<Dictionary<string, object>> ImageFromFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var uri = await DataUri.FromFileAsync(filePath, cancellationToken);
        return new Dictionary<string, object>
        {
            ["type"] = "image",
            ["uri"] = uri
        };
    }

    public static async Task<Dictionary<string, object>> AudioFromFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var uri = await DataUri.FromFileAsync(filePath, cancellationToken);
        return new Dictionary<string, object>
        {
            ["type"] = "audio",
            ["uri"] = uri
        };
    }
}



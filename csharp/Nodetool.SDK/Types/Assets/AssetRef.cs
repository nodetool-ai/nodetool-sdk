using System.Text.Json.Serialization;

namespace Nodetool.SDK.Types.Assets;

/// <summary>
/// Base class for asset references - C# equivalent of Python's AssetRef
/// </summary>
public abstract class AssetRef : BaseType
{
    /// <summary>
    /// URI of the asset
    /// </summary>
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;

    /// <summary>
    /// Optional asset ID
    /// </summary>
    [JsonPropertyName("asset_id")]
    public string? AssetId { get; set; }

    /// <summary>
    /// Raw data for the asset (used for embedding data URIs)
    /// </summary>
    [JsonPropertyName("data")]
    public object? Data { get; set; }

    /// <summary>
    /// Check if this asset reference is empty
    /// </summary>
    public bool IsEmpty() => string.IsNullOrEmpty(Uri) && AssetId == null && Data == null;

    /// <summary>
    /// Check if this asset reference is set
    /// </summary>
    public bool IsSet() => !IsEmpty();

    /// <summary>
    /// Get the document ID (asset_id if available, otherwise uri)
    /// </summary>
    public string DocumentId => AssetId ?? Uri;

    /// <summary>
    /// Convert this asset to a dictionary
    /// </summary>
    public override Dictionary<string, object> ToDict()
    {
        var result = new Dictionary<string, object>
        {
            ["uri"] = Uri
        };

        if (AssetId != null)
        {
            result["asset_id"] = AssetId;
        }

        return result;
    }
}

/// <summary>
/// Reference to an image asset
/// </summary>
public class ImageRef : AssetRef
{
    public override string Type => "image";

    static ImageRef()
    {
        RegisterType(typeof(ImageRef), "image");
    }
}

/// <summary>
/// Reference to an audio asset
/// </summary>
public class AudioRef : AssetRef
{
    public override string Type => "audio";

    static AudioRef()
    {
        RegisterType(typeof(AudioRef), "audio");
    }
}

/// <summary>
/// Reference to a video asset
/// </summary>
public class VideoRef : AssetRef
{
    public override string Type => "video";

    /// <summary>
    /// Duration in seconds
    /// </summary>
    [JsonPropertyName("duration")]
    public float? Duration { get; set; }

    /// <summary>
    /// Video format
    /// </summary>
    [JsonPropertyName("format")]
    public string? Format { get; set; }

    static VideoRef()
    {
        RegisterType(typeof(VideoRef), "video");
    }
}

/// <summary>
/// Reference to a text asset
/// </summary>
public class TextRef : AssetRef
{
    public override string Type => "text";

    static TextRef()
    {
        RegisterType(typeof(TextRef), "text");
    }
}

/// <summary>
/// Reference to a document asset (PDF, DOCX, etc.)
/// </summary>
public class DocumentRef : AssetRef
{
    public override string Type => "document";

    static DocumentRef()
    {
        RegisterType(typeof(DocumentRef), "document");
    }
}

/// <summary>
/// Reference to a folder
/// </summary>
public class FolderRef : AssetRef
{
    public override string Type => "folder";

    static FolderRef()
    {
        RegisterType(typeof(FolderRef), "folder");
    }
}

/// <summary>
/// Reference to a model file
/// </summary>
public class ModelRef : AssetRef
{
    public override string Type => "model_ref";

    static ModelRef()
    {
        RegisterType(typeof(ModelRef), "model_ref");
    }
} 
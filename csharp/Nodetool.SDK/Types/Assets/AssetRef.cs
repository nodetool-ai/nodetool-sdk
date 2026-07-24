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
    /// Optional temporary asset ID used while an upload is being materialized.
    /// </summary>
    [JsonPropertyName("temp_id")]
    public string? TempId { get; set; }

    /// <summary>
    /// Raw data for the asset (used for embedding data URIs)
    /// </summary>
    [JsonPropertyName("data")]
    public object? Data { get; set; }

    /// <summary>
    /// Optional media metadata supplied by the runtime.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object?>? Metadata { get; set; }

    /// <summary>
    /// Check if this asset reference is empty
    /// </summary>
    public bool IsEmpty()
        => string.IsNullOrEmpty(Uri) &&
           string.IsNullOrEmpty(AssetId) &&
           string.IsNullOrEmpty(TempId) &&
           Data == null;

    /// <summary>
    /// Check if this asset reference is set
    /// </summary>
    public bool IsSet() => !IsEmpty();

    /// <summary>
    /// Get the document ID (asset_id if available, then temp_id, otherwise uri)
    /// </summary>
    public string DocumentId => AssetId ?? TempId ?? Uri;

    /// <summary>
    /// Convert this asset to a dictionary
    /// </summary>
    public override Dictionary<string, object> ToDict()
    {
        var result = new Dictionary<string, object>
        {
            ["type"] = Type,
            ["uri"] = Uri
        };

        if (!string.IsNullOrWhiteSpace(AssetId))
            result["asset_id"] = AssetId;
        if (!string.IsNullOrWhiteSpace(TempId))
            result["temp_id"] = TempId;
        if (Data != null)
            result["data"] = Data;
        if (Metadata != null)
            result["metadata"] = Metadata;

        AddTypeSpecificFields(result);
        return result;
    }

    protected virtual void AddTypeSpecificFields(Dictionary<string, object> result)
    {
    }
}

/// <summary>
/// Reference to an image asset
/// </summary>
public class ImageRef : AssetRef
{
    public override string Type => "image";

    [JsonPropertyName("mimeType")]
    public string? MimeType { get; set; }

    [JsonPropertyName("width")]
    public int? Width { get; set; }

    [JsonPropertyName("height")]
    public int? Height { get; set; }

    protected override void AddTypeSpecificFields(Dictionary<string, object> result)
    {
        if (!string.IsNullOrWhiteSpace(MimeType))
            result["mimeType"] = MimeType;
        if (Width.HasValue)
            result["width"] = Width.Value;
        if (Height.HasValue)
            result["height"] = Height.Value;
    }

    static ImageRef()
    {
        RegisterType(typeof(ImageRef), "image");
    }
}

/// <summary>
/// Reference to an asset whose more specific media kind is not known.
/// </summary>
public class GenericAssetRef : AssetRef
{
    public override string Type => "asset";

    static GenericAssetRef()
    {
        RegisterType(typeof(GenericAssetRef), "asset");
    }
}

/// <summary>
/// Reference to an audio asset
/// </summary>
public class AudioRef : AssetRef
{
    public override string Type => "audio";

    [JsonPropertyName("duration")]
    public float? Duration { get; set; }

    protected override void AddTypeSpecificFields(Dictionary<string, object> result)
    {
        if (Duration.HasValue)
            result["duration"] = Duration.Value;
    }

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

    protected override void AddTypeSpecificFields(Dictionary<string, object> result)
    {
        if (Duration.HasValue)
            result["duration"] = Duration.Value;
        if (!string.IsNullOrWhiteSpace(Format))
            result["format"] = Format;
    }

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

/// <summary>
/// Reference to a 3D model asset.
/// </summary>
public class Model3DRef : AssetRef
{
    public override string Type => "model_3d";

    static Model3DRef()
    {
        RegisterType(typeof(Model3DRef), "model_3d");
    }
}

/// <summary>
/// Reference to a font asset.
/// </summary>
public class FontRef : AssetRef
{
    public override string Type => "font";

    static FontRef()
    {
        RegisterType(typeof(FontRef), "font");
    }
}

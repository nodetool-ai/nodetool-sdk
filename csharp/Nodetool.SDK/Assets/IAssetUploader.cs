using Nodetool.SDK.Types.Assets;

namespace Nodetool.SDK.Assets;

/// <summary>
/// Uploads execution inputs to NodeTool.
/// </summary>
public interface IAssetUploader
{
    Task<AssetRef> UploadAssetAsync(
        string localPath,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upload a stream. The caller retains ownership of the stream.
    /// </summary>
    Task<AssetRef> UploadAssetAsync(
        string fileName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<AssetRef> UploadAssetAsync(
        string fileName,
        ReadOnlyMemory<byte> content,
        string contentType,
        CancellationToken cancellationToken = default);
}

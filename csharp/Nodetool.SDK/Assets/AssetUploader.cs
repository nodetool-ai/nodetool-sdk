using Nodetool.SDK.Api;
using Nodetool.SDK.Api.Models;
using Nodetool.SDK.Types.Assets;

namespace Nodetool.SDK.Assets;

/// <summary>
/// Uploads persistent or temporary execution assets through the NodeTool API.
/// </summary>
public sealed class AssetUploader : IAssetUploader
{
    private readonly INodetoolClient _nodetoolClient;
    private readonly bool _useTemporaryUploads;

    public AssetUploader(
        INodetoolClient nodetoolClient,
        bool useTemporaryUploads = false)
    {
        _nodetoolClient = nodetoolClient ??
            throw new ArgumentNullException(nameof(nodetoolClient));
        _useTemporaryUploads = useTemporaryUploads;
    }

    public async Task<AssetRef> UploadAssetAsync(
        string localPath,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(localPath))
            throw new FileNotFoundException(
                $"Local file not found: {localPath}");

        await using var stream = File.OpenRead(localPath);
        return await UploadAssetAsync(
            Path.GetFileName(localPath),
            stream,
            contentType,
            cancellationToken);
    }

    public async Task<AssetRef> UploadAssetAsync(
        string fileName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        using var nonDisposingContent = new NonDisposingStream(content);
        if (_useTemporaryUploads)
        {
            var temporary = await _nodetoolClient
                .UploadTemporaryAssetAsync(
                    fileName,
                    nonDisposingContent,
                    contentType,
                    cancellationToken);
            return CreateTemporaryAssetReference(
                temporary,
                contentType);
        }

        var persistent = await _nodetoolClient.UploadAssetAsync(
            fileName,
            nonDisposingContent,
            contentType,
            cancellationToken);
        return CreateAssetReference(persistent, contentType);
    }

    public async Task<AssetRef> UploadAssetAsync(
        string fileName,
        ReadOnlyMemory<byte> content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream(
            content.ToArray(),
            writable: false);
        return await UploadAssetAsync(
            fileName,
            stream,
            contentType,
            cancellationToken);
    }

    private static AssetRef CreateAssetReference(
        AssetResponse response,
        string requestedContentType)
    {
        var contentType = string.IsNullOrWhiteSpace(response.ContentType)
            ? requestedContentType
            : response.ContentType;
        var result = CreateTypedAssetReference(contentType);

        result.AssetId = response.Id;
        result.Uri = response.GetUrl
            ?? (!string.IsNullOrWhiteSpace(response.Uri)
                ? response.Uri
                : $"/api/assets/{response.Id}/download");
        result.Metadata = new Dictionary<string, object?>
        {
            ["content_type"] = contentType,
            ["name"] = response.Name,
            ["size"] = response.Size
        };
        return result;
    }

    private static AssetRef CreateTemporaryAssetReference(
        TemporaryAssetUploadResponse response,
        string requestedContentType)
    {
        if (string.IsNullOrWhiteSpace(response.Uri))
        {
            throw new InvalidOperationException(
                "Temporary asset upload returned no URI.");
        }

        var contentType = string.IsNullOrWhiteSpace(response.ContentType)
            ? requestedContentType
            : response.ContentType;
        var result = CreateTypedAssetReference(contentType);
        result.Uri = response.Uri;
        result.Metadata = new Dictionary<string, object?>
        {
            ["content_type"] = contentType,
            ["name"] = response.Name,
            ["size"] = response.Size,
            ["temporary"] = true,
            ["expires_at"] = response.ExpiresAt
        };
        return result;
    }

    private static AssetRef CreateTypedAssetReference(string contentType)
        => contentType.ToLowerInvariant() switch
        {
            var value when value.StartsWith("image/") => new ImageRef
            {
                MimeType = contentType
            },
            var value when value.StartsWith("audio/") => new AudioRef(),
            var value when value.StartsWith("video/") => new VideoRef(),
            "application/pdf" => new DocumentRef(),
            _ => new GenericAssetRef()
        };

    private sealed class NonDisposingStream(Stream inner) : Stream
    {
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => inner.CanWrite;
        public override long Length => inner.Length;
        public override long Position
        {
            get => inner.Position;
            set => inner.Position = value;
        }

        public override void Flush() => inner.Flush();
        public override int Read(
            byte[] buffer,
            int offset,
            int count)
            => inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin)
            => inner.Seek(offset, origin);
        public override void SetLength(long value)
            => inner.SetLength(value);
        public override void Write(
            byte[] buffer,
            int offset,
            int count)
            => inner.Write(buffer, offset, count);
        public override Task FlushAsync(
            CancellationToken cancellationToken)
            => inner.FlushAsync(cancellationToken);
        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
            => inner.ReadAsync(buffer, cancellationToken);
        public override ValueTask WriteAsync(
            ReadOnlyMemory<byte> buffer,
            CancellationToken cancellationToken = default)
            => inner.WriteAsync(buffer, cancellationToken);

        protected override void Dispose(bool disposing)
        {
            // The caller owns the wrapped stream.
            base.Dispose(disposing);
        }
    }
}

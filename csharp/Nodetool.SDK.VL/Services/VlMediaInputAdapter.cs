using Nodetool.SDK.Api;
using Nodetool.SDK.Assets;
using SkiaSharp;
using System.Runtime.CompilerServices;

namespace Nodetool.SDK.VL.Services;

/// <summary>
/// VL-only projection for host image encoding and current connection
/// configuration. Generic media policy remains in the portable SDK.
/// </summary>
internal static class VlMediaInputAdapter
{
    private sealed record EncodedImage(byte[] Bytes);
    private static readonly ConditionalWeakTable<SKImage, EncodedImage>
        EncodedImages = new();

    internal static ValueTask<object?> AdaptValueAsync(
        string inputName,
        string mediaType,
        object? value,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (value is SKImage image)
        {
            var cached = EncodedImages.GetValue(
                image,
                static source =>
                {
                    using var encoded = source.Encode(
                        SKEncodedImageFormat.Png,
                        100) ?? throw new InvalidOperationException(
                        "Could not encode image input.");
                    return new EncodedImage(encoded.ToArray());
                });
            return ValueTask.FromResult<object?>(cached.Bytes);
        }
        return ValueTask.FromResult(value);
    }

    internal static async Task<object> PrepareAsync(
        string inputName,
        string mediaType,
        object? value,
        bool useTemporaryAssetUploads,
        CancellationToken cancellationToken)
    {
        var portableValue = await AdaptValueAsync(
            inputName,
            mediaType,
            value,
            cancellationToken);

        NodetoolClient? apiClient = null;
        AssetManager? assetManager = null;
        try
        {
            if (NodeToolClientProvider.CurrentApiBaseUrl is { } apiBase)
            {
                apiClient = new NodetoolClient(
                    apiBase,
                    NodeToolClientProvider.CurrentAuthToken);
                assetManager = new AssetManager(
                    nodetoolClient: apiClient,
                    useTemporaryUploads: useTemporaryAssetUploads);
            }

            return await new MediaInputPreparer(
                    assetManager,
                    NodeToolClientProvider.InlineMediaLimitBytes)
                .PrepareAsync(
                    inputName,
                    mediaType,
                    portableValue,
                    cancellationToken);
        }
        finally
        {
            assetManager?.Dispose();
            apiClient?.Dispose();
        }
    }
}

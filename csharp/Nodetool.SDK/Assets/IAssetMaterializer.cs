using Nodetool.SDK.Types.Assets;

namespace Nodetool.SDK.Assets;

/// <summary>
/// Host-neutral service that resolves a NodeTool asset reference into a local
/// file. Hosts decide how to project that file into their own path, texture,
/// audio, or document types.
/// </summary>
public interface IAssetMaterializer
{
    Task<AssetMaterializationResult> MaterializeAsync(
        AssetRef asset,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default);
}

using Nodetool.SDK.Types.Assets;

namespace Nodetool.SDK.Assets;

/// <summary>
/// Saves a NodeTool asset to a caller-selected local destination.
/// </summary>
public interface IAssetSaver
{
    Task<AssetSaveResult> SaveAsync(
        AssetRef asset,
        string destinationPath,
        bool overwrite = false,
        CancellationToken cancellationToken = default);
}

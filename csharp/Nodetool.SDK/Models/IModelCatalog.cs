namespace Nodetool.SDK.Models;

public interface IModelCatalog
{
    ModelCatalogSnapshot Snapshot { get; }

    Task<ModelCatalogSnapshot> RefreshAsync(
        bool force = false,
        CancellationToken cancellationToken = default);

    IReadOnlyList<ModelDescriptor> FindCompatible(
        string compatibility,
        bool readyOnly = true);

    ModelDescriptor? GetByKey(string key);

    void Clear();
}

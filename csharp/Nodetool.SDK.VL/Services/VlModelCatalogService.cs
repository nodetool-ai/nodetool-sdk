using Nodetool.SDK.Models;
using Nodetool.SDK.VL.Utilities;

namespace Nodetool.SDK.VL.Services;

internal static class VlModelCatalogService
{
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);
    private static ModelCatalog? _catalog;
    private static string? _cacheScope;

    public static async Task<ModelCatalogSnapshot> RefreshAsync(
        bool force,
        CancellationToken cancellationToken)
    {
        await RefreshLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var client = await NodeToolClientProvider
                .GetApiClientAsync(cancellationToken)
                .ConfigureAwait(false);
            var scope = CreateCacheScope();
            if (_catalog == null ||
                !string.Equals(_cacheScope, scope, StringComparison.Ordinal))
            {
                _catalog?.Dispose();
                _catalog = new ModelCatalog(client, scope);
                _cacheScope = scope;
                force = true;
            }

            ModelCatalogSnapshot snapshot;
            try
            {
                snapshot = await _catalog
                    .RefreshAsync(force, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                VlLog.Debug("model catalog refresh timed out; retaining current pin mapping");
                return _catalog.Snapshot;
            }
            if (snapshot.LastSuccessfulRefreshUtc.HasValue)
                DynamicModelEnumFactory.UpdateCatalog(snapshot);
            else if (!string.IsNullOrWhiteSpace(snapshot.LastError))
                VlLog.Debug($"model catalog unavailable: {snapshot.LastError}");
            return snapshot;
        }
        finally
        {
            RefreshLock.Release();
        }
    }

    public static void Reset()
    {
        _catalog?.Dispose();
        _catalog = null;
        _cacheScope = null;
        DynamicModelEnumFactory.ResetCatalog();
    }

    private static string CreateCacheScope()
    {
        var endpoint = NodeToolClientProvider.CurrentApiBaseUrl?.AbsoluteUri ?? "";
        var principal = string.IsNullOrWhiteSpace(
            NodeToolClientProvider.CurrentAuthToken)
            ? "trusted-local"
            : "authenticated";
        return $"{endpoint}|{principal}|local";
    }
}

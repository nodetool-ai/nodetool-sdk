using System.Security.Cryptography;
using System.Text;
using Nodetool.SDK.Api.Models;
using Nodetool.SDK.Models;
using Nodetool.SDK.VL.Utilities;

namespace Nodetool.SDK.VL.Services;

internal static class VlModelCatalogService
{
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);
    private static ModelCatalog? _catalog;
    private static string? _cacheScope;
    private static long _generation;

    public static async Task<ModelCatalogSnapshot> RefreshAsync(
        bool force,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            await RefreshLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var generation = Volatile.Read(ref _generation);
                var client = await NodeToolClientProvider
                    .GetApiClientAsync(cancellationToken)
                    .ConfigureAwait(false);
                var scope = CreateCurrentCacheScope();
                if (_catalog == null ||
                    !string.Equals(_cacheScope, scope, StringComparison.Ordinal))
                {
                    _catalog?.Dispose();
                    _catalog = new ModelCatalog(client, scope);
                    _cacheScope = scope;
                    force = true;
                }
                var catalog = _catalog;

                ModelCatalogSnapshot snapshot;
                try
                {
                    snapshot = await catalog
                        .RefreshAsync(force, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                    when (!cancellationToken.IsCancellationRequested)
                {
                    VlLog.Debug("model catalog refresh timed out; retaining current pin mapping");
                    return catalog.Snapshot;
                }

                // Connection reset can replace the catalog while an old HTTP
                // request is in flight. Retry under the new scope instead of
                // publishing the old principal's models into dynamic enums.
                if (generation != Volatile.Read(ref _generation))
                    continue;

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
    }

    public static void Reset()
    {
        Interlocked.Increment(ref _generation);
        _catalog?.Dispose();
        _catalog = null;
        _cacheScope = null;
        DynamicModelEnumFactory.ResetCatalog();
    }

    internal static string CreateCurrentCacheScope()
        => CreateCacheScope(
            NodeToolClientProvider.CurrentApiBaseUrl,
            NodeToolClientProvider.CurrentAuthToken,
            SdkModelScopes.Local);

    internal static string CreateCacheScope(
        Uri? endpoint,
        string? authToken,
        string modelScope)
    {
        var principal = string.IsNullOrWhiteSpace(authToken)
            ? "anonymous"
            : Convert.ToHexString(SHA256.HashData(
                Encoding.UTF8.GetBytes(authToken)));
        return $"{endpoint?.AbsoluteUri ?? ""}|{principal}|{modelScope}";
    }
}

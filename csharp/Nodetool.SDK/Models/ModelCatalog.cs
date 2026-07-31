using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nodetool.SDK.Api.Models;
using Nodetool.SDK.Diagnostics;

namespace Nodetool.SDK.Models;

/// <summary>
/// Host-neutral, revision-aware model inventory with last-known-good behavior.
/// The caller-supplied cache scope must identify endpoint, principal, and
/// execution target so inventories from different servers cannot mix.
/// </summary>
public sealed class ModelCatalog : IModelCatalog, IDisposable
{
    private readonly IModelCatalogClient _client;
    private readonly string _cacheScope;
    private readonly string _modelScope;
    private readonly TimeSpan _cacheDuration;
    private readonly ILogger<ModelCatalog> _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private ModelCatalogSnapshot _snapshot = ModelCatalogSnapshot.Empty;
    private DateTimeOffset _expiresUtc = DateTimeOffset.MinValue;
    private bool _disposed;

    public ModelCatalogSnapshot Snapshot => Volatile.Read(ref _snapshot);

    public ModelCatalog(
        IModelCatalogClient client,
        string cacheScope,
        string modelScope = SdkModelScopes.Local,
        TimeSpan? cacheDuration = null,
        ILogger<ModelCatalog>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheScope);
        if (modelScope is not (SdkModelScopes.Local or SdkModelScopes.Worker))
            throw new ArgumentOutOfRangeException(nameof(modelScope));

        _client = client;
        _cacheScope = cacheScope;
        _modelScope = modelScope;
        _cacheDuration = cacheDuration ?? TimeSpan.FromMinutes(5);
        _logger = logger ?? NullLogger<ModelCatalog>.Instance;
    }

    public async Task<ModelCatalogSnapshot> RefreshAsync(
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!force && HasFreshSnapshot())
            return Snapshot;

        await _refreshLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!force && HasFreshSnapshot())
                return Snapshot;
            return await RefreshCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public IReadOnlyList<ModelDescriptor> FindCompatible(
        string compatibility,
        bool readyOnly = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(compatibility);
        return Snapshot.Models
            .Where(model =>
                string.Equals(
                    model.Compatibility,
                    compatibility,
                    StringComparison.Ordinal) &&
                (!readyOnly || model.IsReady))
            .ToArray();
    }

    public ModelDescriptor? GetByKey(string key)
        => Snapshot.Models.FirstOrDefault(model =>
            string.Equals(model.Key, key, StringComparison.Ordinal));

    public void Clear()
    {
        Volatile.Write(
            ref _snapshot,
            ModelCatalogSnapshot.Empty with { Scope = _modelScope });
        _expiresUtc = DateTimeOffset.MinValue;
    }

    private bool HasFreshSnapshot()
        => Snapshot.LastSuccessfulRefreshUtc.HasValue &&
           DateTimeOffset.UtcNow < _expiresUtc;

    private async Task<ModelCatalogSnapshot> RefreshCoreAsync(
        CancellationToken cancellationToken)
    {
        var previous = Snapshot;
        try
        {
            var entries = new List<SdkModelCatalogEntryResponse>();
            var visitedCursors = new HashSet<string>(StringComparer.Ordinal);
            string? cursor = null;
            string? revision = null;
            do
            {
                var response = await _client.GetModelCatalogAsync(
                    new SdkModelCatalogQuery(
                        Scope: _modelScope,
                        Cursor: cursor,
                        Limit: 500),
                    cancellationToken).ConfigureAwait(false);
                ValidateResponse(response, revision);
                revision ??= response.CatalogRevision;
                entries.AddRange(response.Entries);
                cursor = response.NextCursor;
                if (cursor != null && !visitedCursors.Add(cursor))
                    throw new InvalidDataException(
                        "The NodeTool model catalog repeated a pagination cursor.");
            }
            while (cursor != null);

            var now = DateTimeOffset.UtcNow;
            var refreshed = new ModelCatalogSnapshot(
                revision ?? string.Empty,
                _modelScope,
                entries.Select(Map).ToArray(),
                now,
                false,
                null);
            Volatile.Write(ref _snapshot, refreshed);
            _expiresUtc = now + _cacheDuration;
            return refreshed;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            var safeError = NodeToolDiagnosticRedactor.RedactText(
                exception.Message);
            var stale = previous with
            {
                IsStale = previous.LastSuccessfulRefreshUtc.HasValue,
                LastError = safeError
            };
            Volatile.Write(ref _snapshot, stale);
            _logger.LogWarning(
                "Model catalog refresh failed for {Scope}: {Error}",
                _cacheScope,
                safeError);
            return stale;
        }
    }

    private void ValidateResponse(
        SdkModelCatalogResponse response,
        string? expectedRevision)
    {
        if (!string.Equals(response.Version, "1", StringComparison.Ordinal) ||
            !string.Equals(response.Scope, _modelScope, StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(response.CatalogRevision) ||
            expectedRevision != null &&
            !string.Equals(
                response.CatalogRevision,
                expectedRevision,
                StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "The NodeTool model catalog response changed during pagination or is incompatible.");
        }
    }

    private static ModelDescriptor Map(SdkModelCatalogEntryResponse entry)
        => new(
            entry.Key,
            entry.DisplayName,
            entry.Compatibility,
            entry.Availability,
            entry.Recommended,
            entry.Scope,
            entry.Provider,
            entry.Id,
            entry.RepositoryId,
            entry.Path,
            entry.SupportedTasks.ToArray(),
            entry.SizeOnDisk,
            entry.WireValue.Clone());

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _refreshLock.Dispose();
    }
}

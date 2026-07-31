using System.Runtime.CompilerServices;
using Nodetool.SDK.Api.Models;

namespace Nodetool.SDK.Models;

/// <summary>
/// Portable model-download lifecycle over the public SDK HTTP contract.
/// Polling is exposed as an async stream so hosts can marshal updates onto
/// their own UI or game loop without a dependency on a reactive framework.
/// </summary>
public sealed class ModelDownloadService : IModelDownloadService
{
    private readonly IModelDownloadClient _client;
    private readonly IModelCatalog? _catalog;
    private readonly string _scope;
    private readonly object _snapshotLock = new();
    private readonly HashSet<string> _catalogRefreshes = new(StringComparer.Ordinal);
    private ModelDownloadSnapshot _snapshot;

    public ModelDownloadSnapshot Snapshot
    {
        get
        {
            lock (_snapshotLock)
                return _snapshot;
        }
    }

    public ModelDownloadService(
        IModelDownloadClient client,
        string scope = SdkModelScopes.Local,
        IModelCatalog? catalog = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        if (scope is not (SdkModelScopes.Local or SdkModelScopes.Worker))
            throw new ArgumentOutOfRangeException(nameof(scope));
        _client = client;
        _scope = scope;
        _catalog = catalog;
        _snapshot = ModelDownloadSnapshot.Empty(scope);
    }

    public async Task<ModelDownloadState> StartAsync(
        ModelDescriptor model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        if (string.IsNullOrWhiteSpace(model.RepositoryId))
            throw new ArgumentException(
                "The selected model does not identify a downloadable repository.",
                nameof(model));
        if (!string.Equals(model.Scope, _scope, StringComparison.Ordinal))
            throw new ArgumentException(
                "The selected model belongs to a different execution scope.",
                nameof(model));

        return await StartCoreAsync(
            new SdkModelDownloadStartRequest(
                model.RepositoryId,
                model.Compatibility,
                model.Path,
                _scope),
            cancellationToken).ConfigureAwait(false);
    }

    public Task<ModelDownloadState> RetryAsync(
        ModelDownloadState download,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(download);
        if (!download.IsTerminal)
            throw new InvalidOperationException(
                "Only a terminal model download can be retried.");
        if (!string.Equals(download.Scope, _scope, StringComparison.Ordinal))
            throw new ArgumentException(
                "The download belongs to a different execution scope.",
                nameof(download));
        lock (_snapshotLock)
            _catalogRefreshes.Remove(download.OperationId);
        return StartCoreAsync(
            new SdkModelDownloadStartRequest(
                download.RepositoryId,
                download.ModelType,
                download.Path,
                _scope),
            cancellationToken);
    }

    public async Task<ModelDownloadState> CancelAsync(
        string operationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        var response = await _client.CancelModelDownloadAsync(
            operationId,
            cancellationToken).ConfigureAwait(false);
        return Store(Map(response));
    }

    public async Task<ModelDownloadSnapshot> RefreshAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _client.GetModelDownloadsAsync(
            new SdkModelDownloadQuery(_scope),
            cancellationToken).ConfigureAwait(false);
        var snapshot = new ModelDownloadSnapshot(
            _scope,
            response.Downloads.Select(Map).ToArray(),
            DateTimeOffset.UtcNow);
        lock (_snapshotLock)
            _snapshot = snapshot;
        foreach (var download in snapshot.Downloads)
            await RefreshCatalogAfterCompletionAsync(download, cancellationToken)
                .ConfigureAwait(false);
        return snapshot;
    }

    public async IAsyncEnumerable<ModelDownloadState> MonitorAsync(
        string operationId,
        TimeSpan? pollInterval = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        var interval = pollInterval ?? TimeSpan.FromMilliseconds(500);
        if (interval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(pollInterval));

        ModelDownloadState? previous = null;
        while (true)
        {
            var response = await _client.GetModelDownloadsAsync(
                new SdkModelDownloadQuery(_scope, operationId),
                cancellationToken).ConfigureAwait(false);
            var current = response.Downloads.Count switch
            {
                1 => Map(response.Downloads[0]),
                0 => throw new KeyNotFoundException(
                    $"Model download operation '{operationId}' was not found."),
                _ => throw new InvalidDataException(
                    "The model download snapshot contained duplicate operations.")
            };
            Store(current);
            if (previous == null || !Equivalent(previous, current))
            {
                yield return current;
                previous = current;
            }
            if (current.IsTerminal)
            {
                await RefreshCatalogAfterCompletionAsync(
                    current,
                    cancellationToken).ConfigureAwait(false);
                yield break;
            }
            await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<ModelDownloadState> StartCoreAsync(
        SdkModelDownloadStartRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _client.StartModelDownloadAsync(
            request,
            cancellationToken).ConfigureAwait(false);
        return Store(Map(response));
    }

    private ModelDownloadState Store(ModelDownloadState state)
    {
        lock (_snapshotLock)
        {
            var downloads = _snapshot.Downloads
                .Where(item => !string.Equals(
                    item.OperationId,
                    state.OperationId,
                    StringComparison.Ordinal))
                .Append(state)
                .OrderByDescending(item => item.UpdatedAt)
                .ToArray();
            _snapshot = new ModelDownloadSnapshot(
                _scope,
                downloads,
                DateTimeOffset.UtcNow);
        }
        return state;
    }

    private async Task RefreshCatalogAfterCompletionAsync(
        ModelDownloadState state,
        CancellationToken cancellationToken)
    {
        if (state.Status != SdkModelDownloadStatuses.Completed ||
            _catalog == null)
        {
            return;
        }
        lock (_snapshotLock)
        {
            if (!_catalogRefreshes.Add(state.OperationId)) return;
        }
        try
        {
            var snapshot = await _catalog.RefreshAsync(
                force: true,
                cancellationToken).ConfigureAwait(false);
            if (snapshot.IsStale || !string.IsNullOrWhiteSpace(snapshot.LastError))
            {
                lock (_snapshotLock)
                    _catalogRefreshes.Remove(state.OperationId);
            }
        }
        catch
        {
            lock (_snapshotLock)
                _catalogRefreshes.Remove(state.OperationId);
            throw;
        }
    }

    private static ModelDownloadState Map(
        SdkModelDownloadStateResponse response)
        => new(
            response.OperationId,
            response.Scope,
            response.RepositoryId,
            response.Path,
            response.ModelType,
            response.Status,
            response.DownloadedBytes,
            response.TotalBytes,
            response.DownloadedFiles,
            response.CurrentFiles.ToArray(),
            response.TotalFiles,
            response.Error,
            response.StartedAt,
            response.UpdatedAt);

    private static bool Equivalent(
        ModelDownloadState left,
        ModelDownloadState right)
        => left.OperationId == right.OperationId &&
           left.Status == right.Status &&
           left.DownloadedBytes == right.DownloadedBytes &&
           left.TotalBytes == right.TotalBytes &&
           left.DownloadedFiles == right.DownloadedFiles &&
           left.TotalFiles == right.TotalFiles &&
           left.Error == right.Error &&
           left.UpdatedAt == right.UpdatedAt &&
           left.CurrentFiles.SequenceEqual(
               right.CurrentFiles,
               StringComparer.Ordinal);
}

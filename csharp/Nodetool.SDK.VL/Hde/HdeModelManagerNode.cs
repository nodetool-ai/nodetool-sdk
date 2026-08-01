using Nodetool.SDK.Api.Models;
using Nodetool.SDK.Models;
using Nodetool.SDK.VL.Services;
using Nodetool.SDK.VL.Utilities;

namespace Nodetool.SDK.VL.Hde;

internal sealed class HdeModelManagerNode : IDisposable
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);
    private readonly CancellationTokenSource _lifetime = new();
    private readonly CancellationToken _lifetimeToken;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private readonly object _stateLock = new();
    private readonly HashSet<string> _actingKeys = new(StringComparer.Ordinal);
    private ModelCatalogSnapshot _catalog = ModelCatalogSnapshot.Empty;
    private ModelDownloadService? _downloads;
    private string? _downloadScope;
    private HdeModelFamily _family = HdeModelFamily.Image;
    private string _search = "";
    private int _pageIndex;
    private int _pageSize = 100;
    private string _target = "Target: resolving...";
    private string _notice = "Loading model catalog...";
    private HdeModelPageSnapshot _view = HdeModelPageSnapshot.Empty;
    private CancellationTokenSource? _monitorCancellation;
    private bool _initialized;
    private volatile bool _disposed;

    public HdeModelManagerNode()
    {
        _lifetimeToken = _lifetime.Token;
        lock (_stateLock)
        {
            UpdateTargetLocked();
            UpdateViewLocked();
        }
    }

    public HdeModelPageSnapshot ReadState()
    {
        lock (_stateLock)
            return _view;
    }

    public void Update()
    {
        if (_disposed || _initialized) return;
        _initialized = true;
        _ = RefreshAsync(force: false);
    }

    public void SelectFamily(HdeModelFamily family)
    {
        lock (_stateLock)
        {
            if (_family == family) return;
            _family = family;
            _pageIndex = 0;
            UpdateViewLocked();
        }
    }

    public void SetSearch(string? search)
    {
        search = search?.Trim() ?? "";
        lock (_stateLock)
        {
            if (string.Equals(_search, search, StringComparison.Ordinal)) return;
            _search = search;
            _pageIndex = 0;
            UpdateViewLocked();
        }
    }

    public void SetPageSize(int pageSize)
    {
        pageSize = pageSize switch
        {
            <= 50 => 50,
            <= 100 => 100,
            _ => 200
        };
        lock (_stateLock)
        {
            if (_pageSize == pageSize) return;
            _pageSize = pageSize;
            _pageIndex = 0;
            UpdateViewLocked();
        }
    }

    public void PreviousPage()
    {
        lock (_stateLock)
        {
            if (_pageIndex == 0) return;
            _pageIndex--;
            UpdateViewLocked();
        }
    }

    public void NextPage()
    {
        lock (_stateLock)
        {
            if (_pageIndex + 1 >= _view.PageCount) return;
            _pageIndex++;
            UpdateViewLocked();
        }
    }

    public void Refresh() => _ = RefreshAsync(force: true);

    public void Act(string modelKey) => _ = ActAsync(modelKey);

    private async Task RefreshAsync(bool force)
    {
        try
        {
            await _refreshLock.WaitAsync(_lifetimeToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_lifetimeToken.IsCancellationRequested)
        {
            return;
        }

        try
        {
            lock (_stateLock)
            {
                _notice = force ? "Refreshing model catalog..." : "Loading model catalog...";
                UpdateTargetLocked();
                UpdateViewLocked();
            }
            var snapshot = await VlModelCatalogService
                .RefreshAsync(force, _lifetimeToken)
                .ConfigureAwait(false);
            var client = await NodeToolClientProvider
                .GetApiClientAsync(_lifetimeToken)
                .ConfigureAwait(false);
            var scope = VlModelCatalogService.CreateCurrentCacheScope();
            ModelDownloadService downloads;
            CancellationTokenSource? previousMonitor = null;
            lock (_stateLock)
            {
                if (_downloads == null ||
                    !string.Equals(_downloadScope, scope, StringComparison.Ordinal))
                {
                    previousMonitor = _monitorCancellation;
                    _monitorCancellation = null;
                    _downloads = new ModelDownloadService(client);
                    _downloadScope = scope;
                }
                downloads = _downloads;
            }
            CancelAndDispose(previousMonitor);
            await downloads.RefreshAsync(_lifetimeToken).ConfigureAwait(false);

            if (_disposed) return;
            lock (_stateLock)
            {
                _catalog = snapshot;
                _notice = snapshot.LastError is { Length: > 0 } error
                    ? $"Catalog warning: {error}"
                    : "";
                UpdateViewLocked();
            }
            StartMonitorIfNeeded();
        }
        catch (OperationCanceledException) when (_lifetimeToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            SetNotice($"Error: {VlLog.SafeError(exception, NodeToolClientProvider.CurrentAuthToken)}");
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task ActAsync(string modelKey)
    {
        ModelDescriptor? model;
        ModelDownloadService? service;
        ModelDownloadState? download;
        lock (_stateLock)
        {
            if (!_actingKeys.Add(modelKey)) return;
            model = _catalog.Models.FirstOrDefault(item =>
                string.Equals(item.Key, modelKey, StringComparison.Ordinal));
            service = _downloads;
            download = model == null || service == null
                ? null
                : HdeModelListProjector.FindDownload(model, service.Snapshot);
            UpdateViewLocked();
        }

        try
        {
            if (model == null || service == null) return;
            if (download is { IsTerminal: false })
            {
                await service.CancelAsync(download.OperationId, _lifetimeToken)
                    .ConfigureAwait(false);
            }
            else if (download is
                     { Status: SdkModelDownloadStatuses.Error or SdkModelDownloadStatuses.Cancelled })
            {
                await service.RetryAsync(download, _lifetimeToken).ConfigureAwait(false);
            }
            else if (!model.IsReady &&
                     model.Availability == SdkModelAvailability.Downloadable &&
                     !string.IsNullOrWhiteSpace(model.RepositoryId))
            {
                await service.StartAsync(model, _lifetimeToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (_lifetimeToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            SetNotice($"Error: {VlLog.SafeError(exception, NodeToolClientProvider.CurrentAuthToken)}");
        }
        finally
        {
            lock (_stateLock)
            {
                _actingKeys.Remove(modelKey);
                UpdateViewLocked();
            }
            StartMonitorIfNeeded();
        }
    }

    private void StartMonitorIfNeeded()
    {
        ModelDownloadService? service;
        CancellationTokenSource monitor;
        lock (_stateLock)
        {
            if (_disposed || _monitorCancellation != null) return;
            service = _downloads;
            if (service == null || !HasActiveDownloads(service.Snapshot)) return;
            monitor = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeToken);
            _monitorCancellation = monitor;
        }
        _ = MonitorDownloadsAsync(service, monitor);
    }

    private async Task MonitorDownloadsAsync(
        ModelDownloadService service,
        CancellationTokenSource monitor)
    {
        var token = monitor.Token;
        var checkForNewDownloads = false;
        try
        {
            while (true)
            {
                await Task.Delay(PollInterval, token).ConfigureAwait(false);
                var downloads = await service.RefreshAsync(token).ConfigureAwait(false);
                lock (_stateLock)
                    UpdateViewLocked();
                if (!HasActiveDownloads(downloads))
                {
                    checkForNewDownloads = true;
                    break;
                }
            }

            var catalog = await VlModelCatalogService
                .RefreshAsync(force: true, token)
                .ConfigureAwait(false);
            lock (_stateLock)
            {
                _catalog = catalog;
                _notice = catalog.LastError is { Length: > 0 } error
                    ? $"Catalog warning: {error}"
                    : "";
                UpdateViewLocked();
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            SetNotice($"Download error: {VlLog.SafeError(exception, NodeToolClientProvider.CurrentAuthToken)}");
        }
        finally
        {
            lock (_stateLock)
            {
                if (ReferenceEquals(_monitorCancellation, monitor))
                    _monitorCancellation = null;
            }
            monitor.Dispose();
            if (checkForNewDownloads)
                StartMonitorIfNeeded();
        }
    }

    private static bool HasActiveDownloads(ModelDownloadSnapshot snapshot)
        => snapshot.Downloads.Any(download => !download.IsTerminal);

    private void UpdateViewLocked()
    {
        _view = HdeModelListProjector.Project(
            _catalog,
            _downloads?.Snapshot ?? ModelDownloadSnapshot.Empty(SdkModelScopes.Local),
            _family,
            _search,
            _pageIndex,
            _pageSize,
            _actingKeys,
            _target,
            _notice);
        _pageIndex = _view.PageNumber - 1;
    }

    private void SetNotice(string value)
    {
        lock (_stateLock)
        {
            _notice = value;
            UpdateViewLocked();
        }
    }

    private void UpdateTargetLocked()
    {
        var endpoint = NodeToolClientProvider.CurrentApiBaseUrl?.AbsoluteUri.TrimEnd('/')
                       ?? "http://127.0.0.1:7777";
        _target = $"Target: {endpoint} · local";
    }

    public void Dispose()
    {
        CancellationTokenSource? monitor;
        lock (_stateLock)
        {
            if (_disposed) return;
            _disposed = true;
            monitor = _monitorCancellation;
            _monitorCancellation = null;
        }
        _lifetime.Cancel();
        CancelAndDispose(monitor);
        _lifetime.Dispose();
    }

    private static void CancelAndDispose(CancellationTokenSource? cancellation)
    {
        if (cancellation == null) return;
        try
        {
            cancellation.Cancel();
        }
        finally
        {
            cancellation.Dispose();
        }
    }
}

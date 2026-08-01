using Nodetool.SDK.Api.Models;
using Nodetool.SDK.Models;
using Nodetool.SDK.VL.Services;
using Nodetool.SDK.VL.Utilities;

namespace Nodetool.SDK.VL.Hde;

internal enum HdeModelFamily
{
    Language,
    Image,
    Audio,
    Video3D,
    Other
}

internal static class HdeModelFamilyClassifier
{
    public static HdeModelFamily Classify(string compatibility)
    {
        var value = compatibility.ToLowerInvariant();
        if (ContainsAny(value, "audio", "speech", "asr", "tts", "music"))
            return HdeModelFamily.Audio;
        if (ContainsAny(value, "video", "3d", "mesh", "point_cloud", "pointcloud"))
            return HdeModelFamily.Video3D;
        if (ContainsAny(
                value,
                "image",
                "flux",
                "stable_diffusion",
                "controlnet",
                "inpainting",
                "lora",
                "ip_adapter",
                "real_esrgan",
                "depth",
                "object_detection",
                "segmentation"))
        {
            return HdeModelFamily.Image;
        }
        if (ContainsAny(
                value,
                "text",
                "language",
                "embedding",
                "reranker",
                "sentence",
                "fill_mask",
                "question_answering",
                "t5",
                "llama"))
        {
            return HdeModelFamily.Language;
        }
        return HdeModelFamily.Other;
    }

    private static bool ContainsAny(string value, params string[] parts)
        => parts.Any(value.Contains);
}

internal sealed class HdeModelManagerNode : IDisposable
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);
    private readonly CancellationTokenSource _lifetime = new();
    private readonly object _stateLock = new();
    private ModelCatalogSnapshot _catalog = ModelCatalogSnapshot.Empty;
    private ModelDownloadService? _downloads;
    private string? _downloadScope;
    private HdeModelFamily _family = HdeModelFamily.Image;
    private ModelDescriptor? _selected;
    private ModelDownloadState? _selectedDownload;
    private CancellationTokenSource? _monitorCancellation;
    private bool _initialized;
    private volatile bool _disposed;
    private int _refreshing;
    private int _acting;
    private bool _lastLanguage;
    private bool _lastImage;
    private bool _lastAudio;
    private bool _lastVideo3D;
    private bool _lastOther;
    private bool _lastRefresh;
    private bool _lastAction;

    public HdeModelManagerNode() => UpdateTarget();

    public bool Language { get; set; }
    public bool Image { get; set; }
    public bool Audio { get; set; }
    public bool Video3D { get; set; }
    public bool Other { get; set; }
    public bool Refresh { get; set; }
    public bool Action { get; set; }
    public string Target { get; private set; } = "Target: resolving...";
    public string Family { get; private set; } = "Image";
    public string Model { get; private set; } = "Loading models...";
    public string Status { get; private set; } = "Loading catalog...";
    public string ActionLabel { get; private set; } = "Download";
    public float Progress { get; private set; }
    public string ProgressText { get; private set; } = "";
    public bool CanAct { get; private set; }

    public void Update()
    {
        if (_disposed) return;
        if (!_initialized)
        {
            _initialized = true;
            _ = RefreshAsync(force: false);
        }

        HandleFamilyTrigger(Language, HdeModelFamily.Language, ref _lastLanguage);
        HandleFamilyTrigger(Image, HdeModelFamily.Image, ref _lastImage);
        HandleFamilyTrigger(Audio, HdeModelFamily.Audio, ref _lastAudio);
        HandleFamilyTrigger(Video3D, HdeModelFamily.Video3D, ref _lastVideo3D);
        HandleFamilyTrigger(Other, HdeModelFamily.Other, ref _lastOther);

        if (RisingEdge(Refresh, ref _lastRefresh))
            _ = RefreshAsync(force: true);
        if (RisingEdge(Action, ref _lastAction))
            _ = ActAsync();
    }

    private void HandleFamilyTrigger(
        bool current,
        HdeModelFamily family,
        ref bool previous)
    {
        if (!RisingEdge(current, ref previous)) return;
        lock (_stateLock)
        {
            _family = family;
            SelectModelLocked();
            UpdateViewLocked();
        }
    }

    private static bool RisingEdge(bool current, ref bool previous)
    {
        var rising = current && !previous;
        previous = current;
        return rising;
    }

    private async Task RefreshAsync(bool force)
    {
        if (Interlocked.CompareExchange(ref _refreshing, 1, 0) != 0) return;
        try
        {
            SetStatus(force ? "Refreshing model catalog..." : "Loading model catalog...");
            UpdateTarget();
            var snapshot = await VlModelCatalogService
                .RefreshAsync(force, _lifetime.Token)
                .ConfigureAwait(false);
            var client = await NodeToolClientProvider
                .GetApiClientAsync(_lifetime.Token)
                .ConfigureAwait(false);
            var scope = CurrentDownloadScope();
            if (_downloads == null ||
                !string.Equals(_downloadScope, scope, StringComparison.Ordinal))
            {
                _monitorCancellation?.Cancel();
                _monitorCancellation?.Dispose();
                _monitorCancellation = null;
                _downloads = new ModelDownloadService(client);
                _downloadScope = scope;
            }
            var downloadSnapshot = await _downloads
                .RefreshAsync(_lifetime.Token)
                .ConfigureAwait(false);

            if (_disposed) return;

            lock (_stateLock)
            {
                _catalog = snapshot;
                SelectModelLocked(downloadSnapshot);
                UpdateViewLocked();
            }
            StartMonitorIfNeeded();
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            SetStatus($"Error: {VlLog.SafeError(exception, NodeToolClientProvider.CurrentAuthToken)}");
        }
        finally
        {
            Interlocked.Exchange(ref _refreshing, 0);
        }
    }

    private async Task ActAsync()
    {
        if (Interlocked.CompareExchange(ref _acting, 1, 0) != 0) return;
        try
        {
            ModelDescriptor? model;
            ModelDownloadState? download;
            ModelDownloadService? service;
            lock (_stateLock)
            {
                model = _selected;
                download = _selectedDownload;
                service = _downloads;
            }
            if (model == null || service == null) return;

            ModelDownloadState next;
            if (download is { IsTerminal: false })
            {
                next = await service.CancelAsync(
                    download.OperationId,
                    _lifetime.Token).ConfigureAwait(false);
            }
            else if (download is
                     { Status: SdkModelDownloadStatuses.Error or SdkModelDownloadStatuses.Cancelled })
            {
                next = await service.RetryAsync(
                    download,
                    _lifetime.Token).ConfigureAwait(false);
            }
            else
            {
                if (model.IsReady ||
                    model.Availability != SdkModelAvailability.Downloadable ||
                    string.IsNullOrWhiteSpace(model.RepositoryId))
                {
                    return;
                }
                next = await service.StartAsync(
                    model,
                    _lifetime.Token).ConfigureAwait(false);
            }

            if (_disposed) return;

            lock (_stateLock)
            {
                _selectedDownload = next;
                UpdateViewLocked();
            }
            StartMonitorIfNeeded();
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            SetStatus($"Error: {VlLog.SafeError(exception, NodeToolClientProvider.CurrentAuthToken)}");
        }
        finally
        {
            Interlocked.Exchange(ref _acting, 0);
        }
    }

    private void StartMonitorIfNeeded()
    {
        if (_disposed) return;

        ModelDownloadState? download;
        ModelDownloadService? service;
        lock (_stateLock)
        {
            download = _selectedDownload;
            service = _downloads;
        }
        if (service == null || download == null || download.IsTerminal) return;

        _monitorCancellation?.Cancel();
        _monitorCancellation?.Dispose();
        _monitorCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            _lifetime.Token);
        _ = MonitorAsync(service, download.OperationId, _monitorCancellation.Token);
    }

    private async Task MonitorAsync(
        ModelDownloadService service,
        string operationId,
        CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var update in service.MonitorAsync(
                               operationId,
                               PollInterval,
                               cancellationToken).ConfigureAwait(false))
            {
                lock (_stateLock)
                {
                    if (_selectedDownload?.OperationId == operationId)
                    {
                        _selectedDownload = update;
                        UpdateViewLocked();
                    }
                }
            }
            await RefreshAsync(force: true).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            SetStatus($"Download error: {VlLog.SafeError(exception, NodeToolClientProvider.CurrentAuthToken)}");
        }
    }

    private void SelectModelLocked(ModelDownloadSnapshot? downloads = null)
    {
        var candidates = _catalog.Models
            .Where(model => HdeModelFamilyClassifier.Classify(model.Compatibility) == _family)
            .ToArray();
        _selected = candidates
            .OrderBy(ModelRank)
            .ThenBy(model => model.DisplayName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        var downloadItems = downloads?.Downloads ?? _downloads?.Snapshot.Downloads ?? [];
        _selectedDownload = _selected == null
            ? null
            : downloadItems
                .Where(download => DownloadMatches(_selected, download))
                .OrderByDescending(download => download.UpdatedAt)
                .FirstOrDefault();
    }

    private static int ModelRank(ModelDescriptor model)
        => model switch
        {
            { Recommended: true, Availability: SdkModelAvailability.Downloadable } => 0,
            { Recommended: true, IsReady: true } => 1,
            { IsReady: true } => 2,
            { Availability: SdkModelAvailability.Downloadable } => 3,
            _ => 4
        };

    private static bool DownloadMatches(
        ModelDescriptor model,
        ModelDownloadState download)
        => !string.IsNullOrWhiteSpace(model.RepositoryId) &&
           string.Equals(model.RepositoryId, download.RepositoryId, StringComparison.Ordinal) &&
           string.Equals(model.Compatibility, download.ModelType, StringComparison.Ordinal) &&
           string.Equals(model.Path ?? "", download.Path ?? "", StringComparison.Ordinal);

    private void UpdateViewLocked()
    {
        var familyModels = _catalog.Models
            .Where(model => HdeModelFamilyClassifier.Classify(model.Compatibility) == _family)
            .ToArray();
        var ready = familyModels.Count(model => model.IsReady);
        var downloadable = familyModels.Count(model =>
            model.Availability == SdkModelAvailability.Downloadable);
        Family = FamilyLabel(_family);
        Status = _catalog.LastError is { Length: > 0 } error
            ? $"Catalog warning: {error}"
            : $"{Family} · {familyModels.Length} models · {ready} ready · {downloadable} downloadable";

        if (_selected == null)
        {
            Model = "No models in this category";
            ActionLabel = "Unavailable";
            Progress = 0f;
            ProgressText = "";
            CanAct = false;
            return;
        }

        var recommended = _selected.Recommended ? " · recommended" : "";
        Model = $"{_selected.DisplayName}{recommended} · {_selected.Compatibility} · {_selected.Availability}";

        if (_selectedDownload is { IsTerminal: false } running)
        {
            ActionLabel = "Cancel";
            CanAct = true;
            SetProgress(running);
            return;
        }
        if (_selectedDownload is
            { Status: SdkModelDownloadStatuses.Error or SdkModelDownloadStatuses.Cancelled } failed)
        {
            ActionLabel = "Retry";
            CanAct = true;
            SetProgress(failed);
            return;
        }

        Progress = 0f;
        ProgressText = "";
        if (_selected.IsReady)
        {
            ActionLabel = "Ready";
            CanAct = false;
        }
        else if (_selected.Availability == SdkModelAvailability.Downloadable &&
                 !string.IsNullOrWhiteSpace(_selected.RepositoryId))
        {
            ActionLabel = "Download";
            CanAct = true;
        }
        else
        {
            ActionLabel = "Unavailable";
            CanAct = false;
        }
    }

    private void SetProgress(ModelDownloadState download)
    {
        Progress = (float)(download.Progress ?? 0d);
        var progress = download.Progress.HasValue
            ? $"{download.Progress.Value:P0} · {FormatBytes(download.DownloadedBytes)} / {FormatBytes(download.TotalBytes)}"
            : download.Status;
        ProgressText = string.IsNullOrWhiteSpace(download.Error)
            ? progress
            : $"{progress} · {download.Error}";
    }

    private void SetStatus(string value)
    {
        lock (_stateLock)
            Status = value;
    }

    private void UpdateTarget()
    {
        var endpoint = NodeToolClientProvider.CurrentApiBaseUrl?.AbsoluteUri.TrimEnd('/')
                       ?? "http://127.0.0.1:7777";
        lock (_stateLock)
            Target = $"Target: {endpoint} · local";
    }

    private static string CurrentDownloadScope()
    {
        var endpoint = NodeToolClientProvider.CurrentApiBaseUrl?.AbsoluteUri ?? "";
        var principal = string.IsNullOrWhiteSpace(NodeToolClientProvider.CurrentAuthToken)
            ? "trusted-local"
            : "authenticated";
        return $"{endpoint}|{principal}|local";
    }

    private static string FamilyLabel(HdeModelFamily family)
        => family == HdeModelFamily.Video3D ? "Video / 3D" : family.ToString();

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0) return "0 B";
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var value = (double)bytes;
        var unit = 0;
        while (value >= 1024d && unit < units.Length - 1)
        {
            value /= 1024d;
            unit++;
        }
        return $"{value:0.#} {units[unit]}";
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _lifetime.Cancel();
        _monitorCancellation?.Cancel();
        _monitorCancellation?.Dispose();
        _lifetime.Dispose();
    }
}

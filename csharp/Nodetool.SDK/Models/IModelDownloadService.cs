namespace Nodetool.SDK.Models;

public interface IModelDownloadService
{
    ModelDownloadSnapshot Snapshot { get; }

    Task<ModelDownloadState> StartAsync(
        ModelDescriptor model,
        CancellationToken cancellationToken = default);

    Task<ModelDownloadState> RetryAsync(
        ModelDownloadState download,
        CancellationToken cancellationToken = default);

    Task<ModelDownloadState> CancelAsync(
        string operationId,
        CancellationToken cancellationToken = default);

    Task<ModelDownloadSnapshot> RefreshAsync(
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<ModelDownloadState> MonitorAsync(
        string operationId,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default);
}

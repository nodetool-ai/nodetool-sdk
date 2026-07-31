using Nodetool.SDK.Api.Models;

namespace Nodetool.SDK.Models;

public interface IModelDownloadClient
{
    Task<SdkModelDownloadStateResponse> StartModelDownloadAsync(
        SdkModelDownloadStartRequest request,
        CancellationToken cancellationToken = default);

    Task<SdkModelDownloadSnapshotResponse> GetModelDownloadsAsync(
        SdkModelDownloadQuery? query = null,
        CancellationToken cancellationToken = default);

    Task<SdkModelDownloadStateResponse> CancelModelDownloadAsync(
        string operationId,
        CancellationToken cancellationToken = default);
}

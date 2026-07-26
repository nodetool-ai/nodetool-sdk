using Nodetool.SDK.Api.Models;

namespace Nodetool.SDK.Execution;

/// <summary>
/// Supplies a connected execution client and its matching HTTP connection
/// profile. The connection owns the client; workflow runtimes never dispose
/// it.
/// </summary>
public interface INodeToolExecutionConnection
{
    Uri? ApiBaseUrl { get; }
    string? AuthToken { get; }

    Task<SdkCapabilitiesResponse> GetSdkCapabilitiesAsync(
        CancellationToken cancellationToken = default);

    Task<INodeToolExecutionClient> GetConnectedClientAsync(
        CancellationToken cancellationToken = default);
}

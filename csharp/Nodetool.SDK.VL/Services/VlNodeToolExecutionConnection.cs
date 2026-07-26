using Nodetool.SDK.Api.Models;
using Nodetool.SDK.Execution;

namespace Nodetool.SDK.VL.Services;

/// <summary>
/// Adapts the VL package's shared connection node to the portable execution
/// runtime. The provider retains ownership of its shared client.
/// </summary>
internal sealed class VlNodeToolExecutionConnection :
    INodeToolExecutionConnection
{
    public Uri? ApiBaseUrl => NodeToolClientProvider.CurrentApiBaseUrl;
    public string? AuthToken => NodeToolClientProvider.CurrentAuthToken;

    public Task<SdkCapabilitiesResponse> GetSdkCapabilitiesAsync(
        CancellationToken cancellationToken = default)
        => NodeToolClientProvider.GetSdkCapabilitiesAsync(
            cancellationToken);

    public async Task<INodeToolExecutionClient> GetConnectedClientAsync(
        CancellationToken cancellationToken = default)
    {
        if (!NodeToolClientProvider.IsConnected)
        {
            var connected = await NodeToolClientProvider.ConnectAsync(
                cancellationToken);
            if (!connected)
            {
                throw new InvalidOperationException(
                    NodeToolClientProvider.LastError ??
                    "Failed to connect to NodeTool server.");
            }
        }

        return NodeToolClientProvider.GetClient();
    }
}

using Nodetool.SDK.Api;
using Nodetool.SDK.Workflows;

namespace Nodetool.SDK.Execution;

/// <summary>
/// Creates a host-neutral workflow controller from an authoritative workflow
/// descriptor and one connection profile.
/// </summary>
public static class WorkflowExecutionControllerFactory
{
    public static WorkflowExecutionController Create(
        INodeToolExecutionClient executionClient,
        WorkflowDescriptor workflow,
        Uri? apiBaseUrl = null,
        string? authToken = null,
        HttpClient? httpClient = null,
        Func<CancellationToken, Task<Api.Models.SdkCapabilitiesResponse>>?
            capabilitiesProvider = null)
    {
        ArgumentNullException.ThrowIfNull(executionClient);
        ArgumentNullException.ThrowIfNull(workflow);

        Func<CancellationToken, Task<Api.Models.SdkCapabilitiesResponse>>?
            getCapabilities = capabilitiesProvider ?? (
                apiBaseUrl == null
                ? null
                : async cancellationToken =>
                {
                    using var client = new NodetoolClient(
                        apiBaseUrl,
                        authToken,
                        httpClient);
                    return await client.GetSdkCapabilitiesAsync(
                        cancellationToken);
                });

        return new WorkflowExecutionController(
            executionClient,
            workflow.Outputs,
            getCapabilities);
    }
}

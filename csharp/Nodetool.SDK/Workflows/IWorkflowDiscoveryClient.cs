using Nodetool.SDK.Api.Models;

namespace Nodetool.SDK.Workflows;

/// <summary>
/// Transport-neutral workflow discovery operations shared by HTTP and
/// WebSocket clients.
/// </summary>
public interface IWorkflowDiscoveryClient
{
    Task<List<WorkflowSummaryResponse>> GetWorkflowSummariesAsync(
        CancellationToken cancellationToken = default);

    Task<WorkflowInterfaceResponse> GetWorkflowInterfaceAsync(
        string workflowId,
        CancellationToken cancellationToken = default);

    Task<WorkflowInterfacesResponse> GetWorkflowInterfacesAsync(
        IReadOnlyCollection<string> workflowIds,
        CancellationToken cancellationToken = default);
}

namespace Nodetool.SDK.Workflows;

public interface IWorkflowCatalog
{
    WorkflowCatalogSnapshot Snapshot { get; }

    Task<WorkflowCatalogSnapshot> RefreshAsync(
        bool force = false,
        CancellationToken cancellationToken = default);

    WorkflowDescriptor? GetById(string workflowId);

    void Clear();
}

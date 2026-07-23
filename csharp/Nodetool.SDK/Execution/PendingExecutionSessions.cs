namespace Nodetool.SDK.Execution;

/// <summary>
/// Thread-safe FIFO binding store for sessions waiting for a server job id.
/// </summary>
internal sealed class PendingExecutionSessions
{
    private readonly object _lock = new();
    private readonly Dictionary<string, Queue<ExecutionSession>> _byWorkflowId =
        new(StringComparer.Ordinal);

    public void Add(string workflowId, ExecutionSession session)
    {
        lock (_lock)
        {
            if (!_byWorkflowId.TryGetValue(workflowId, out var queue))
            {
                queue = new Queue<ExecutionSession>();
                _byWorkflowId[workflowId] = queue;
            }
            queue.Enqueue(session);
        }
    }

    public bool TryTake(
        string? workflowId,
        out ExecutionSession? session,
        out string? matchedWorkflowId)
    {
        lock (_lock)
        {
            if (!string.IsNullOrWhiteSpace(workflowId) &&
                TryDequeue(workflowId, out session))
            {
                matchedWorkflowId = workflowId;
                return true;
            }

            var pending = _byWorkflowId
                .Where(item => item.Value.Count > 0)
                .Take(2)
                .ToArray();
            if (pending.Length == 1 && pending[0].Value.Count == 1)
            {
                matchedWorkflowId = pending[0].Key;
                return TryDequeue(matchedWorkflowId, out session);
            }

            session = null;
            matchedWorkflowId = null;
            return false;
        }
    }

    public void Remove(string workflowId, ExecutionSession session)
    {
        lock (_lock)
        {
            if (!_byWorkflowId.TryGetValue(workflowId, out var queue))
                return;
            var remaining = queue.Where(item => !ReferenceEquals(item, session));
            var replacement = new Queue<ExecutionSession>(remaining);
            if (replacement.Count == 0)
                _byWorkflowId.Remove(workflowId);
            else
                _byWorkflowId[workflowId] = replacement;
        }
    }

    public IReadOnlyList<ExecutionSession> Drain()
    {
        lock (_lock)
        {
            var sessions = _byWorkflowId.Values.SelectMany(queue => queue).ToArray();
            _byWorkflowId.Clear();
            return sessions;
        }
    }

    private bool TryDequeue(string workflowId, out ExecutionSession? session)
    {
        if (!_byWorkflowId.TryGetValue(workflowId, out var queue) ||
            !queue.TryDequeue(out session))
        {
            session = null;
            return false;
        }
        if (queue.Count == 0)
            _byWorkflowId.Remove(workflowId);
        return true;
    }
}

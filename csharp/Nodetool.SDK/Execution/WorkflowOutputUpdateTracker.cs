namespace Nodetool.SDK.Execution;

/// <summary>
/// Selects output values that became newer since the previous host snapshot.
/// This prevents frame-based hosts from repeatedly materializing unchanged
/// media or structured outputs.
/// </summary>
public sealed class WorkflowOutputUpdateTracker
{
    private readonly Dictionary<string, DateTimeOffset> _applied =
        new(StringComparer.Ordinal);

    public IReadOnlyList<WorkflowOutputState> SelectChanges(
        WorkflowExecutionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var changed = new List<WorkflowOutputState>();
        foreach (var output in snapshot.Outputs.Values)
        {
            if (_applied.TryGetValue(output.PublicName, out var appliedAt) &&
                appliedAt >= output.UpdatedAt)
            {
                continue;
            }
            _applied[output.PublicName] = output.UpdatedAt;
            changed.Add(output);
        }
        return changed;
    }

    public void Reset() => _applied.Clear();
}

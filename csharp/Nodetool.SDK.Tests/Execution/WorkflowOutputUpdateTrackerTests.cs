using Nodetool.SDK.Execution;
using Nodetool.SDK.Values;

namespace Nodetool.SDK.Tests.Execution;

public sealed class WorkflowOutputUpdateTrackerTests
{
    [Fact]
    public void SelectChanges_ReturnsOnlyNewerOutputValues()
    {
        var tracker = new WorkflowOutputUpdateTracker();
        var firstTime = DateTimeOffset.Parse("2026-07-26T00:00:00Z");
        var secondTime = firstTime.AddSeconds(1);

        var first = Snapshot(new WorkflowOutputState(
            "value",
            NodeToolValue.From(1),
            IsStreaming: false,
            Done: false,
            firstTime));
        Assert.Single(tracker.SelectChanges(first));
        Assert.Empty(tracker.SelectChanges(first));

        var second = Snapshot(new WorkflowOutputState(
            "value",
            NodeToolValue.From(2),
            IsStreaming: false,
            Done: true,
            secondTime));
        Assert.Single(tracker.SelectChanges(second));
        Assert.Empty(tracker.SelectChanges(first));
    }

    [Fact]
    public void Reset_AllowsCurrentSnapshotToBeReapplied()
    {
        var tracker = new WorkflowOutputUpdateTracker();
        var snapshot = Snapshot(new WorkflowOutputState(
            "value",
            NodeToolValue.From("hello"),
            IsStreaming: false,
            Done: true,
            DateTimeOffset.UtcNow));

        Assert.Single(tracker.SelectChanges(snapshot));
        tracker.Reset();
        Assert.Single(tracker.SelectChanges(snapshot));
    }

    private static WorkflowExecutionSnapshot Snapshot(
        WorkflowOutputState output)
        => WorkflowExecutionSnapshot.Idle with
        {
            Outputs = new Dictionary<string, WorkflowOutputState>
            {
                [output.PublicName] = output
            }
        };
}

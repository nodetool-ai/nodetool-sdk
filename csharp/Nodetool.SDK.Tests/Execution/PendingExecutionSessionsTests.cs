using Nodetool.SDK.Execution;

namespace Nodetool.SDK.Tests.Execution;

public class PendingExecutionSessionsTests
{
    [Fact]
    public void SameWorkflowRunsAreBoundInCreationOrder()
    {
        var pending = new PendingExecutionSessions();
        using var first = new ExecutionSession("", "workflow-1");
        using var second = new ExecutionSession("", "workflow-1");
        pending.Add("workflow-1", first);
        pending.Add("workflow-1", second);

        Assert.True(pending.TryTake("workflow-1", out var firstResult, out _));
        Assert.Same(first, firstResult);
        Assert.True(pending.TryTake("workflow-1", out var secondResult, out _));
        Assert.Same(second, secondResult);
    }

    [Fact]
    public void UnscopedUpdateDoesNotGuessWhenMultipleSessionsArePending()
    {
        var pending = new PendingExecutionSessions();
        using var first = new ExecutionSession("", "workflow-1");
        using var second = new ExecutionSession("", "workflow-2");
        pending.Add("workflow-1", first);
        pending.Add("workflow-2", second);

        Assert.False(pending.TryTake(null, out var result, out var workflowId));
        Assert.Null(result);
        Assert.Null(workflowId);
    }

    [Fact]
    public void FailedSendCanRemoveOnlyItsOwnPendingSession()
    {
        var pending = new PendingExecutionSessions();
        using var first = new ExecutionSession("", "workflow-1");
        using var second = new ExecutionSession("", "workflow-1");
        pending.Add("workflow-1", first);
        pending.Add("workflow-1", second);

        pending.Remove("workflow-1", first);

        Assert.True(pending.TryTake("workflow-1", out var result, out _));
        Assert.Same(second, result);
    }
}

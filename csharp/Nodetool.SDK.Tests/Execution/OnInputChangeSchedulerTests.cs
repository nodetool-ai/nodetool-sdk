using Nodetool.SDK.Utilities.Execution;

namespace Nodetool.SDK.Tests.Execution;

public sealed class OnInputChangeSchedulerTests
{
    [Fact]
    public void UnchangedSignature_DoesNothing()
    {
        var scheduler = new OnInputChangeScheduler();
        scheduler.Reset("same");

        var action = scheduler.NotifyInputs(
            "same",
            isRunning: false,
            restartOnChange: false);

        Assert.Equal(OnInputChangeAction.None, action);
        Assert.False(scheduler.RerunRequested);
    }

    [Fact]
    public void ChangedIdleSignature_StartsImmediately()
    {
        var scheduler = new OnInputChangeScheduler();
        scheduler.Reset("before");

        var action = scheduler.NotifyInputs(
            "after",
            isRunning: false,
            restartOnChange: false);

        Assert.Equal(OnInputChangeAction.Start, action);
        Assert.False(scheduler.RerunRequested);
    }

    [Theory]
    [InlineData(false, OnInputChangeAction.QueueRerun)]
    [InlineData(true, OnInputChangeAction.CancelAndRestart)]
    public void ChangedRunningSignature_CoalescesLatestRerun(
        bool restartOnChange,
        OnInputChangeAction expected)
    {
        var scheduler = new OnInputChangeScheduler();
        scheduler.Reset("before");

        var action = scheduler.NotifyInputs(
            "after",
            isRunning: true,
            restartOnChange);

        Assert.Equal(expected, action);
        Assert.True(scheduler.ConsumeRerunRequested());
        Assert.False(scheduler.ConsumeRerunRequested());
    }
}

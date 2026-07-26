using Nodetool.SDK.VL.Utilities;
using Xunit;

namespace Nodetool.SDK.VL.UnitTests;

public class VlReadinessStateTests
{
    [Fact]
    public void Readiness_IsEmittedOnceAfterEveryMilestone()
    {
        var info = new List<string>();
        var errors = new List<string>();
        var state = new VlReadinessState(info.Add, errors.Add);

        state.MarkRegistered();
        state.MarkNodeDiscovery(2527);
        state.MarkWorkflowDiscovery(76, "WebSocket");
        state.MarkNodeFactoryResolved();

        Assert.Empty(info);

        state.MarkWorkflowFactoryResolved();
        state.MarkWorkflowFactoryResolved();
        state.MarkNodeDiscovery(2527);

        var message = Assert.Single(info);
        Assert.Equal(
            "ready: connection and discovery resolved; 2527 nodes, " +
            "76 workflows via WebSocket; factories ready.",
            message);
        Assert.Empty(errors);
    }

    [Fact]
    public void Errors_AreDeduplicatedUntilSuccessOrReset()
    {
        var info = new List<string>();
        var errors = new List<string>();
        var state = new VlReadinessState(info.Add, errors.Add);

        state.ReportError("node discovery", "Server unavailable.");
        state.ReportError("node discovery", "Server unavailable.");
        state.MarkNodeDiscovery(3);
        state.ReportError("node discovery", "Server unavailable.");
        state.Reset();
        state.ReportError("node discovery", "Server unavailable.");

        Assert.Equal(
            new[]
            {
                "node discovery: Server unavailable.",
                "node discovery: Server unavailable.",
                "node discovery: Server unavailable."
            },
            errors);
        Assert.Empty(info);
    }
}

using Nodetool.SDK.VL.Utilities;
using Xunit;

namespace Nodetool.SDK.VL.UnitTests;

public sealed class ExecutionPinVisibilityTests
{
    [Theory]
    [InlineData("Cancel")]
    [InlineData("AutoRun")]
    [InlineData("RestartOnChange")]
    [InlineData("ExecutionTimeoutSeconds")]
    public void GenericControlInputs_AreHiddenByDefault(string name)
        => Assert.False(ExecutionPinVisibility.IsInputVisible(name));

    [Theory]
    [InlineData("Run")]
    [InlineData("prompt")]
    public void PrimaryAndDataInputs_RemainVisible(string name)
        => Assert.True(ExecutionPinVisibility.IsInputVisible(name));

    [Fact]
    public void WorkflowPrimaryInput_IsNamedRun()
        => Assert.Equal(
            "Run",
            Nodetool.SDK.VL.Nodes.WorkflowNodeDescription.RunInputName);

    [Theory]
    [InlineData("Error")]
    [InlineData("Debug")]
    [InlineData("Execution Time")]
    public void DiagnosticOutputs_AreHiddenByDefault(string name)
        => Assert.False(ExecutionPinVisibility.IsOutputVisible(name));

    [Theory]
    [InlineData("IsRunning")]
    [InlineData("On Update")]
    [InlineData("result")]
    public void StatusAndDataOutputs_RemainVisible(string name)
        => Assert.True(ExecutionPinVisibility.IsOutputVisible(name));
}

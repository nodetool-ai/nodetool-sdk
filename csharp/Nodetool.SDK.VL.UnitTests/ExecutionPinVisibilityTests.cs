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
    [InlineData("Trigger")]
    [InlineData("Execute")]
    [InlineData("prompt")]
    public void PrimaryAndDataInputs_RemainVisible(string name)
        => Assert.True(ExecutionPinVisibility.IsInputVisible(name));

    [Theory]
    [InlineData("Error")]
    [InlineData("Debug")]
    public void DiagnosticOutputs_AreHiddenByDefault(string name)
        => Assert.False(ExecutionPinVisibility.IsOutputVisible(name));

    [Theory]
    [InlineData("IsRunning")]
    [InlineData("On Update")]
    [InlineData("result")]
    public void StatusAndDataOutputs_RemainVisible(string name)
        => Assert.True(ExecutionPinVisibility.IsOutputVisible(name));
}

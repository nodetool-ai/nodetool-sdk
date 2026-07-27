using Nodetool.SDK.Execution;

namespace Nodetool.SDK.Tests.Execution;

public sealed class WorkflowExecutionOptionsTests
{
    [Fact]
    public void SdkDefault_DisablesGeneratedAssetAutosave()
    {
        var options = new WorkflowExecutionOptions();

        Assert.Equal(
            WorkflowAssetPersistence.Temporary,
            options.AssetPersistence);
    }
}

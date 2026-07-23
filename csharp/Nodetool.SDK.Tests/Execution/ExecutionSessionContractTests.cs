using Nodetool.SDK.Execution;
using Nodetool.SDK.Types;

namespace Nodetool.SDK.Tests.Execution;

public class ExecutionSessionContractTests
{
    [Fact]
    public async Task CompletedJob_UnwrapsCurrentResultOutputsEnvelope()
    {
        using var session = new ExecutionSession("job-1", "workflow-1");
        session.ProcessJobUpdate(new JobUpdate
        {
            status = "completed",
            job_id = "job-1",
            result = new Dictionary<string, object>
            {
                ["outputs"] = new Dictionary<string, object>
                {
                    ["answer"] = new object[] { 42L }
                }
            }
        });

        Assert.True(await session.WaitForCompletionAsync());
        var output = session.GetLatestOutput("job_result", "answer");
        Assert.NotNull(output);
        Assert.True(output.AsListOrEmpty().Single().TryGetLong(out var answer));
        Assert.Equal(42L, answer);
        Assert.Null(session.GetLatestOutput("job_result", "outputs"));
    }

    [Fact]
    public void OutputUpdate_ModelsCurrentStreamingFields()
    {
        var update = new OutputUpdate { disposition = "replace", done = true };

        Assert.Equal("replace", update.disposition);
        Assert.True(update.done);
    }

    [Fact]
    public void OutputUpdate_PreservesDispositionAndDoneForConsumers()
    {
        using var session = new ExecutionSession("job-1", "workflow-1");
        ExecutionOutputUpdate? received = null;
        session.OutputReceived += update => received = update;

        session.ProcessOutputUpdate(new OutputUpdate
        {
            job_id = "job-1",
            node_id = "output-1",
            output_name = "text",
            output_type = "chunk",
            value = new Dictionary<string, object>
            {
                ["type"] = "chunk",
                ["content"] = "hello"
            },
            disposition = "replace",
            done = true
        });

        Assert.NotNull(received);
        Assert.Equal("replace", received.Disposition);
        Assert.True(received.Done);
    }

    [Fact]
    public void OutputUpdate_DefaultsMissingDispositionToAppend()
    {
        using var session = new ExecutionSession("job-1", "workflow-1");
        ExecutionOutputUpdate? received = null;
        session.OutputReceived += update => received = update;

        session.ProcessOutputUpdate(new OutputUpdate
        {
            job_id = "job-1",
            node_id = "output-1",
            output_name = "text",
            value = "hello"
        });

        Assert.NotNull(received);
        Assert.Equal("append", received.Disposition);
        Assert.False(received.Done);
    }

    [Fact]
    public void ExecutionUpdates_IgnoreForeignJobIds()
    {
        using var session = new ExecutionSession("job-1", "workflow-1");
        var outputCount = 0;
        var nodeCount = 0;
        var progressCount = 0;
        var previewCount = 0;
        session.OutputReceived += _ => outputCount++;
        session.NodeUpdated += _ => nodeCount++;
        session.ProgressChanged += _ => progressCount++;
        session.PreviewReceived += _ => previewCount++;

        session.ProcessOutputUpdate(new OutputUpdate
        {
            job_id = "job-2",
            node_id = "node",
            output_name = "value"
        });
        session.ProcessNodeUpdate(new NodeUpdate { job_id = "job-2" });
        session.ProcessNodeProgress(new NodeProgress
        {
            job_id = "job-2",
            progress = 1,
            total = 2
        });
        session.ProcessPreviewUpdate(new PreviewUpdate { job_id = "job-2" });

        Assert.Equal(0, outputCount);
        Assert.Equal(0, nodeCount);
        Assert.Equal(0, progressCount);
        Assert.Equal(0, previewCount);
        Assert.Empty(session.GetLatestOutputs());
    }
}

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
}

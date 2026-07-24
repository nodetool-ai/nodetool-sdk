using Nodetool.SDK.Execution;
using Nodetool.SDK.Types;
using Nodetool.SDK.Configuration;

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
    public async Task ProvisionalCompletion_WaitsForAuthoritativeResult()
    {
        using var session = new ExecutionSession("job-1", "workflow-1");
        session.ProcessJobUpdate(new JobUpdate
        {
            status = "completed",
            job_id = "job-1"
        });

        Assert.False(session.IsCompleted);
        Assert.True(session.IsRunning);
        Assert.Equal("finalizing", session.CurrentStatus);

        session.ProcessJobUpdate(new JobUpdate
        {
            status = "completed",
            job_id = "job-1",
            result = new Dictionary<string, object>
            {
                ["outputs"] = new Dictionary<string, object>()
            }
        });

        Assert.True(await session.WaitForCompletionAsync());
        Assert.True(session.IsCompleted);
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

    [Fact]
    public void EmptyJobId_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => new ExecutionSession("", "workflow-1"));
    }

    [Fact]
    public async Task Cancellation_IsSentExactlyOnceWithPreboundJobId()
    {
        using var session = new ExecutionSession("job-1", "workflow-1");
        var cancellations = new List<(string JobId, string? WorkflowId)>();
        session.CancelAction = (jobId, workflowId, _) =>
        {
            cancellations.Add((jobId, workflowId));
            return Task.CompletedTask;
        };

        await session.CancelAsync();
        await session.CancelAsync();

        var cancellation = Assert.Single(cancellations);
        Assert.Equal("job-1", cancellation.JobId);
        Assert.Equal("workflow-1", cancellation.WorkflowId);
    }

    [Fact]
    public async Task CancellingAWait_DoesNotReportAJobFailure()
    {
        using var session = new ExecutionSession("job-1", "workflow-1");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => session.WaitForCompletionAsync(cancellation.Token));

        Assert.False(session.IsCompleted);
        Assert.Null(session.ErrorMessage);
    }

    [Fact]
    public void ScopedUpdates_DoNotCrossRouteBetweenConcurrentSessions()
    {
        using var client = new NodeToolExecutionClient(new NodeToolClientOptions
        {
            WorkerWebSocketUrl = new Uri("ws://127.0.0.1:7777/ws")
        });
        var first = client.CreateSession("job-1", "workflow-1");
        var second = client.CreateSession("job-2", "workflow-2");

        client.RouteExecutionMessage(new OutputUpdate
        {
            job_id = "job-1",
            node_id = "output",
            output_name = "value",
            value = "first"
        });

        Assert.Equal("first", first.GetLatestOutput("output", "value")?.AsString());
        Assert.Null(second.GetLatestOutput("output", "value"));
    }

    [Fact]
    public void UnscopedUpdates_AreDroppedUntilOnlyOneActiveSessionRemains()
    {
        using var client = new NodeToolExecutionClient(new NodeToolClientOptions
        {
            WorkerWebSocketUrl = new Uri("ws://127.0.0.1:7777/ws")
        });
        var first = client.CreateSession("job-1", "workflow-1");
        var second = client.CreateSession("job-2", "workflow-2");
        var unscoped = new OutputUpdate
        {
            node_id = "output",
            output_name = "value",
            value = "only-active"
        };

        client.RouteExecutionMessage(unscoped);
        Assert.Null(first.GetLatestOutput("output", "value"));
        Assert.Null(second.GetLatestOutput("output", "value"));

        client.RouteExecutionMessage(new JobUpdate
        {
            job_id = "job-2",
            status = "completed",
            result = new Dictionary<string, object>
            {
                ["outputs"] = new Dictionary<string, object>()
            }
        });
        client.RouteExecutionMessage(unscoped);

        Assert.Equal("only-active", first.GetLatestOutput("output", "value")?.AsString());
        Assert.Null(second.GetLatestOutput("output", "value"));
    }
}

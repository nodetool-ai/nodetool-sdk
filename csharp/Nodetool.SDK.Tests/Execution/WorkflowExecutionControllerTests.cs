using Nodetool.SDK.Api.Models;
using Nodetool.SDK.Execution;
using Nodetool.SDK.Types;
using Nodetool.SDK.Workflows;

namespace Nodetool.SDK.Tests.Execution;

public class WorkflowExecutionControllerTests
{
    [Fact]
    public async Task ForwardsExplicitExecutionOptionsToPortableClient()
    {
        using var session = new ExecutionSession("job-options", "workflow-options");
        using var client = new FakeExecutionClient(session);
        await using var controller = CreateController(
            client,
            _ => Task.FromResult(SupportedCapabilities()));

        var executionOptions = new WorkflowExecutionOptions(
            WorkflowPersistence.Session,
            WorkflowEventDetail.Outputs,
            WorkflowAssetPersistence.Temporary);
        await controller.StartAsync(new WorkflowInvocation(
            "workflow-options",
            new Dictionary<string, object?>(),
            ExecutionOptions: executionOptions));

        Assert.Equal(
            executionOptions,
            client.LastExecutionOptions);
    }

    [Fact]
    public async Task RejectsNonDefaultOptionsWithoutCapabilityNegotiation()
    {
        using var session = new ExecutionSession("job-options", "workflow-options");
        using var client = new FakeExecutionClient(session);
        await using var controller = CreateController(client);

        var snapshot = await controller.StartAsync(new WorkflowInvocation(
            "workflow-options",
            new Dictionary<string, object?>(),
            ExecutionOptions: new WorkflowExecutionOptions(
                WorkflowPersistence.Session)));

        Assert.Equal(WorkflowExecutionState.Failed, snapshot.State);
        Assert.Contains("capability", snapshot.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(client.Invocations);
    }

    [Fact]
    public async Task RoutesAndAccumulatesLiveOutputThenReconcilesTerminalResult()
    {
        using var session = new ExecutionSession("job-1", "workflow-1");
        using var client = new FakeExecutionClient(session);
        await using var controller = CreateController(client);
        var observed = new List<WorkflowExecutionSnapshot>();
        controller.SnapshotChanged += _ => throw new InvalidOperationException("host failure");
        controller.SnapshotChanged += observed.Add;

        await controller.StartAsync(new WorkflowInvocation("workflow-1"));
        session.ProcessOutputUpdate(Output(
            nodeId: "unrelated-node",
            value: "must-not-route"));
        session.ProcessOutputUpdate(Output(
            nodeId: "output-node",
            value: Chunk("hello ")));
        session.ProcessOutputUpdate(Output(
            nodeId: "output-node",
            value: Chunk("world")));
        session.ProcessOutputUpdate(Output(
            nodeId: "output-node",
            value: Chunk("replacement"),
            disposition: "replace",
            done: true));

        Assert.Equal(
            "replacement",
            controller.Snapshot.Outputs["answer"].Value.AsString());
        Assert.True(controller.Snapshot.Outputs["answer"].IsStreaming);
        Assert.True(controller.Snapshot.Outputs["answer"].Done);

        session.ProcessJobUpdate(new JobUpdate
        {
            job_id = "job-1",
            status = "completed",
            result = new Dictionary<string, object>
            {
                ["outputs"] = new Dictionary<string, object>
                {
                    ["answer"] = new object[] { "authoritative final" }
                }
            }
        });
        await controller.WaitForTerminalAsync();

        Assert.Equal(
            WorkflowExecutionState.Completed,
            controller.Snapshot.State);
        Assert.Equal(
            "authoritative final",
            controller.Snapshot.Outputs["answer"].Value.AsString());
        Assert.False(controller.Snapshot.Outputs["answer"].IsStreaming);
        Assert.True(controller.Snapshot.Outputs["answer"].Done);
        Assert.DoesNotContain(
            controller.Snapshot.Outputs.Values,
            output => output.Value.AsString() == "must-not-route");
        Assert.Contains(
            observed,
            snapshot => snapshot.State == WorkflowExecutionState.Completed);
    }

    [Fact]
    public async Task PublishesRawStreamUpdatesAlongsideAccumulatedText()
    {
        using var session = new ExecutionSession("job-1", "workflow-1");
        using var client = new FakeExecutionClient(session);
        await using var controller = CreateController(client);
        ExecutionStreamUpdate? received = null;
        controller.StreamReceived += _ =>
            throw new InvalidOperationException("host failure");
        controller.StreamReceived += update => received = update;

        await controller.StartAsync(new WorkflowInvocation("workflow-1"));
        session.ProcessOutputUpdate(Output(
            nodeId: "output-node",
            value: new Dictionary<string, object>
            {
                ["type"] = "chunk",
                ["content_type"] = "text",
                ["content"] = "delta",
                ["done"] = true
            }));

        Assert.NotNull(received);
        Assert.Equal("delta", received.Content.AsString());
        Assert.True(received.Done);
        Assert.Equal(
            "delta",
            controller.Snapshot.Outputs["answer"].Value.AsString());
    }

    [Fact]
    public async Task NonTextChunksRemainIndividualBlocks()
    {
        using var session = new ExecutionSession("job-1", "workflow-1");
        using var client = new FakeExecutionClient(session);
        await using var controller = CreateController(client);

        await controller.StartAsync(new WorkflowInvocation("workflow-1"));
        session.ProcessOutputUpdate(Output(
            nodeId: "output-node",
            value: AudioChunk("first")));
        session.ProcessOutputUpdate(Output(
            nodeId: "output-node",
            value: AudioChunk("second")));

        var latest = controller.Snapshot.Outputs["answer"].Value.AsMapOrEmpty();
        Assert.Equal("audio", latest["content_type"].AsString());
        Assert.Equal("second", latest["content"].AsString());
    }

    [Fact]
    public async Task MatchingTerminalMediaDoesNotRetriggerHostMaterialization()
    {
        using var session = new ExecutionSession("job-1", "workflow-1");
        using var client = new FakeExecutionClient(session);
        await using var controller = CreateController(client);
        var tracker = new WorkflowOutputUpdateTracker();
        var image = new Dictionary<string, object?>
        {
            ["type"] = "image",
            ["uri"] = "/api/storage/temp/roundtrip.png",
            ["data"] = null
        };

        await controller.StartAsync(new WorkflowInvocation("workflow-1"));
        session.ProcessOutputUpdate(Output("output-node", image));
        var streamed = controller.Snapshot.Outputs["answer"];
        Assert.Single(tracker.SelectChanges(controller.Snapshot));

        session.ProcessJobUpdate(new JobUpdate
        {
            job_id = "job-1",
            status = "completed",
            result = new Dictionary<string, object>
            {
                ["outputs"] = new Dictionary<string, object>
                {
                    ["answer"] = new object[] { image }
                }
            }
        });
        await controller.WaitForTerminalAsync();

        var terminal = controller.Snapshot.Outputs["answer"];
        Assert.Equal(streamed.UpdatedAt, terminal.UpdatedAt);
        Assert.False(terminal.IsStreaming);
        Assert.True(terminal.Done);
        Assert.Empty(tracker.SelectChanges(controller.Snapshot));
    }

    [Fact]
    public async Task RejectsOverlappingRunsAndCancelsRemoteSessionOnce()
    {
        using var session = new ExecutionSession("job-1", "workflow-1");
        var cancels = 0;
        session.CancelAction = (_, _, _) =>
        {
            Interlocked.Increment(ref cancels);
            session.ProcessJobUpdate(new JobUpdate
            {
                job_id = "job-1",
                status = "cancelled"
            });
            return Task.CompletedTask;
        };
        using var client = new FakeExecutionClient(session);
        await using var controller = CreateController(client);

        await controller.StartAsync(new WorkflowInvocation("workflow-1"));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => controller.StartAsync(new WorkflowInvocation("workflow-1")));

        await controller.CancelAsync();
        await controller.CancelAsync();
        await controller.WaitForTerminalAsync();

        Assert.Equal(1, cancels);
        Assert.Equal(
            WorkflowExecutionState.Cancelled,
            controller.Snapshot.State);
    }

    [Fact]
    public async Task TimeoutCancelsRemoteSessionAndReportsTimedOut()
    {
        using var session = new ExecutionSession("job-1", "workflow-1");
        var cancels = 0;
        session.CancelAction = (_, _, _) =>
        {
            Interlocked.Increment(ref cancels);
            return Task.CompletedTask;
        };
        using var client = new FakeExecutionClient(session);
        await using var controller = CreateController(client);

        await controller.StartAsync(new WorkflowInvocation(
            "workflow-1",
            new Dictionary<string, object?>(),
            TimeSpan.FromMilliseconds(25)));
        await controller.WaitForTerminalAsync();

        Assert.Equal(1, cancels);
        Assert.Equal(
            WorkflowExecutionState.TimedOut,
            controller.Snapshot.State);
        Assert.Contains("timed out", controller.Snapshot.Error);
    }

    [Fact]
    public async Task ManualCancellationAttemptsRemoteCancelOnlyOnce()
    {
        using var session = new ExecutionSession("job-1", "workflow-1");
        var cancels = 0;
        session.CancelAction = (_, _, _) =>
        {
            Interlocked.Increment(ref cancels);
            return Task.CompletedTask;
        };
        using var client = new FakeExecutionClient(session);
        await using var controller = CreateController(client);

        await controller.StartAsync(new WorkflowInvocation("workflow-1"));
        await controller.CancelAsync();
        await controller.WaitForTerminalAsync();

        Assert.Equal(1, cancels);
        Assert.Equal(
            WorkflowExecutionState.Cancelled,
            controller.Snapshot.State);
    }

    [Fact]
    public async Task RuntimeConnectsPreparesAndExecutesWithinOneLifecycle()
    {
        using var session = new ExecutionSession("job-runtime", "workflow-1");
        StreamInputData? streamedInput = null;
        session.StreamInputAction = (data, _) =>
        {
            streamedInput = data;
            return Task.CompletedTask;
        };
        using var client = new FakeExecutionClient(session);
        var connection = new FakeConnection(client);
        await using var runtime = new WorkflowExecutionRuntime(
            connection,
            CreateDescriptor());
        ExecutionStreamUpdate? streamedOutput = null;
        runtime.StreamReceived += update => streamedOutput = update;

        var run = runtime.ExecuteAsync(
            new Dictionary<string, object?> { ["prompt"] = "hello" },
            TimeSpan.FromSeconds(2));
        await WaitUntilAsync(() => client.Invocations.Count == 1);
        await runtime.StreamInputAsync("prompt", "live");
        session.ProcessOutputUpdate(new OutputUpdate
        {
            job_id = "job-runtime",
            node_id = "output-node",
            node_name = "answer",
            output_name = "answer",
            output_type = "chunk",
            value = new Dictionary<string, object>
            {
                ["type"] = "chunk",
                ["content_type"] = "text",
                ["content"] = "streamed"
            }
        });
        session.ProcessJobUpdate(Completed("job-runtime", "done"));
        var result = await run;

        Assert.Equal(1, connection.ConnectCalls);
        Assert.Equal("hello", client.Invocations[0].Inputs["prompt"]);
        Assert.Equal("live", streamedInput?.value);
        Assert.Equal("streamed", streamedOutput?.Content.AsString());
        Assert.Equal(
            WorkflowExecutionState.Completed,
            result.Snapshot.State);
        Assert.Equal("done", result.Snapshot.Outputs["answer"].Value.AsString());
        Assert.Same(result.Timing, runtime.LastTiming);
        await runtime.DisposeAsync();
        Assert.Equal(0, client.DisposeCalls);
    }

    [Fact]
    public async Task RuntimeRejectsObviousInvalidInputsBeforeConnecting()
    {
        using var session = new ExecutionSession(
            "job-runtime-invalid",
            "workflow-1");
        using var client = new FakeExecutionClient(session);
        var connection = new FakeConnection(client);
        var descriptor = CreateDescriptor() with
        {
            Inputs =
            [
                CreateDescriptor().Inputs[0] with
                {
                    Required = true
                }
            ]
        };
        await using var runtime = new WorkflowExecutionRuntime(
            connection,
            descriptor);

        var exception =
            await Assert.ThrowsAsync<WorkflowInputValidationException>(
                () => runtime.ExecuteAsync(
                    new Dictionary<string, object?>(),
                    TimeSpan.FromSeconds(2)));

        Assert.Equal(0, connection.ConnectCalls);
        Assert.Contains(
            exception.Issues,
            issue =>
                issue.Code ==
                WorkflowInputValidationCodes.MissingRequiredInput);
    }

    [Fact]
    public async Task RuntimeReplacesControllerWhenConnectionClientChanges()
    {
        using var firstSession =
            new ExecutionSession("job-runtime-1", "workflow-1");
        using var secondSession =
            new ExecutionSession("job-runtime-2", "workflow-1");
        using var firstClient = new FakeExecutionClient(firstSession);
        using var secondClient = new FakeExecutionClient(secondSession);
        var connection = new FakeConnection(firstClient);
        await using var runtime = new WorkflowExecutionRuntime(
            connection,
            CreateDescriptor());

        var firstRun = runtime.ExecuteAsync(
            new Dictionary<string, object?>(),
            TimeSpan.FromSeconds(2));
        await WaitUntilAsync(() => firstClient.Invocations.Count == 1);
        firstSession.ProcessJobUpdate(Completed("job-runtime-1", "first"));
        await firstRun;

        connection.Client = secondClient;
        var secondRun = runtime.ExecuteAsync(
            new Dictionary<string, object?>(),
            TimeSpan.FromSeconds(2));
        await WaitUntilAsync(() => secondClient.Invocations.Count == 1);
        secondSession.ProcessJobUpdate(Completed("job-runtime-2", "second"));
        var result = await secondRun;

        Assert.Equal(2, connection.ConnectCalls);
        Assert.Equal(
            "second",
            result.Snapshot.Outputs["answer"].Value.AsString());
    }

    [Fact]
    public async Task FailedSessionPreservesStructuredTerminalState()
    {
        using var session = new ExecutionSession("job-1", "workflow-1");
        using var client = new FakeExecutionClient(session);
        await using var controller = CreateController(client);

        await controller.StartAsync(new WorkflowInvocation("workflow-1"));
        session.ProcessJobUpdate(new JobUpdate
        {
            job_id = "job-1",
            status = "failed",
            error = "provider failed"
        });
        await controller.WaitForTerminalAsync();

        Assert.Equal(WorkflowExecutionState.Failed, controller.Snapshot.State);
        Assert.Equal("provider failed", controller.Snapshot.Error);
        Assert.NotNull(controller.Snapshot.CompletedAt);
    }

    [Fact]
    public async Task RetainsOutputsAcrossRunsUnlessInvocationClearsThem()
    {
        using var first = new ExecutionSession("job-1", "workflow-1");
        using var second = new ExecutionSession("job-2", "workflow-1");
        using var third = new ExecutionSession("job-3", "workflow-1");
        using var client = new FakeExecutionClient(first, second, third);
        await using var controller = CreateController(client);

        await controller.StartAsync(new WorkflowInvocation("workflow-1"));
        first.ProcessOutputUpdate(Output("output-node", "latched"));
        first.ProcessJobUpdate(Completed("job-1", "latched"));
        await controller.WaitForTerminalAsync();

        await controller.StartAsync(new WorkflowInvocation("workflow-1"));
        Assert.Equal(
            "latched",
            controller.Snapshot.Outputs["answer"].Value.AsString());
        second.ProcessJobUpdate(Completed("job-2", "second"));
        await controller.WaitForTerminalAsync();

        await controller.StartAsync(new WorkflowInvocation(
            "workflow-1",
            new Dictionary<string, object?>(),
            RetainOutputs: false));
        Assert.Empty(controller.Snapshot.Outputs);
        third.ProcessJobUpdate(Completed("job-3", "third"));
        await controller.WaitForTerminalAsync();
    }

    [Fact]
    public async Task QueueLatestCoalescesPendingInvocations()
    {
        using var first = new ExecutionSession("job-1", "workflow-1");
        using var second = new ExecutionSession("job-2", "workflow-1");
        using var client = new FakeExecutionClient(first, second);
        await using var controller = CreateController(client);

        await controller.StartAsync(new WorkflowInvocation("workflow-1"));
        await controller.RequestStartAsync(
            InvocationWithVersion(1),
            WorkflowStartPolicy.QueueLatest);
        await controller.RequestStartAsync(
            InvocationWithVersion(2),
            WorkflowStartPolicy.QueueLatest);

        first.ProcessJobUpdate(Completed("job-1", "first"));
        await WaitUntilAsync(() => client.Invocations.Count == 2);

        Assert.Equal(2, client.Invocations[1].Inputs["version"]);
        second.ProcessJobUpdate(Completed("job-2", "second"));
        await controller.WaitForTerminalAsync();
        Assert.Equal(2, client.Invocations.Count);
        Assert.Equal(
            WorkflowExecutionState.Completed,
            controller.Snapshot.State);
    }

    [Fact]
    public async Task CancelAndRestartCancelsCurrentRunThenStartsLatestRequest()
    {
        using var first = new ExecutionSession("job-1", "workflow-1");
        using var second = new ExecutionSession("job-2", "workflow-1");
        var cancels = 0;
        first.CancelAction = (_, _, _) =>
        {
            Interlocked.Increment(ref cancels);
            first.ProcessJobUpdate(new JobUpdate
            {
                job_id = "job-1",
                status = "cancelled"
            });
            return Task.CompletedTask;
        };
        using var client = new FakeExecutionClient(first, second);
        await using var controller = CreateController(client);

        await controller.StartAsync(new WorkflowInvocation("workflow-1"));
        await controller.RequestStartAsync(
            InvocationWithVersion(7),
            WorkflowStartPolicy.CancelAndRestart);
        await WaitUntilAsync(() => client.Invocations.Count == 2);

        Assert.Equal(1, cancels);
        Assert.Equal(7, client.Invocations[1].Inputs["version"]);
        second.ProcessJobUpdate(Completed("job-2", "restarted"));
        await controller.WaitForTerminalAsync();
        Assert.Equal(
            "restarted",
            controller.Snapshot.Outputs["answer"].Value.AsString());
    }

    private static WorkflowExecutionController CreateController(
        INodeToolExecutionClient client,
        Func<CancellationToken, Task<SdkCapabilitiesResponse>>?
            getCapabilities = null)
        => new(
            client,
            [
                new WorkflowOutputDescriptor(
                    "output-node",
                    "answer",
                    "Answer",
                    new WorkflowTypeDescriptor(
                        "str",
                        Optional: false,
                        TypeName: null,
                        Values: [],
                        TypeArguments: []),
                    Stream: true)
            ],
            getCapabilities);

    private static WorkflowDescriptor CreateDescriptor()
        => new(
            "workflow-1",
            "Test",
            "",
            "revision",
            null,
            null,
            1,
            null,
            "test",
            [
                new WorkflowInputDescriptor(
                    "input-node",
                    "prompt",
                    "",
                    new WorkflowTypeDescriptor(
                        "str",
                        Optional: false,
                        TypeName: null,
                        Values: [],
                        TypeArguments: []),
                    Required: false,
                    DefaultValue: null,
                    Minimum: null,
                    Maximum: null)
            ],
            [
                new WorkflowOutputDescriptor(
                    "output-node",
                    "answer",
                    "Answer",
                    new WorkflowTypeDescriptor(
                        "str",
                        Optional: false,
                        TypeName: null,
                        Values: [],
                        TypeArguments: []),
                    Stream: true)
            ],
            []);

    private static SdkCapabilitiesResponse SupportedCapabilities()
        => new()
        {
            ProtocolVersion = "1",
            NodetoolVersion = "test",
            SupportedEncodings = ["messagepack"],
            ExecutionOptions = new SdkExecutionOptionsCapabilities
            {
                Persistence = ["job", "session"],
                EventDetail = ["full", "outputs", "terminal"],
                AssetPersistence = ["auto", "temporary"]
            },
            Profiles = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["temporary_asset_upload"] = "available"
            }
        };

    private static OutputUpdate Output(
        string nodeId,
        object value,
        string disposition = "append",
        bool? done = null)
        => new()
        {
            job_id = "job-1",
            node_id = nodeId,
            node_name = "answer",
            output_name = "answer",
            output_type = "str",
            value = value,
            disposition = disposition,
            done = done
        };

    private static Dictionary<string, object> Chunk(string content)
        => new()
        {
            ["type"] = "chunk",
            ["content"] = content
        };

    private static Dictionary<string, object> AudioChunk(string content)
        => new()
        {
            ["type"] = "chunk",
            ["content_type"] = "audio",
            ["content"] = content
        };

    private static WorkflowInvocation InvocationWithVersion(int version)
        => new(
            "workflow-1",
            new Dictionary<string, object?>
            {
                ["version"] = version
            });

    private static JobUpdate Completed(string jobId, string answer)
        => new()
        {
            job_id = jobId,
            status = "completed",
            result = new Dictionary<string, object>
            {
                ["outputs"] = new Dictionary<string, object>
                {
                    ["answer"] = new object[] { answer }
                }
            }
        };

    private static async Task WaitUntilAsync(Func<bool> predicate)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        while (!predicate())
            await Task.Delay(10, timeout.Token);
    }

    private sealed class FakeExecutionClient : INodeToolExecutionClient
    {
        private readonly Queue<IExecutionSession> _sessions;

        public FakeExecutionClient(params IExecutionSession[] sessions)
        {
            _sessions = new Queue<IExecutionSession>(sessions);
        }

        public List<(string WorkflowId, Dictionary<string, object> Inputs)>
            Invocations { get; } = [];
        public WorkflowExecutionOptions? LastExecutionOptions { get; private set; }
        public int DisposeCalls { get; private set; }

        public bool IsConnected => true;
        public string ConnectionStatus => "connected";
        public string? LastError => null;
        public event Action<string>? ConnectionStatusChanged
        {
            add { }
            remove { }
        }

        public Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task DisconnectAsync() => Task.CompletedTask;

        public Task<IExecutionSession> ExecuteWorkflowAsync(
            string workflowId,
            Dictionary<string, object>? inputs = null,
            CancellationToken cancellationToken = default)
        {
            Invocations.Add((
                workflowId,
                inputs ?? new Dictionary<string, object>()));
            return Task.FromResult(_sessions.Dequeue());
        }

        public Task<IExecutionSession> ExecuteWorkflowAsync(
            string workflowId,
            Dictionary<string, object>? inputs,
            WorkflowExecutionOptions? executionOptions,
            CancellationToken cancellationToken = default)
        {
            LastExecutionOptions = executionOptions;
            return ExecuteWorkflowAsync(workflowId, inputs, cancellationToken);
        }

        public Task<IExecutionSession> ExecuteWorkflowByNameAsync(
            string workflowName,
            Dictionary<string, object>? inputs = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IExecutionSession> ExecuteWorkflowByNameAsync(
            string workflowName,
            string inputName,
            object? inputValue,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IExecutionSession> ExecuteWorkflowByNameAsync(
            string workflowName,
            CancellationToken cancellationToken = default,
            params (string Name, object? Value)[] inputs)
            => throw new NotSupportedException();

        public Task<List<NodeMetadataResponse>> GetNodeTypesAsync(
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<NodeTypeInventoryResponse> GetNodeTypeInventoryAsync(
            int cursor = 0,
            int limit = 100,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<NodeMetadataResponse?> GetNodeAsync(
            string nodeType,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<List<WorkflowResponse>> GetWorkflowsAsync(
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<WorkflowResponse?> GetWorkflowAsync(
            string workflowId,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<List<WorkflowSummaryResponse>> GetWorkflowSummariesAsync(
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<WorkflowInterfaceResponse> GetWorkflowInterfaceAsync(
            string workflowId,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<WorkflowInterfacesResponse> GetWorkflowInterfacesAsync(
            IReadOnlyCollection<string> workflowIds,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<List<AssetResponse>> GetAssetsAsync(
            string? contentType = null,
            string? parentId = null,
            int pageSize = 10000,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<AssetResponse?> GetAssetAsync(
            string assetId,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IExecutionSession> ExecuteGraphAsync(
            Graph graph,
            Dictionary<string, object>? inputs = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IExecutionSession> ExecuteNodeAsync(
            string nodeType,
            Dictionary<string, object>? inputs = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public void Dispose()
        {
            DisposeCalls++;
        }

        public ValueTask DisposeAsync()
        {
            DisposeCalls++;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeConnection(
        INodeToolExecutionClient client) : INodeToolExecutionConnection
    {
        public INodeToolExecutionClient Client { get; set; } = client;
        public int ConnectCalls { get; private set; }
        public Uri? ApiBaseUrl => null;
        public string? AuthToken => null;

        public Task<SdkCapabilitiesResponse> GetSdkCapabilitiesAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult(SupportedCapabilities());

        public Task<INodeToolExecutionClient> GetConnectedClientAsync(
            CancellationToken cancellationToken = default)
        {
            ConnectCalls++;
            return Task.FromResult(Client);
        }
    }
}

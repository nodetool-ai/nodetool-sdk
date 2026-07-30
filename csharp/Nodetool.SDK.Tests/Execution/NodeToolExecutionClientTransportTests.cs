using Nodetool.SDK.Configuration;
using Nodetool.SDK.Connection;
using Nodetool.SDK.Execution;
using Nodetool.SDK.Types;
using Nodetool.SDK.WebSocket;

namespace Nodetool.SDK.Tests.Execution;

public sealed class NodeToolExecutionClientTransportTests
{
    [Fact]
    public async Task Connect_UsesDerivedEndpointAndFreshBearerToken()
    {
        var transport = new FakeTransport();
        var tokenProvider = new CountingTokenProvider();
        await using var client = new NodeToolExecutionClient(
            new NodeToolClientOptions
            {
                WorkerWebSocketUrl =
                    new Uri("https://cloud.example/nodetool"),
                TokenProvider = tokenProvider
            },
            webSocketTransport: transport);

        Assert.True(await client.ConnectAsync());
        await client.DisconnectAsync();
        Assert.True(await client.ConnectAsync());

        Assert.Equal(2, tokenProvider.Calls);
        Assert.Equal(
            [
                new Uri("wss://cloud.example/nodetool/ws"),
                new Uri("wss://cloud.example/nodetool/ws")
            ],
            transport.ConnectedUris);
        Assert.Equal(["token-1", "token-2"], transport.BearerTokens);
    }

    [Fact]
    public async Task RunCancelAndReconnect_UseJobScopedCommands()
    {
        var transport = new FakeTransport();
        await using var client = new NodeToolExecutionClient(
            new NodeToolClientOptions
            {
                WorkerWebSocketUrl =
                    new Uri("ws://localhost:7777/ws"),
                AutoReconnect = false
            },
            webSocketTransport: transport);
        await client.ConnectAsync();

        var session = await client.ExecuteWorkflowAsync(
            "workflow-1",
            new Dictionary<string, object> { ["prompt"] = "hello" });
        await session.CancelAsync();
        await client.DisconnectAsync();
        await client.ConnectAsync();

        var commands = transport.SentMessages
            .OfType<WebSocketCommand>()
            .ToArray();
        var run = Assert.Single(
            commands,
            command => command.command == "run_job");
        var cancel = Assert.Single(
            commands,
            command => command.command == "cancel_job");
        var reconnect = Assert.Single(
            commands,
            command => command.command == "reconnect_job");

        var runData = Assert.IsType<RunJobRequest>(run.data);
        Assert.Equal(session.JobId, runData.JobId);
        Assert.Equal(
            session.JobId,
            Assert.IsType<CancelJobData>(cancel.data).job_id);
        Assert.Equal(
            session.JobId,
            Assert.IsType<ReconnectJobData>(reconnect.data).job_id);
    }

    [Fact]
    public async Task ActiveSession_SendsLiveInputEndAndPropertyCommands()
    {
        var transport = new FakeTransport();
        await using var client = new NodeToolExecutionClient(
            new NodeToolClientOptions
            {
                WorkerWebSocketUrl =
                    new Uri("ws://localhost:7777/ws"),
                AutoReconnect = false
            },
            webSocketTransport: transport);
        await client.ConnectAsync();
        var session = await client.ExecuteWorkflowAsync("workflow-1");

        await session.StreamInputAsync("prompt", "hello", "value");
        await session.EndInputStreamAsync("prompt", "value");
        await session.UpdateNodePropertiesAsync(
            "synth-1",
            new Dictionary<string, object?> { ["frequency"] = 440f });

        var commands = transport.SentMessages
            .OfType<WebSocketCommand>()
            .ToArray();
        var stream = Assert.Single(
            commands,
            command => command.command == "stream_input");
        var end = Assert.Single(
            commands,
            command => command.command == "end_input_stream");
        var properties = Assert.Single(
            commands,
            command => command.command == "update_node_properties");

        var streamData = Assert.IsType<StreamInputData>(stream.data);
        Assert.Equal(session.JobId, streamData.job_id);
        Assert.Equal("workflow-1", streamData.workflow_id);
        Assert.Equal("prompt", streamData.input);
        Assert.Equal("value", streamData.handle);
        Assert.Equal("hello", streamData.value);

        var endData = Assert.IsType<EndInputStreamData>(end.data);
        Assert.Equal(session.JobId, endData.job_id);
        Assert.Equal("prompt", endData.input);
        Assert.Equal("value", endData.handle);

        var propertyData = Assert.IsType<UpdateNodePropertiesData>(
            properties.data);
        Assert.Equal(session.JobId, propertyData.job_id);
        Assert.Equal("synth-1", propertyData.node_id);
        Assert.Equal(440f, propertyData.properties["frequency"]);
    }

    [Fact]
    public async Task LiveCommandSendFailure_IsReportedToCaller()
    {
        var transport = new FakeTransport();
        await using var client = new NodeToolExecutionClient(
            new NodeToolClientOptions
            {
                WorkerWebSocketUrl =
                    new Uri("ws://localhost:7777/ws"),
                AutoReconnect = false
            },
            webSocketTransport: transport);
        await client.ConnectAsync();
        var session = await client.ExecuteWorkflowAsync("workflow-1");
        transport.SendResult = false;

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => session.StreamInputAsync("prompt", "hello"));

        Assert.Contains("stream_input", error.Message);
    }

    [Fact]
    public async Task SingleNodeRun_ProjectsNegotiatedExecutionOptions()
    {
        var transport = new FakeTransport();
        await using var client = new NodeToolExecutionClient(
            new NodeToolClientOptions
            {
                WorkerWebSocketUrl =
                    new Uri("ws://localhost:7777/ws"),
                AutoReconnect = false
            },
            webSocketTransport: transport);
        await client.ConnectAsync();

        await client.ExecuteNodeAsync(
            "nodetool.constant.String",
            new Dictionary<string, object> { ["value"] = "hello" },
            new WorkflowExecutionOptions(
                WorkflowPersistence.Session,
                WorkflowEventDetail.Outputs,
                WorkflowAssetPersistence.Temporary));

        var command = Assert.Single(
            transport.SentMessages.OfType<WebSocketCommand>(),
            value => value.command == "run_job");
        var request = Assert.IsType<RunJobRequest>(command.data);
        Assert.NotNull(request.ExecutionOptions);
        Assert.Equal("session", request.ExecutionOptions.Persistence);
        Assert.Equal("outputs", request.ExecutionOptions.EventDetail);
        Assert.Equal(
            "temporary",
            request.ExecutionOptions.AssetPersistence);
    }

    [Fact]
    public async Task ConnectionAndSendFailuresRemainDeterministic()
    {
        var connectionFailure = new FakeTransport
        {
            ConnectResult = false
        };
        await using (var failedClient = new NodeToolExecutionClient(
            new NodeToolClientOptions
            {
                WorkerWebSocketUrl =
                    new Uri("ws://localhost:7777/ws"),
                AutoReconnect = false
            },
            webSocketTransport: connectionFailure))
        {
            Assert.False(await failedClient.ConnectAsync());
            Assert.Equal("error", failedClient.ConnectionStatus);
            Assert.Equal("Connection failed", failedClient.LastError);
        }

        var sendFailure = new FakeTransport();
        await using var client = new NodeToolExecutionClient(
            new NodeToolClientOptions
            {
                WorkerWebSocketUrl =
                    new Uri("ws://localhost:7777/ws"),
                AutoReconnect = false
            },
            webSocketTransport: sendFailure);
        await client.ConnectAsync();
        sendFailure.SendResult = false;

        var session = await client.ExecuteWorkflowAsync("workflow-1");

        Assert.True(session.IsCompleted);
        Assert.False(await session.WaitForCompletionAsync());
        Assert.Equal(
            "Failed to send execution request",
            session.ErrorMessage);
    }

    [Fact]
    public async Task ReadRpc_RetriesWithStableRequestIdentity()
    {
        var transport = new FakeTransport
        {
            RequestFailuresRemaining = 2,
            RequestResponse = new Dictionary<string, object?>
            {
                ["result"] = new Dictionary<string, object?>
                {
                    ["nodes"] = Array.Empty<object>()
                }
            }
        };
        await using var client = new NodeToolExecutionClient(
            new NodeToolClientOptions
            {
                WorkerWebSocketUrl =
                    new Uri("ws://localhost:7777/ws"),
                AutoReconnect = false,
                ReadRetryPolicy = new NodeToolReadRetryPolicy
                {
                    MaximumAttempts = 3,
                    InitialDelay = TimeSpan.Zero,
                    MaximumDelay = TimeSpan.Zero
                }
            },
            webSocketTransport: transport);
        await client.ConnectAsync();

        var nodes = await client.GetNodeTypesAsync();

        Assert.Empty(nodes);
        Assert.Equal(3, transport.ReadRequests.Count);
        Assert.Single(
            transport.ReadRequests
                .Select(request => request.RequestId)
                .Distinct(StringComparer.Ordinal));
        Assert.All(
            transport.ReadRequests,
            request => Assert.Equal("list_nodes", request.Command));
    }

    [Fact]
    public async Task DirectClient_DefaultReadPolicyDoesNotRetry()
    {
        var transport = new FakeTransport
        {
            RequestFailuresRemaining = 1
        };
        await using var client = new NodeToolExecutionClient(
            new NodeToolClientOptions
            {
                WorkerWebSocketUrl =
                    new Uri("ws://localhost:7777/ws"),
                AutoReconnect = false
            },
            webSocketTransport: transport);
        await client.ConnectAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetNodeTypesAsync());

        Assert.Single(transport.ReadRequests);
    }

    private sealed class CountingTokenProvider : INodeToolTokenProvider
    {
        public int Calls { get; private set; }

        public ValueTask<string?> GetTokenAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            return ValueTask.FromResult<string?>($"token-{Calls}");
        }
    }

    private sealed class FakeTransport : INodeToolWebSocketTransport
    {
        public bool IsConnected { get; private set; }
        public List<Uri> ConnectedUris { get; } = [];
        public List<string?> BearerTokens { get; } = [];
        public List<object> SentMessages { get; } = [];
        public bool ConnectResult { get; init; } = true;
        public bool SendResult { get; set; } = true;
        public int RequestFailuresRemaining { get; set; }
        public Dictionary<string, object?>? RequestResponse { get; set; }
        public List<(
            string Command,
            string? RequestId)> ReadRequests { get; } = [];

        public event EventHandler<MessageReceivedEventArgs>? MessageReceived;
        public event EventHandler<ConnectionStatusEventArgs>?
            ConnectionStatusChanged;

        public Task<bool> ConnectAsync(
            Uri uri,
            string? bearerToken,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ConnectedUris.Add(uri);
            BearerTokens.Add(bearerToken);
            IsConnected = ConnectResult;
            ConnectionStatusChanged?.Invoke(
                this,
                new ConnectionStatusEventArgs
                {
                    Status = ConnectResult ? "connected" : "error",
                    Message = ConnectResult
                        ? "connected"
                        : "Connection failed"
                });
            return Task.FromResult(ConnectResult);
        }

        public Task DisconnectAsync()
        {
            IsConnected = false;
            ConnectionStatusChanged?.Invoke(
                this,
                new ConnectionStatusEventArgs
                {
                    Status = "disconnected",
                    Message = "disconnected"
                });
            return Task.CompletedTask;
        }

        public Task<bool> SendMessageAsync(
            object message,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SentMessages.Add(message);
            return Task.FromResult(SendResult);
        }

        public Task<Dictionary<string, object?>?> SendRequestAsync(
            string command,
            Dictionary<string, object?>? data = null,
            CancellationToken cancellationToken = default,
            TimeSpan? timeout = null,
            string? requestId = null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReadRequests.Add((command, requestId));
            if (RequestFailuresRemaining > 0)
            {
                RequestFailuresRemaining--;
                throw new InvalidOperationException(
                    "Injected read transport failure.");
            }
            return Task.FromResult(RequestResponse);
        }

        public void RaiseMessage(object message)
            => MessageReceived?.Invoke(
                this,
                new MessageReceivedEventArgs { Message = message });

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

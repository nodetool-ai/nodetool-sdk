using MessagePack;
using MessagePack.Resolvers;
using Nodetool.SDK.WebSocket;
using Nodetool.SDK.Types;
using Nodetool.SDK.Execution;

namespace Nodetool.SDK.Tests.WebSocket;

public class MessagePackWebSocketClientContractTests
{
    [Fact]
    public void RunJobRequest_OptsIntoAuthoritativeTerminalResult()
    {
        var options = MessagePackSerializerOptions.Standard.WithResolver(
            CompositeResolver.Create(
                ContractlessStandardResolver.Instance,
                StandardResolver.Instance));
        var payload = MessagePackSerializer.Serialize(new RunJobRequest(), options);
        var map = MessagePackSerializer.Deserialize<Dictionary<string, object?>>(payload, options);

        Assert.Equal(true, map["require_terminal_result"]);
    }

    [Fact]
    public void RunJobRequest_SerializesClientGeneratedJobId()
    {
        var options = MessagePackSerializerOptions.Standard.WithResolver(
            CompositeResolver.Create(
                ContractlessStandardResolver.Instance,
                StandardResolver.Instance));
        var payload = MessagePackSerializer.Serialize(
            new RunJobRequest { JobId = "client-job-1" },
            options);
        var map = MessagePackSerializer.Deserialize<Dictionary<string, object?>>(payload, options);

        Assert.Equal("client-job-1", map["job_id"]);
    }

    [Fact]
    public void RunJobRequest_SerializesExplicitTransientExecutionOptions()
    {
        var options = MessagePackSerializerOptions.Standard.WithResolver(
            CompositeResolver.Create(
                ContractlessStandardResolver.Instance,
                StandardResolver.Instance));
        var wireOptions = NodeToolExecutionClient.CreateRunJobExecutionOptions(
            new WorkflowExecutionOptions(
                WorkflowPersistence.Session,
                WorkflowEventDetail.Outputs,
                WorkflowAssetPersistence.Temporary));
        var payload = MessagePackSerializer.Serialize(
            new RunJobRequest { ExecutionOptions = wireOptions },
            options);
        var map = MessagePackSerializer.Deserialize<Dictionary<string, object?>>(payload, options);
        var executionOptions = Assert.IsType<Dictionary<object, object?>>(
            map["execution_options"]);

        Assert.Equal("session", executionOptions["persistence"]);
        Assert.Equal("outputs", executionOptions["event_detail"]);
        Assert.Equal("temporary", executionOptions["asset_persistence"]);
    }

    [Fact]
    public void RequestEnvelope_PutsCorrelationIdAtTheTopLevel()
    {
        var data = new Dictionary<string, object?> { ["limit"] = 25 };

        var envelope = MessagePackWebSocketClient.CreateRequestEnvelope(
            "list_workflows",
            data,
            "request-1");

        Assert.Equal("list_workflows", envelope["command"]);
        Assert.Equal("request-1", envelope["request_id"]);
        Assert.Same(data, envelope["data"]);
        Assert.DoesNotContain("request_id", data.Keys);
    }

    [Fact]
    public void ReconnectCommand_PreservesJobAndWorkflowIdentity()
    {
        var command = NodeToolExecutionClient.CreateReconnectCommand("job-1", "workflow-1");
        var data = Assert.IsType<ReconnectJobData>(command.data);

        Assert.Equal("reconnect_job", command.command);
        Assert.Equal("reconnect_job", command.type);
        Assert.Equal("job-1", data.job_id);
        Assert.Equal("workflow-1", data.workflow_id);
    }

    [Fact]
    public void StandardNilFromMsgpackr_DecodesWithoutPrivateExtensions()
    {
        // Produced by msgpackr Packr({ useRecords: false, encodeUndefinedAsNil: true }).
        var payload = Convert.FromBase64String(
            "3gADpHR5cGWscnBjX3Jlc3BvbnNlqnJlcXVlc3RfaWSicjGmcmVzdWx03gABpW5vZGVzkd4AAqlub2RlX3R5cGWpdGVzdC5Ob2RlqG9wdGlvbmFswA==");
        var options = MessagePackSerializerOptions.Standard.WithResolver(
            CompositeResolver.Create(
                ContractlessStandardResolver.Instance,
                StandardResolver.Instance));

        var envelope = MessagePackSerializer.Deserialize<Dictionary<string, object?>>(payload, options);

        Assert.Equal("rpc_response", envelope["type"]);
        Assert.Equal("r1", envelope["request_id"]);
    }

    [Fact]
    public void BinaryImageData_RoundTripsWithoutExpandingIntoNumbers()
    {
        var options = MessagePackSerializerOptions.Standard.WithResolver(
            CompositeResolver.Create(
                ContractlessStandardResolver.Instance,
                StandardResolver.Instance));
        var imageBytes = new byte[] { 0, 1, 2, 127, 128, 254, 255 };
        var payload = MessagePackSerializer.Serialize(
            new Dictionary<string, object?>
            {
                ["type"] = "output_update",
                ["value"] = new Dictionary<string, object?>
                {
                    ["type"] = "image",
                    ["data"] = imageBytes,
                },
            },
            options);

        var envelope = MessagePackSerializer.Deserialize<Dictionary<string, object?>>(payload, options);
        var value = Assert.IsType<Dictionary<object, object?>>(envelope["value"]);

        Assert.Equal(imageBytes, Assert.IsType<byte[]>(value["data"]));
    }

    [Fact]
    public void TypedMessage_IgnoresUnknownServerFields()
    {
        var options = MessagePackSerializerOptions.Standard.WithResolver(
            CompositeResolver.Create(
                ContractlessStandardResolver.Instance,
                StandardResolver.Instance));
        var payload = MessagePackSerializer.Serialize(
            new Dictionary<string, object?>
            {
                ["type"] = "output_update",
                ["job_id"] = "job-1",
                ["node_id"] = "node-1",
                ["node_name"] = "Output",
                ["output_name"] = "value",
                ["output_type"] = "string",
                ["value"] = "hello",
                ["future_server_field"] = new Dictionary<string, object?> { ["enabled"] = true },
            },
            options);

        var update = MessagePackSerializer.Deserialize<OutputUpdate>(payload, options);

        Assert.Equal("job-1", update.job_id);
        Assert.Equal("node-1", update.node_id);
        Assert.Equal("hello", update.value);
    }
}

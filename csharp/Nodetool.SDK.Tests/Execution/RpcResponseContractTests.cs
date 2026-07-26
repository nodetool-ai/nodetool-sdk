using Nodetool.SDK.Api.Models;
using Nodetool.SDK.Api;
using Nodetool.SDK.Configuration;
using Nodetool.SDK.Execution;

namespace Nodetool.SDK.Tests.Execution;

public class RpcResponseContractTests
{
    [Fact]
    public void ListResult_NormalizesNestedUntypedMessagePackMaps()
    {
        using var client = new NodeToolExecutionClient(new NodeToolClientOptions
        {
            WorkerWebSocketUrl = new Uri("ws://127.0.0.1:7777/ws")
        });
        var raw = new Dictionary<string, object?>
        {
            ["type"] = "rpc_response",
            ["result"] = new Dictionary<object, object?>
            {
                ["nodes"] = new object[]
                {
                    new Dictionary<object, object?>
                    {
                        ["node_type"] = "test.Node",
                        ["title"] = "Test Node",
                        ["properties"] = Array.Empty<object>(),
                        ["outputs"] = Array.Empty<object>()
                    }
                }
            }
        };

        var nodes = client.DeserializeListResult<NodeMetadataResponse>(raw, "list_nodes", "nodes");

        var node = Assert.Single(nodes);
        Assert.Equal("test.Node", node.NodeType);
        Assert.Equal("Test Node", node.Title);
    }

    [Fact]
    public void RequiredResult_DeserializesWorkflowInterfaceEnvelope()
    {
        using var client = CreateClient();
        var raw = new Dictionary<string, object?>
        {
            ["type"] = "rpc_response",
            ["result"] = new Dictionary<object, object?>
            {
                ["version"] = 1L,
                ["workflow_id"] = "wf-1",
                ["etag"] = "etag-1",
                ["source"] = "server",
                ["inputs"] = Array.Empty<object>(),
                ["outputs"] = Array.Empty<object>(),
                ["diagnostics"] = Array.Empty<object>()
            }
        };

        var result = client.DeserializeRequiredResult<WorkflowInterfaceResponse>(
            raw,
            "get_workflow_interface");

        Assert.Equal(1, result.Version);
        Assert.Equal("wf-1", result.WorkflowId);
        Assert.Equal("server", result.Source);
    }

    [Fact]
    public void RequiredResult_ClassifiesDisabledWorkflowInterfaceFeature()
    {
        using var client = CreateClient();
        var raw = new Dictionary<string, object?>
        {
            ["type"] = "rpc_response",
            ["error"] = new Dictionary<object, object?>
            {
                ["code"] = "INTERNAL_SERVER_ERROR",
                ["apiCode"] = "SERVICE_UNAVAILABLE",
                ["message"] = "SDK workflow interface v1 is disabled"
            }
        };

        var error = Assert.Throws<SdkApiException>(() =>
            client.DeserializeRequiredResult<WorkflowInterfaceResponse>(
                raw,
                "get_workflow_interface"));

        Assert.Equal(SdkApiTransport.WebSocket, error.Transport);
        Assert.True(error.Retryable);
        Assert.Equal("SERVICE_UNAVAILABLE", error.ApiCode);
    }

    [Fact]
    public void ExecutionTargetAnnouncement_IsRetainedForPreflight()
    {
        using var client = CreateClient();

        client.RouteExecutionMessage(new Dictionary<string, object?>
        {
            ["type"] = "sdk_execution_target",
            ["runner_id"] = "runner-1"
        });

        Assert.Equal("runner-1", client.ExecutionTargetId);
    }

    private static NodeToolExecutionClient CreateClient()
        => new(new NodeToolClientOptions
        {
            WorkerWebSocketUrl = new Uri("ws://127.0.0.1:7777/ws")
        });
}

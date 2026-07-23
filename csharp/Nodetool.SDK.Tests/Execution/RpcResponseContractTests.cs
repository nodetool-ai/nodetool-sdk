using Nodetool.SDK.Api.Models;
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
}

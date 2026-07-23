using MessagePack;
using MessagePack.Resolvers;
using Nodetool.SDK.WebSocket;

namespace Nodetool.SDK.Tests.WebSocket;

public class MessagePackWebSocketClientContractTests
{
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
}

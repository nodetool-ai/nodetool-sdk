using Nodetool.SDK.VL.Services;

namespace Nodetool.SDK.Tests.VL;

public class NodeToolClientProviderTests
{
    [Fact]
    public void WebSocketDiscoveryFlag_IsIdempotentAndReversible()
    {
        try
        {
            NodeToolClientProvider.SetUseWebSocketDiscovery(true);
            NodeToolClientProvider.SetUseWebSocketDiscovery(true);

            Assert.True(NodeToolClientProvider.UseWebSocketDiscovery);
        }
        finally
        {
            NodeToolClientProvider.SetUseWebSocketDiscovery(false);
        }

        Assert.False(NodeToolClientProvider.UseWebSocketDiscovery);
    }
}

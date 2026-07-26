using Nodetool.SDK.Connection;
using Nodetool.SDK.VL.Services;
using Xunit;

namespace Nodetool.SDK.VL.UnitTests;

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

    [Fact]
    public void DisposedHostSession_IsNotRetainedByStaticFacade()
    {
        var settings = new VlNodeToolHostSettings();
        var hostSession =
            NodeToolClientProvider.CreateHostSession(settings);
        NodeToolClientProvider.UseHostSession(
            hostSession,
            settings);
        hostSession.Dispose();

        var error = Record.Exception(
            () => NodeToolClientProvider.SetAutoReconnect(false));

        Assert.Null(error);
        Assert.True(hostSession.IsDisposed);
        NodeToolClientProvider.SetAutoReconnect(true);
    }
}

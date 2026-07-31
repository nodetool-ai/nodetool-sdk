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
    public void DiscoveryCatalogSelection_IsIdempotentAndReversible()
    {
        try
        {
            NodeToolClientProvider.SetLoadNodes(false);
            NodeToolClientProvider.SetLoadNodes(false);
            NodeToolClientProvider.SetShowAllNodes(true);
            NodeToolClientProvider.SetShowAllNodes(true);
            NodeToolClientProvider.SetLoadWorkflows(false);
            NodeToolClientProvider.SetLoadWorkflows(false);

            Assert.False(NodeToolClientProvider.LoadNodes);
            Assert.True(NodeToolClientProvider.ShowAllNodes);
            Assert.False(NodeToolClientProvider.LoadWorkflows);
        }
        finally
        {
            NodeToolClientProvider.SetLoadNodes(true);
            NodeToolClientProvider.SetShowAllNodes(false);
            NodeToolClientProvider.SetLoadWorkflows(true);
        }

        Assert.True(NodeToolClientProvider.LoadNodes);
        Assert.False(NodeToolClientProvider.ShowAllNodes);
        Assert.True(NodeToolClientProvider.LoadWorkflows);
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

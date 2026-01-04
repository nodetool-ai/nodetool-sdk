using System.Collections.Immutable;
using VL.Core;
using VL.Core.CompilerServices;
using Nodetool.SDK.VL.Services;

namespace Nodetool.SDK.VL.Factories;

/// <summary>
/// Factory for creating diagnostic and connection VL nodes.
/// </summary>
internal static class DiagnosticsNodeFactory
{
    /// <summary>
    /// Gets a VL node factory that provides the basic diagnostic nodes (Connect, ConnectionStatus).
    /// This factory is intentionally lightweight and does not call the Nodetool API.
    /// </summary>
    public static NodeBuilding.FactoryImpl GetFactory(IVLNodeDescriptionFactory vlSelfFactory)
    {
        Console.WriteLine("=== DiagnosticsNodeFactory.GetFactory called ===");
        Console.WriteLine($"DiagnosticsNodeFactory: vlSelfFactory type: {vlSelfFactory?.GetType().Name ?? "null"}");

        if (vlSelfFactory == null)
            return NodeBuilding.NewFactoryImpl(ImmutableArray<IVLNodeDescription>.Empty);

        var nodeDescriptions = new List<IVLNodeDescription>();
        AddDiagnosticsNodes(vlSelfFactory, nodeDescriptions);

        Console.WriteLine($"DiagnosticsNodeFactory: Creating factory with {nodeDescriptions.Count} node descriptions...");
        return NodeBuilding.NewFactoryImpl(ImmutableArray.CreateRange(nodeDescriptions));
    }

    /// <summary>
    /// Creates the Connect node description.
    /// </summary>
    public static IVLNodeDescription? CreateConnectNode(IVLNodeDescriptionFactory vlSelfFactory)
    {
        return vlSelfFactory?.NewNodeDescription(
            name: "Connect",
            category: "Nodetool",
            fragmented: false,
            bc =>
            {
                // Input pins
                var baseUrlPin = bc.Pin("BaseUrl", typeof(string), "ws://localhost:7777",
                    "🌐 Server URL", "WebSocket URL of the NodeTool server (e.g., ws://localhost:7777)");
                var apiKeyPin = bc.Pin("ApiKey", typeof(string), "",
                    "🔑 API Key", "Optional API key for authentication");
                var autoReconnectPin = bc.Pin("AutoReconnect", typeof(bool), true,
                    "🔄 Auto Reconnect", "Automatically reconnect on connection loss");
                var reconnectTriggerPin = bc.Pin("Reconnect", typeof(bool), false,
                    "⚡ Reconnect", "Trigger to force reconnection");

                // Output pins
                var isConnectedPin = bc.Pin("IsConnected", typeof(bool), false,
                    "✅ Connected", "True when connected to the server");
                var statusPin = bc.Pin("Status", typeof(string), "disconnected",
                    "📊 Status", "Current connection status");
                var lastErrorPin = bc.Pin("LastError", typeof(string), "",
                    "❌ Last Error", "Last error message if connection failed");

                return bc.Node(
                    inputs: new IVLPinDescription[] { baseUrlPin, apiKeyPin, autoReconnectPin, reconnectTriggerPin },
                    outputs: new IVLPinDescription[] { isConnectedPin, statusPin, lastErrorPin },
                    newNode: ibc =>
                    {
                        bool lastReconnectState = false;
                        bool hasConnected = false;
                        string lastUrl = "";
                        string lastApiKey = "";

                        return ibc.Node(
                            inputs: new IVLPin[]
                            {
                                ibc.Input<string>(val =>
                                {
                                    // Handle URL changes - only update if value changed
                                    if (!string.IsNullOrEmpty(val) && val != lastUrl)
                                    {
                                        lastUrl = val;
                                        // Update config (do not create client here)
                                        NodeToolClientProvider.Configure(lastUrl, lastApiKey, disposeExistingClient: true);
                                    }
                                }),
                                ibc.Input<string>(val =>
                                {
                                    // Handle API key changes - store for use with URL
                                    if (val != lastApiKey)
                                    {
                                        lastApiKey = val ?? "";

                                        if (!string.IsNullOrEmpty(lastUrl))
                                            NodeToolClientProvider.Configure(lastUrl, lastApiKey, disposeExistingClient: true);
                                    }
                                }),
                                ibc.Input<bool>(val =>
                                {
                                    // Auto reconnect is handled by the client itself
                                    // This pin is for future implementation
                                }),
                                ibc.Input<bool>(val =>
                                {
                                    // Reconnect trigger
                                    if (val && !lastReconnectState)
                                    {
                                        _ = NodeToolClientProvider.ReconnectAsync();
                                    }
                                    lastReconnectState = val;
                                    
                                    // Also trigger initial connection if not connected yet
                                    if (!hasConnected && !NodeToolClientProvider.IsConnected)
                                    {
                                        hasConnected = true;
                                        _ = NodeToolClientProvider.ConnectAsync();
                                    }
                                })
                            },
                            outputs: new IVLPin[]
                            {
                                ibc.Output<bool>(() => NodeToolClientProvider.IsConnected),
                                ibc.Output<string>(() => NodeToolClientProvider.Status),
                                ibc.Output<string>(() => NodeToolClientProvider.LastError ?? "")
                            }
                        );
                    },
                    summary: "Connect to NodeTool server",
                    remarks: "Establishes a WebSocket connection to the NodeTool server. The connection is shared across all NodeTool nodes."
                );
            }
        );
    }

    /// <summary>
    /// Creates the ConnectionStatus node description.
    /// </summary>
    public static IVLNodeDescription? CreateConnectionStatusNode(IVLNodeDescriptionFactory vlSelfFactory)
    {
        return vlSelfFactory?.NewNodeDescription(
            name: "ConnectionStatus",
            category: "Nodetool.Diagnostics",
            fragmented: false,
            bc =>
            {
                var isConnectedPin = bc.Pin("IsConnected", typeof(bool), false,
                    "✅ Connected", "Whether the client is connected");
                var statusPin = bc.Pin("Status", typeof(string), "disconnected",
                    "📊 Status", "Current connection status string");
                var lastErrorPin = bc.Pin("LastError", typeof(string), "",
                    "❌ Last Error", "Last error message if any");

                return bc.Node(
                    inputs: Enumerable.Empty<IVLPinDescription>(),
                    outputs: new IVLPinDescription[] { isConnectedPin, statusPin, lastErrorPin },
                    newNode: ibc => ibc.Node(
                        inputs: Enumerable.Empty<IVLPin>(),
                        outputs: new IVLPin[]
                        {
                            ibc.Output<bool>(() => NodeToolClientProvider.IsConnected),
                            ibc.Output<string>(() => NodeToolClientProvider.Status),
                            ibc.Output<string>(() => NodeToolClientProvider.LastError ?? "")
                        }
                    ),
                    summary: "Get NodeTool connection status",
                    remarks: "Reports the current connection status of the shared NodeTool client."
                );
            }
        );
    }

    /// <summary>
    /// Add all diagnostic nodes to a factory.
    /// </summary>
    public static void AddDiagnosticsNodes(IVLNodeDescriptionFactory? vlSelfFactory, List<IVLNodeDescription> nodeDescriptions)
    {
        if (vlSelfFactory == null)
        {
            Console.WriteLine("DiagnosticsNodeFactory: vlSelfFactory is null, skipping diagnostic nodes");
            return;
        }

        // Connect node
        var connectNode = CreateConnectNode(vlSelfFactory);
        if (connectNode != null)
        {
            nodeDescriptions.Add(connectNode);
            Console.WriteLine("DiagnosticsNodeFactory: Created Connect node");
        }

        // Connection status node
        var statusNode = CreateConnectionStatusNode(vlSelfFactory);
        if (statusNode != null)
        {
            nodeDescriptions.Add(statusNode);
            Console.WriteLine("DiagnosticsNodeFactory: Created ConnectionStatus node");
        }
    }
}

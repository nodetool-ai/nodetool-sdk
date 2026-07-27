using System.Collections.Immutable;
using VL.Core;
using VL.Core.CompilerServices;
using Nodetool.SDK.VL.Services;
using Nodetool.SDK.VL.Utilities;
using Nodetool.SDK.VL.Factories;
using Nodetool.SDK.Execution;

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
        VlLog.Debug(
            $"DiagnosticsNodeFactory: resolving for " +
            $"{vlSelfFactory?.GetType().Name ?? "null"}");

        if (vlSelfFactory == null)
            return NodeBuilding.NewFactoryImpl(ImmutableArray<IVLNodeDescription>.Empty);

        var nodeDescriptions = new List<IVLNodeDescription>();
        AddDiagnosticsNodes(vlSelfFactory, nodeDescriptions);

        VlLog.Debug(
            $"DiagnosticsNodeFactory: resolved {nodeDescriptions.Count} descriptions");
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

                var executionTimeoutPin = bc.Pin("ExecutionTimeoutSeconds", typeof(int),
                    NodeToolClientProvider.DefaultExecutionTimeoutSeconds,
                    "Execution timeout",
                    "Default maximum duration of workflow and node runs in seconds. "
                    + $"Clamped to 1-{NodeToolClientProvider.MaximumExecutionTimeoutSeconds}; individual execution nodes can override it.");
                var inlineMediaLimitPin = bc.Pin("InlineMediaLimitBytes", typeof(int),
                    NodeToolClientProvider.DefaultInlineMediaLimitBytes,
                    "Inline media limit",
                    "Media larger than this byte count is uploaded as an asset before execution. Use 0 to upload all binary media.");
                var useWebSocketDiscoveryPin = bc.Pin("UseWebSocketDiscovery", typeof(bool), false,
                    "WebSocket discovery",
                    "Use compact correlated WebSocket RPC for workflow discovery after the shared connection opens. HTTP remains the bootstrap transport.");
                var loadNodesPin = bc.Pin("LoadNodes", typeof(bool), true,
                    "Load individual nodes",
                    "Publish individual NodeTool nodes in the vvvv node browser. Disable this when only workflows are needed.");
                var loadWorkflowsPin = bc.Pin("LoadWorkflows", typeof(bool), true,
                    "Load workflows",
                    "Publish NodeTool workflows in the vvvv node browser. Disable this when only individual nodes are needed.");
                var refreshDiscoveryPin = bc.Pin("RefreshDiscovery", typeof(bool), false,
                    "Refresh discovery",
                    "Refresh the enabled node and workflow catalogs without restarting vvvv.");
                var persistencePin = new VlPinDescription(
                    "Persistence",
                    typeof(WorkflowPersistence),
                    WorkflowPersistence.Job,
                    "Execution persistence",
                    "Job preserves normal history and reconnect behavior; Session requests lower-overhead session-only execution when advertised by the server.",
                    isVisible: false);
                var eventDetailPin = new VlPinDescription(
                    "EventDetail",
                    typeof(WorkflowEventDetail),
                    WorkflowEventDetail.Full,
                    "Execution event detail",
                    "Select Full, Outputs, or Terminal events when advertised by the server.",
                    isVisible: false);
                var assetPersistencePin = new VlPinDescription(
                    "AssetPersistence",
                    typeof(WorkflowAssetPersistence),
                    WorkflowAssetPersistence.Temporary,
                    "Asset persistence",
                    "Temporary is the SDK default and disables generated-asset autosave. Auto explicitly enables normal persistent asset behavior.",
                    isVisible: false);

                // Output pins
                var isConnectedPin = bc.Pin("IsConnected", typeof(bool), false,
                    "✅ Connected", "True when connected to the server");
                var statusPin = bc.Pin("Status", typeof(string), "disconnected",
                    "📊 Status", "Current connection status");
                var lastErrorPin = bc.Pin("LastError", typeof(string), "",
                    "❌ Last Error", "Last error message if connection failed");

                return bc.Node(
                    inputs: new IVLPinDescription[] {
                        baseUrlPin, apiKeyPin, autoReconnectPin,
                        reconnectTriggerPin, executionTimeoutPin,
                        inlineMediaLimitPin, useWebSocketDiscoveryPin,
                        loadNodesPin, loadWorkflowsPin, refreshDiscoveryPin,
                        persistencePin, eventDetailPin, assetPersistencePin
                    },
                    outputs: new IVLPinDescription[] { isConnectedPin, statusPin, lastErrorPin },
                    newNode: ibc =>
                    {
                        bool lastReconnectState = false;
                        bool lastRefreshDiscoveryState = false;
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
                                    NodeToolClientProvider.SetAutoReconnect(val);
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
                                }),
                                ibc.Input<int>(NodeToolClientProvider.SetExecutionTimeoutSeconds),
                                ibc.Input<int>(NodeToolClientProvider.SetInlineMediaLimitBytes),
                                ibc.Input<bool>(NodeToolClientProvider.SetUseWebSocketDiscovery),
                                ibc.Input<bool>(NodeToolClientProvider.SetLoadNodes),
                                ibc.Input<bool>(NodeToolClientProvider.SetLoadWorkflows),
                                ibc.Input<bool>(val =>
                                {
                                    if (val && !lastRefreshDiscoveryState)
                                        NodeToolClientProvider.RefreshDiscovery();
                                    lastRefreshDiscoveryState = val;
                                }),
                                ibc.Input<WorkflowPersistence>(
                                    NodeToolClientProvider.SetWorkflowPersistence),
                                ibc.Input<WorkflowEventDetail>(
                                    NodeToolClientProvider.SetWorkflowEventDetail),
                                ibc.Input<WorkflowAssetPersistence>(
                                    NodeToolClientProvider.SetWorkflowAssetPersistence)
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
                    remarks: "Establishes a WebSocket connection to the NodeTool server. The connection is shared across all NodeTool nodes"
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
                    remarks: "Reports the current connection status of the shared NodeTool client"
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
            VlLog.Error("DiagnosticsNodeFactory: vlSelfFactory is null");
            return;
        }

        // Connect node
        var connectNode = CreateConnectNode(vlSelfFactory);
        if (connectNode != null)
        {
            nodeDescriptions.Add(connectNode);
        }

        // Connection status node
        var statusNode = CreateConnectionStatusNode(vlSelfFactory);
        if (statusNode != null)
        {
            nodeDescriptions.Add(statusNode);
        }

        // Image helper nodes (decode ImageRef JSON to bytes/path)
        var decodeImageRefNode = ImageNodeFactory.CreateDecodeImageRefNode(vlSelfFactory);
        if (decodeImageRefNode != null)
        {
            nodeDescriptions.Add(decodeImageRefNode);
        }

        var assetAsFileNode = AssetNodeFactory.CreateAssetAsFileNode(vlSelfFactory);
        if (assetAsFileNode != null)
        {
            nodeDescriptions.Add(assetAsFileNode);
        }
    }
}

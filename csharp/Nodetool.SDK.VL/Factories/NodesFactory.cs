using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using VL.Core;
using VL.Core.CompilerServices;
using Nodetool.SDK.Api.Models;
using Nodetool.SDK.VL.Nodes;
using Nodetool.SDK.VL.Services;
using Nodetool.SDK.Configuration;
using Nodetool.SDK.VL.Utilities;

namespace Nodetool.SDK.VL.Factories
{
    /// <summary>
    /// Factory that creates VL node descriptions from individual Nodetool nodes metadata
    /// </summary>
    internal static class NodesFactory
    {
        private static NodeBuilding.FactoryImpl? _factoryImpl = null;
        private static readonly object _lock = new object();
        private static readonly TimeSpan DiscoveryTimeout = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan InitialSnapshotGrace = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan RefreshDebounce = TimeSpan.FromMilliseconds(250);
        private static readonly ISubject<object> FactoryInvalidated =
            Subject.Synchronize(new Subject<object>());
        private static CancellationTokenSource _refreshCancellation = new();
        private static Task? _refreshTask;
        private static long _refreshRequestVersion;
        private static long _resetGeneration;
        private static bool _hasSuccessfulSnapshot;
        private static bool _factoryWasRequested;
        private static SynchronizationContext? _synchronizationContext;

        // Cached data from the Nodetool API
        private static ImmutableList<NodeMetadataResponse> _fetchedNodes = ImmutableList<NodeMetadataResponse>.Empty;
        private static string _apiStatusMessage = "API data not fetched.";
        private static string _processingSummary = "Node processing summary not yet available.";

        // Public getters for status/debugging
        public static string CurrentApiStatusMessage => _apiStatusMessage;
        public static string CurrentProcessingSummary => _processingSummary;

        public static void Configure(SynchronizationContext? synchronizationContext)
        {
            lock (_lock)
                _synchronizationContext = synchronizationContext;
        }

        public static void RequestRefresh()
        {
            lock (_lock)
            {
                _apiStatusMessage = _fetchedNodes.Count > 0
                    ? "Node metadata refresh requested; current nodes remain available."
                    : "Node metadata refresh requested.";
                _refreshRequestVersion++;
                if (_factoryWasRequested)
                    StartRefreshLoopLocked();
            }
        }

        public static void Reset()
        {
            CancellationTokenSource cancellation;
            lock (_lock)
            {
                cancellation = _refreshCancellation;
                _refreshCancellation = new CancellationTokenSource();
                _refreshTask = null;
                _refreshRequestVersion = 0;
                _resetGeneration++;
                _hasSuccessfulSnapshot = false;
                _factoryWasRequested = false;
                _factoryImpl = null;
                _fetchedNodes = ImmutableList<NodeMetadataResponse>.Empty;
                _apiStatusMessage = "API data not fetched.";
                _processingSummary = "Node processing summary not yet available.";
            }

            cancellation.Cancel();
            cancellation.Dispose();
            SignalFactoryInvalidated();
        }

        /// <summary>
        /// Gets the VL node factory, initializing if necessary
        /// </summary>
        public static NodeBuilding.FactoryImpl GetFactory(IVLNodeDescriptionFactory vlSelfFactory)
        {
            // Add safety check for vlSelfFactory
            if (vlSelfFactory == null)
            {
                VlLog.Error("NodesFactory: vlSelfFactory is null");
                return NodeBuilding.NewFactoryImpl(
                    ImmutableArray<IVLNodeDescription>.Empty,
                    FactoryInvalidated);
            }
            
            Task? initialRefreshTask = null;
            lock (_lock)
            {
                _factoryWasRequested = true;
                if (_factoryImpl != null)
                    return _factoryImpl;

                if (!_hasSuccessfulSnapshot)
                {
                    QueueRefreshLocked();
                    initialRefreshTask = _refreshTask;
                }
            }

            // Existing VL documents resolve factory nodes during their first compilation.
            // Give a fast local server a short grace period to provide that initial snapshot,
            // while keeping offline startup bounded far below the old 30-second wait.
            if (initialRefreshTask != null)
            {
                try
                {
                    initialRefreshTask.Wait(InitialSnapshotGrace);
                }
                catch (AggregateException ex)
                {
                    VlLog.Error($"NodesFactory: initial refresh failed: {VlLog.SafeError(ex.GetBaseException())}");
                }
            }

            lock (_lock)
            {
                if (_factoryImpl != null)
                    return _factoryImpl;
                _factoryImpl = BuildFactoryLocked(vlSelfFactory);
                return _factoryImpl;
            }
        }

        private static NodeBuilding.FactoryImpl BuildFactoryLocked(
            IVLNodeDescriptionFactory vlSelfFactory)
        {
            try
            {
                    var allDescriptions = new List<IVLNodeDescription>();
                    var usedNodeNames = new HashSet<string>();
                    var nameCounters = new Dictionary<string, int>();
                    int successfullyProcessedCount = 0;
                    int failedToProcessCount = 0;

                    // Process each node definition from the metadata
                    foreach (var nodeMetadata in _fetchedNodes)
                    {
                        if (nodeMetadata == null)
                        {
                            failedToProcessCount++;
                            continue;
                        }

                        try
                        {
                            // Generate unique VL node name
                            string vlNodeName = GenerateUniqueNodeName(nodeMetadata, usedNodeNames, nameCounters);
                            usedNodeNames.Add(vlNodeName);

                                                    // Create VL node description using vlSelfFactory.NewNodeDescription pattern for proper tooltips
                        try
                        {
                            var category = DetermineNodeCategory(nodeMetadata.NodeType);
                            var summary = TextCleanup.StripTrailingPeriodsPerLine(
                                nodeMetadata.Description ?? nodeMetadata.Title ?? $"Nodetool {nodeMetadata.NodeType}");
                            
                            var nodeDesc = vlSelfFactory?.NewNodeDescription(
                                name: vlNodeName,
                                category: category,
                                fragmented: false,
                                bc =>
                                {
                                    // Create input pins with documentation
                                    var inputPins = new List<IVLPinDescription>();
                                    
                                    // Add trigger pin with documentation
                                    inputPins.Add(bc.Pin("Execute", typeof(bool), false, 
                                        "⚡ Execute node", 
                                        "Boolean input - set to true to execute the Nodetool node"));

                                    inputPins.Add(new VlPinDescription("Cancel", typeof(bool), false,
                                        "🛑 Cancel execution",
                                        "Boolean input - set to true (rising edge) to cancel the current execution.\n\n"
                                        + "- If the node is not running, this does nothing.\n"
                                        + "- Cancellation is best-effort: the server may take a moment to stop.\n"
                                        + "- The node's last outputs stay latched.",
                                        isVisible: ExecutionPinVisibility.IsInputVisible("Cancel")));

                                    inputPins.Add(new VlPinDescription("AutoRun", typeof(bool), false,
                                        "🔁 Execute on input change",
                                        "When enabled, this node automatically executes whenever any *data input* changes.\n\n"
                                        + "- This watches data pins, not execution-control pins.\n"
                                        + "- Useful for chaining nodes and building autorun patches.\n"
                                        + "- If an input changes while a run is active, behavior depends on RestartOnChange.",
                                        isVisible: ExecutionPinVisibility.IsInputVisible("AutoRun")));

                                    inputPins.Add(new VlPinDescription("RestartOnChange", typeof(bool), false,
                                        "♻️ Restart on input change",
                                        "Only relevant when AutoRun is enabled.\n\n"
                                        + "If true and inputs change while the node is already running:\n"
                                        + "- the current run is cancelled, and\n"
                                        + "- the node restarts immediately with the latest inputs.\n\n"
                                        + "If false:\n"
                                        + "- the node finishes the current run, then reruns once.\n\n"
                                        + "Tip: enable this for interactive tweaking (sliders/knobs). Leave it off for expensive or non-cancellable nodes.",
                                        isVisible: ExecutionPinVisibility.IsInputVisible("RestartOnChange")));
                                    
                                    inputPins.Add(new VlPinDescription("ExecutionTimeoutSeconds", typeof(int), 0,
                                        "Execution timeout override",
                                        "Maximum duration of this node run in seconds. Use 0 to inherit the default from the Nodetool Connect node.",
                                        isVisible: ExecutionPinVisibility.IsInputVisible("ExecutionTimeoutSeconds")));

                                    // Add input pins from node properties with documentation
                                    if (nodeMetadata.Properties != null)
                                    {
                                        foreach (var property in nodeMetadata.Properties)
                                        {
                                            var (vlType, defaultValue) = VlTypeMapping.MapNodeInputType(property.Type);
                                            var targetType = vlType ?? typeof(string);
                                            var initial = VlValueConversion.ConvertOrFallback(property.Default, targetType, defaultValue);
                                            var pinSummary = TextCleanup.StripTrailingPeriod(property.Description ?? property.Title ?? property.Name);
                                            var pinRemarks = BuildPinRemarks(property);
                                            
                                            inputPins.Add(bc.Pin(property.Name, targetType, initial, pinSummary, pinRemarks));
                                        }
                                    }

                                    // Create output pins with documentation
                                    var outputPins = new List<IVLPinDescription>();
                                    
                                    // Add node-specific output pins with documentation
                                    if (nodeMetadata.Outputs != null)
                                    {
                                        foreach (var output in nodeMetadata.Outputs)
                                        {
                                            var (vlType, defaultValue) = MapNodeType(output.Type);
                                            var pinSummary = $"📤 {output.Name}";
                                            var pinRemarks = BuildOutputRemarks(output);
                                            
                                            outputPins.Add(bc.Pin(output.Name, vlType ?? typeof(string), defaultValue, pinSummary, pinRemarks));
                                        }
                                    }
                                    
                                    // Add standard status outputs with documentation
                                    outputPins.Add(bc.Pin("IsRunning", typeof(bool), false,
                                        "⏳ Execution status", 
                                        "True while the node is processing, false when complete or idle"));
                                    outputPins.Add(bc.Pin("On Update", typeof(bool), false,
                                        "⚡ On Update",
                                        "Pulse: goes true briefly when the node run finishes (success/failed/cancelled).\n\n"
                                        + "This does not mean the values actually changed—only that the node executed.\n"
                                        + "Use it to trigger downstream logic."));
                                    outputPins.Add(new VlPinDescription("Error", typeof(string), "",
                                        "❌ Error message", 
                                        "Contains error details if execution fails, empty string if successful",
                                        isVisible: ExecutionPinVisibility.IsOutputVisible("Error")));
                                    outputPins.Add(new VlPinDescription("Debug", typeof(string), "",
                                        summary: "🪵 Debug (last updates)",
                                        remarks: "Last few runner updates (progress/node_update/output_update). Useful when results are partial or missing",
                                        isVisible: false));

                                    // Build comprehensive node documentation
                                    var nodeRemarks = BuildNodeRemarks(nodeMetadata);
                                    
                                    return bc.Node(
                                        inputs: inputPins,
                                        outputs: outputPins,
                                        newNode: ibc => new NodeBase(ibc.NodeContext, nodeMetadata),
                                        summary: summary,
                                        remarks: nodeRemarks
                                    );
                                }
                            );
                            
                            if (nodeDesc != null)
                            {
                                allDescriptions.Add(nodeDesc);
                                successfullyProcessedCount++;
                            }
                            else
                            {
                                failedToProcessCount++;
                                VlLog.Error($"NodesFactory: vlSelfFactory returned null for '{nodeMetadata.NodeType}'");
                            }
                        }
                            catch (Exception ex)
                            {
                                failedToProcessCount++;
                                VlLog.Error($"NodesFactory: error creating VL node '{nodeMetadata.NodeType}': {VlLog.SafeError(ex)}");
                            }
                        }
                        catch (Exception ex)
                        {
                            failedToProcessCount++;
                            VlLog.Error($"NodesFactory: error processing node '{nodeMetadata.NodeType}': {VlLog.SafeError(ex)}");
                        }
                    }

                    _processingSummary = $"Processed {successfullyProcessedCount} nodes successfully (Failed: {failedToProcessCount}) from {_fetchedNodes.Count} total definitions.";
                    VlLog.Debug(_processingSummary);

                    // Add diagnostic status node
                    try
                    {
                        var statusNode = vlSelfFactory?.NewNodeDescription(
                            name: "NodesAPIStatus",
                            category: "Nodetool Nodes.Status",
                            fragmented: false,
                            bc =>
                            {
                                var statusPin = bc.Pin("Status", typeof(string));
                                var summaryPin = bc.Pin("ProcessingSummary", typeof(string));
                                var nodeCountPin = bc.Pin("NodeCount", typeof(int));

                                return bc.Node(
                                    inputs: Enumerable.Empty<IVLPinDescription>(),
                                    outputs: new IVLPinDescription[] { statusPin, summaryPin, nodeCountPin },
                                    newNode: ibc => ibc.Node(
                                        inputs: Enumerable.Empty<IVLPin>(),
                                        outputs: new IVLPin[] {
                                            ibc.Output<string>(() => _apiStatusMessage),
                                            ibc.Output<string>(() => _processingSummary),
                                            ibc.Output<int>(() => _fetchedNodes.Count)
                                        }
                                    )
                                );
                            }
                        );
                        
                        if (statusNode != null)
                        {
                            allDescriptions.Add(statusNode);
                        }
                        else
                        {
                            VlLog.Error("NodesFactory: failed to create NodesAPIStatus node (vlSelfFactory returned null)");
                        }
                    }
                    catch (Exception ex)
                    {
                        VlLog.Error($"NodesFactory: error creating NodesAPIStatus node: {VlLog.SafeError(ex)}");
                    }

                    // Note: diagnostics nodes (Connect/ConnectionStatus) are provided by DiagnosticsNodeFactory.
                    // Avoid duplicating them here to prevent duplicate node descriptions under the same category/name.

                var factory = NodeBuilding.NewFactoryImpl(
                    ImmutableArray.CreateRange(allDescriptions),
                    FactoryInvalidated);
                if (_hasSuccessfulSnapshot)
                    VlReadinessLog.MarkNodeFactoryResolved();
                return factory;
            }
            catch (Exception ex)
            {
                VlLog.Error($"NodesFactory: factory build failed: {ex.GetType().Name}: {VlLog.SafeError(ex)}");

                return NodeBuilding.NewFactoryImpl(
                    ImmutableArray<IVLNodeDescription>.Empty,
                    FactoryInvalidated);
            }
        }

        private static void QueueRefreshLocked()
        {
            _refreshRequestVersion++;
            StartRefreshLoopLocked();
        }

        private static void StartRefreshLoopLocked()
        {
            if (_refreshTask is { IsCompleted: false })
                return;

            var generation = _resetGeneration;
            var cancellationToken = _refreshCancellation.Token;
            _refreshTask = Task.Run(
                () => RunRefreshLoopAsync(generation, cancellationToken),
                CancellationToken.None);
        }

        private static async Task RunRefreshLoopAsync(
            long generation,
            CancellationToken cancellationToken)
        {
            long processedVersion = 0;
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    lock (_lock)
                    {
                        if (generation != _resetGeneration)
                            return;
                        if (_refreshRequestVersion == processedVersion)
                            return;
                    }

                    await Task.Delay(RefreshDebounce, cancellationToken).ConfigureAwait(false);

                    lock (_lock)
                    {
                        if (generation != _resetGeneration)
                            return;
                        processedVersion = _refreshRequestVersion;
                    }

                    try
                    {
                        var nodes = await FetchNodeMetadataAsync(cancellationToken)
                            .ConfigureAwait(false);
                        var hadPublishedFactory = false;
                        lock (_lock)
                        {
                            if (generation != _resetGeneration)
                                return;

                            hadPublishedFactory = _factoryImpl is not null;
                            _fetchedNodes = nodes;
                            _apiStatusMessage =
                                $"Successfully fetched {_fetchedNodes.Count} node definitions";
                            _hasSuccessfulSnapshot = true;
                            _factoryImpl = null;
                        }

                        VlReadinessLog.MarkNodeDiscovery(nodes.Count);
                        VlLog.Debug($"NodesFactory: {_apiStatusMessage}");
                        if (hadPublishedFactory)
                            SignalFactoryInvalidated();
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        bool retry;
                        lock (_lock)
                        {
                            if (generation != _resetGeneration)
                                return;
                            HandleApiError(ex);
                            retry = !_hasSuccessfulSnapshot;
                        }

                        if (retry)
                        {
                            await Task.Delay(RetryDelay, cancellationToken).ConfigureAwait(false);
                            lock (_lock)
                            {
                                if (generation != _resetGeneration)
                                    return;
                                if (_refreshRequestVersion == processedVersion)
                                    _refreshRequestVersion++;
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Reset/configuration changes cancel the old refresh generation.
            }
            finally
            {
                lock (_lock)
                {
                    if (generation == _resetGeneration)
                    {
                        _refreshTask = null;
                        if (!cancellationToken.IsCancellationRequested &&
                            _refreshRequestVersion != processedVersion)
                        {
                            StartRefreshLoopLocked();
                        }
                    }
                }
            }
        }

        private static async Task<ImmutableList<NodeMetadataResponse>> FetchNodeMetadataAsync(
            CancellationToken cancellationToken)
        {
            var apiBase = NodeToolClientProvider.CurrentApiBaseUrl?.ToString().TrimEnd('/')
                          ?? NodetoolConstants.Defaults.BaseUrl;
            VlLog.Debug($"NodesFactory: Target URL: {apiBase}{NodetoolConstants.Endpoints.NodesMetadata}");

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(DiscoveryTimeout);
            VlLog.Debug("NodesFactory: fetching node metadata...");
            var client = await NodeToolClientProvider
                .GetApiClientAsync(timeout.Token)
                .ConfigureAwait(false);
            var nodes = await client.GetNodeTypesAsync(timeout.Token).ConfigureAwait(false);
            return nodes?.ToImmutableList() ?? ImmutableList<NodeMetadataResponse>.Empty;
        }

        private static void SignalFactoryInvalidated()
        {
            void Publish()
            {
                try
                {
                    FactoryInvalidated.OnNext(new object());
                }
                catch (Exception ex)
                {
                    VlLog.Error($"NodesFactory: invalidation failed: {VlLog.SafeError(ex)}");
                }
            }

            SynchronizationContext? synchronizationContext;
            lock (_lock)
                synchronizationContext = _synchronizationContext;

            if (synchronizationContext != null)
            {
                try
                {
                    synchronizationContext.Post(_ => Publish(), null);
                    return;
                }
                catch (Exception ex)
                {
                    VlLog.Error($"NodesFactory: failed to post invalidation: {VlLog.SafeError(ex)}");
                }
            }

            ThreadPool.QueueUserWorkItem(_ => Publish());
        }

        /// <summary>
        /// Handle API errors with detailed logging and user guidance
        /// </summary>
        private static void HandleApiError(Exception ex)
        {
            var hasStaleNodes = _fetchedNodes.Count > 0;
            string errorCategory = "Unknown";
            string userGuidance = "";
            
            // Categorize the error and provide specific guidance
            switch (ex)
            {
                case HttpRequestException httpEx:
                    errorCategory = "HTTP Request Failed";
                    _apiStatusMessage = $"🔌 API Connection Error: Cannot reach Nodetool API at {NodetoolConstants.Defaults.BaseUrl}";
                    userGuidance = GetNetworkErrorGuidance();
                    break;
                    
                case OperationCanceledException:
                    errorCategory = "Request Timeout";
                    _apiStatusMessage = $"API Timeout: Nodetool API did not respond within {DiscoveryTimeout.TotalSeconds:0} seconds";
                    userGuidance = GetTimeoutErrorGuidance();
                    break;
                    
                case System.Net.Sockets.SocketException:
                    errorCategory = "Network Connection Failed";
                    _apiStatusMessage = $"🔌 Network Error: Cannot establish connection to Nodetool API";
                    userGuidance = GetNetworkErrorGuidance();
                    break;
                    
                case System.Net.WebException webEx:
                    errorCategory = "Web Request Failed";
                    _apiStatusMessage = $"🌐 Web Error: {VlLog.SafeError(webEx)}";
                    userGuidance = GetNetworkErrorGuidance();
                    break;
                    
                default:
                    errorCategory = "API Error";
                    _apiStatusMessage = $"❌ Unexpected Error: {VlLog.SafeError(ex)}";
                    userGuidance = "Check the console output for detailed error information.";
                    break;
            }

            if (hasStaleNodes)
                _apiStatusMessage = $"Stale node descriptions retained. {_apiStatusMessage}";

            // Keep default startup logs concise; show full troubleshooting only in verbose mode.
            VlReadinessLog.ReportError(
                "node discovery",
                $"{errorCategory}: {_apiStatusMessage}");

            if (VlLog.Verbose)
            {
                VlLog.Debug("");
                VlLog.Debug("=================== NODETOOL API ERROR ===================");
                VlLog.Debug("🚨 NODES CANNOT BE CREATED - API UNREACHABLE");
                VlLog.Debug("");
                VlLog.Debug($"Error Category: {errorCategory}");
                VlLog.Debug($"API Endpoint: {NodetoolConstants.Defaults.BaseUrl}{NodetoolConstants.Endpoints.NodesMetadata}");
                VlLog.Debug($"Status: {_apiStatusMessage}");
                VlLog.Debug("");
                VlLog.Debug("📋 USER ACTION REQUIRED:");
                VlLog.Debug(userGuidance);
                VlLog.Debug("");
                VlLog.Debug("🔧 Technical Details:");
                VlLog.Debug($"   Error Type: {ex.GetType().Name}");
                VlLog.Debug($"   Message: {VlLog.SafeError(ex)}");
                if (ex.InnerException != null)
                {
                    VlLog.Debug($"   Inner Error: {ex.InnerException.GetType().Name}: {VlLog.SafeError(ex.InnerException)}");
                }
                VlLog.Debug($"   Timeout Setting: {DiscoveryTimeout.TotalSeconds:0} seconds");
                VlLog.Debug("");
                VlLog.Debug("🔍 Troubleshooting Steps:");
                VlLog.Debug("   1. Verify Nodetool server is running");
                VlLog.Debug($"   2. Check API URL: {NodetoolConstants.Defaults.BaseUrl}");
                VlLog.Debug($"   3. Test API manually: GET {NodetoolConstants.Defaults.BaseUrl}{NodetoolConstants.Endpoints.NodesMetadata}");
                VlLog.Debug("   4. Check firewall/network settings");
                VlLog.Debug("   5. Verify Nodetool server health");
                VlLog.Debug("===========================================================");
                VlLog.Debug("");
            }
            
        }

        /// <summary>
        /// Get user guidance for network-related errors
        /// </summary>
        private static string GetNetworkErrorGuidance()
        {
            return @"1. Ensure Nodetool server is running and accessible
   2. Verify the API URL configuration is correct
   3. Check your network connection and firewall settings
   4. Try accessing the API URL directly in a browser
   5. Confirm Nodetool server is listening on the expected port";
        }

        /// <summary>
        /// Get user guidance for timeout errors
        /// </summary>
        private static string GetTimeoutErrorGuidance()
        {
            return @"1. Check if Nodetool server is responding slowly
   2. Verify server resources (CPU, memory) are sufficient
   3. Consider increasing timeout in NodetoolConstants.Defaults.TimeoutSeconds
   4. Check network latency to the server
   5. Restart Nodetool server if it appears hung";
        }

        /// <summary>
        /// Generate a unique VL node name from node metadata
        /// </summary>
        private static string GenerateUniqueNodeName(NodeMetadataResponse nodeMetadata, HashSet<string> usedNames, Dictionary<string, int> nameCounters)
        {
            // Use the node type as base name (e.g., "nodetool.constant.Float" -> "Float")
            var baseName = nodeMetadata.NodeType?.Split('.').LastOrDefault() ?? "UnknownNode";
            
            // Clean up the name for VL (remove special characters)
            baseName = baseName.Replace("_", "").Replace("-", "");
            
            // Ensure uniqueness
            string candidateName = baseName;
            int counter = 1;
            
            while (usedNames.Contains(candidateName))
            {
                counter++;
                candidateName = $"{baseName}{counter}";
            }
            
            return candidateName;
        }

        /// <summary>
        /// Map Nodetool type to VL type
        /// </summary>
        private static (Type?, object?) MapNodeType(NodeTypeDefinition? nodeType)
        {
            return VlTypeMapping.MapNodeType(nodeType);
        }

        /// <summary>
        /// Determine the VL category for a node based on its type
        /// </summary>
        private static string DetermineNodeCategory(string? nodeType)
        {
            if (string.IsNullOrEmpty(nodeType))
                return "Nodetool Nodes.General";
            
            // Parse category from node type (e.g., "nodetool.constant.Float" -> "Constant")
            var parts = nodeType.Split('.');
            if (parts.Length >= 2)
            {
                var category = parts[1]; // e.g., "constant", "image", "audio"
                return $"Nodetool Nodes.{char.ToUpper(category[0])}{category.Substring(1)}";
            }
            
            return "Nodetool Nodes.General";
        }

        /// <summary>
        /// Build comprehensive pin documentation for input properties
        /// </summary>
        private static string BuildPinRemarks(NodeProperty property)
        {
            var parts = new List<string>();
            
            if (property.Type?.Type != null)
                parts.Add($"Type: {property.Type.Type}");
                
            if (property.Min != null || property.Max != null)
            {
                if (property.Min != null && property.Max != null)
                    parts.Add($"Range: {property.Min} - {property.Max}");
                else if (property.Min != null)
                    parts.Add($"Min: {property.Min}");
                else
                    parts.Add($"Max: {property.Max}");
            }
            
            if (property.Default != null)
                parts.Add($"Default: {property.Default}");
                
            if (property.Type?.Optional == true)
                parts.Add("(Optional)");
                
            return string.Join(" | ", parts);
        }
        
        /// <summary>
        /// Build documentation for output pins
        /// </summary>
        private static string BuildOutputRemarks(NodeOutput output)
        {
            var parts = new List<string>();
            
            if (output.Type?.Type != null)
                parts.Add($"Type: {output.Type.Type}");
                
            parts.Add("Node output");
            
            return string.Join(" | ", parts);
        }
        
        /// <summary>
        /// Build comprehensive node documentation
        /// </summary>
        private static string BuildNodeRemarks(NodeMetadataResponse nodeMetadata)
        {
            static string TrimTrailingPeriod(string s)
                => s.EndsWith(".", StringComparison.Ordinal) ? s.TrimEnd('.') : s;

            // vvvv shows Summary + Remarks; keep Remarks short and non-duplicative.
            // Requested style:
            // - show namespace (no "NodeTool Type:" label)
            // - no title
            // - no "2 inputs, 2 outputs"
            var ns = (nodeMetadata.Namespace ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(ns))
                return TrimTrailingPeriod(ns);

            // Fallback if namespace is missing
            var nodeType = (nodeMetadata.NodeType ?? "").Trim();
            return TrimTrailingPeriod(nodeType);
        }
    }
}

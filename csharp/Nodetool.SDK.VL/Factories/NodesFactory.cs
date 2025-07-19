using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using VL.Core;
using VL.Core.CompilerServices;
using Nodetool.SDK.Api.Models;
using Nodetool.SDK.VL.Nodes;
using Nodetool.SDK.VL.Services;
using Nodetool.SDK.Configuration;

namespace Nodetool.SDK.VL.Factories
{
    /// <summary>
    /// Factory that creates VL node descriptions from individual Nodetool nodes metadata
    /// </summary>
    internal static class NodesFactory
    {
        private static NodeBuilding.FactoryImpl? _factoryImpl = null;
        private static bool _isInitialized = false;
        private static readonly object _lock = new object();

        // Cached data from the Nodetool API
        private static ImmutableList<NodeMetadataResponse> _fetchedNodes = ImmutableList<NodeMetadataResponse>.Empty;
        private static string _apiStatusMessage = "API data not fetched.";
        private static string _processingSummary = "Node processing summary not yet available.";

        // Public getters for status/debugging
        public static string CurrentApiStatusMessage => _apiStatusMessage;
        public static string CurrentProcessingSummary => _processingSummary;

        /// <summary>
        /// Gets the VL node factory, initializing if necessary
        /// </summary>
        public static NodeBuilding.FactoryImpl GetFactory(IVLNodeDescriptionFactory vlSelfFactory)
        {
            Console.WriteLine("=== NodesFactory.GetFactory called ===");
            Console.WriteLine($"NodesFactory: vlSelfFactory type: {vlSelfFactory?.GetType().Name ?? "null"}");
            
            // Add safety check for vlSelfFactory
            if (vlSelfFactory == null)
            {
                Console.WriteLine("NodesFactory: ERROR - vlSelfFactory is null!");
                return NodeBuilding.NewFactoryImpl(ImmutableArray<IVLNodeDescription>.Empty);
            }
            
            lock (_lock)
            {
                if (_isInitialized && _factoryImpl != null)
                {
                    Console.WriteLine("NodesFactory: Returning cached factory instance");
                    return _factoryImpl;
                }

                Console.WriteLine("=== NodesFactory: Performing one-time initialization ===");
                
                try
                {
                    PerformGlobalDataFetchAndStore();

                    var allDescriptions = new List<IVLNodeDescription>();
                    var usedNodeNames = new HashSet<string>();
                    var nameCounters = new Dictionary<string, int>();
                    int successfullyProcessedCount = 0;
                    int failedToProcessCount = 0;

                    Console.WriteLine($"NodesFactory: Processing {_fetchedNodes.Count} fetched node definitions...");
                    
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
                            var summary = nodeMetadata.Description ?? nodeMetadata.Title ?? $"Nodetool {nodeMetadata.NodeType}";
                            
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
                                    
                                    // Add input pins from node properties with documentation
                                    if (nodeMetadata.Properties != null)
                                    {
                                        foreach (var property in nodeMetadata.Properties)
                                        {
                                            var (vlType, defaultValue) = MapNodeType(property.Type);
                                            var pinSummary = property.Description ?? property.Title ?? property.Name;
                                            var pinRemarks = BuildPinRemarks(property);
                                            
                                            inputPins.Add(bc.Pin(property.Name, vlType ?? typeof(string), defaultValue, pinSummary, pinRemarks));
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
                                    outputPins.Add(bc.Pin("Error", typeof(string), "",
                                        "❌ Error message", 
                                        "Contains error details if execution fails, empty string if successful"));

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
                                var inputCount = nodeMetadata.Properties?.Count ?? 0;
                                var outputCount = nodeMetadata.Outputs?.Count ?? 0;
                                Console.WriteLine($"✅ NodesFactory: Created documented VL node '{vlNodeName}' from '{nodeMetadata.NodeType}' ({inputCount + 1} inputs, {outputCount + 2} outputs with tooltips)");
                            }
                            else
                            {
                                failedToProcessCount++;
                                Console.WriteLine($"NodesFactory: Failed to create node description for '{nodeMetadata.NodeType}' - vlSelfFactory returned null");
                            }
                        }
                            catch (Exception ex)
                            {
                                failedToProcessCount++;
                                Console.WriteLine($"NodesFactory: Error creating NodeDescription for '{nodeMetadata.NodeType}': {ex.Message}");
                                Console.WriteLine($"NodesFactory: Stack trace: {ex.StackTrace}");
                            }
                        }
                        catch (Exception ex)
                        {
                            failedToProcessCount++;
                            Console.WriteLine($"NodesFactory: Error processing node '{nodeMetadata.NodeType}': {ex.Message}");
                        }
                    }

                    _processingSummary = $"Processed {successfullyProcessedCount} nodes successfully (Failed: {failedToProcessCount}) from {_fetchedNodes.Count} total definitions.";
                    Console.WriteLine($"NodesFactory: {_processingSummary}");

                    // Add diagnostic status node
                    Console.WriteLine("NodesFactory: Creating diagnostic status node...");
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
                            Console.WriteLine("NodesFactory: Diagnostic status node created successfully");
                        }
                        else
                        {
                            Console.WriteLine("NodesFactory: Failed to create diagnostic status node - vlSelfFactory returned null");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"NodesFactory: Error creating status node: {ex.Message}");
                        Console.WriteLine($"NodesFactory: Status node stack trace: {ex.StackTrace}");
                    }

                    Console.WriteLine($"NodesFactory: Creating factory with {allDescriptions.Count} node descriptions...");
                    _factoryImpl = NodeBuilding.NewFactoryImpl(ImmutableArray.CreateRange(allDescriptions));
                    _isInitialized = true;
                    Console.WriteLine($"=== NodesFactory: Factory created successfully with {allDescriptions.Count} node descriptions ===");
                    return _factoryImpl;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"=== NodesFactory: CRITICAL ERROR during factory initialization ===");
                    Console.WriteLine($"NodesFactory: Error message: {ex.Message}");
                    Console.WriteLine($"NodesFactory: Error type: {ex.GetType().Name}");
                    Console.WriteLine($"NodesFactory: Stack trace: {ex.StackTrace}");
                    
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"NodesFactory: Inner exception: {ex.InnerException.Message}");
                        Console.WriteLine($"NodesFactory: Inner stack trace: {ex.InnerException.StackTrace}");
                    }
                    
                    // Return empty factory to prevent VL from considering it "not found"
                    _factoryImpl = NodeBuilding.NewFactoryImpl(ImmutableArray<IVLNodeDescription>.Empty);
                    _isInitialized = true;
                    Console.WriteLine("NodesFactory: Returning empty factory due to initialization error");
                    return _factoryImpl;
                }
            }
        }

        /// <summary>
        /// Fetches node metadata from the API and stores it
        /// </summary>
        private static void PerformGlobalDataFetchAndStore()
        {
            Console.WriteLine("=== NodesFactory: Fetching node metadata from API ===");
            Console.WriteLine($"NodesFactory: Target URL: {NodetoolConstants.Defaults.BaseUrl}{NodetoolConstants.Endpoints.NodesMetadata}");
            
            try
            {
                using var client = new Nodetool.SDK.Api.NodetoolClient();
                client.Configure(NodetoolConstants.Defaults.BaseUrl);
                Console.WriteLine("NodesFactory: Created NodetoolClient instance");
                
                // Since we can't use async in static constructor context, we need to handle this differently
                Console.WriteLine("NodesFactory: Starting async metadata fetch...");
                var task = Task.Run(async () => await client.GetNodeTypesAsync());
                
                Console.WriteLine($"NodesFactory: Waiting for API response ({NodetoolConstants.Defaults.TimeoutSeconds} second timeout)...");
                bool completed = task.Wait(TimeSpan.FromSeconds(NodetoolConstants.Defaults.TimeoutSeconds));
                
                if (!completed)
                {
                    _apiStatusMessage = $"Timeout waiting for API response after {NodetoolConstants.Defaults.TimeoutSeconds} seconds";
                    Console.WriteLine($"NodesFactory: {_apiStatusMessage}");
                    _fetchedNodes = ImmutableList<NodeMetadataResponse>.Empty;
                    return;
                }
                
                var nodes = task.Result;
                _fetchedNodes = nodes?.ToImmutableList() ?? ImmutableList<NodeMetadataResponse>.Empty;
                _apiStatusMessage = $"Successfully fetched {_fetchedNodes.Count} node definitions";
                
                Console.WriteLine($"=== NodesFactory: Successfully fetched {_fetchedNodes.Count} node definitions ===");
                Console.WriteLine($"NodesFactory: API Status: {_apiStatusMessage}");
            }
            catch (AggregateException aggEx)
            {
                var innerEx = aggEx.InnerException ?? aggEx;
                _apiStatusMessage = $"Error fetching metadata: {innerEx.Message}";
                Console.WriteLine($"=== NodesFactory: API FETCH ERROR (AggregateException) ===");
                Console.WriteLine($"NodesFactory: Error message: {_apiStatusMessage}");
                Console.WriteLine($"NodesFactory: Error type: {innerEx.GetType().Name}");
                Console.WriteLine($"NodesFactory: Stack trace: {innerEx.StackTrace}");
                _fetchedNodes = ImmutableList<NodeMetadataResponse>.Empty;
            }
            catch (Exception ex)
            {
                _apiStatusMessage = $"General error during metadata fetch: {ex.Message}";
                Console.WriteLine($"=== NodesFactory: API FETCH ERROR (General) ===");
                Console.WriteLine($"NodesFactory: Error message: {_apiStatusMessage}");
                Console.WriteLine($"NodesFactory: Error type: {ex.GetType().Name}");
                Console.WriteLine($"NodesFactory: Stack trace: {ex.StackTrace}");
                _fetchedNodes = ImmutableList<NodeMetadataResponse>.Empty;
            }
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
            if (nodeType == null || string.IsNullOrEmpty(nodeType.Type))
                return (typeof(string), "");

            return nodeType.Type.ToLowerInvariant() switch
            {
                "str" or "string" => (typeof(string), ""),
                "int" or "integer" => (typeof(int), 0),
                "float" or "number" => (typeof(float), 0.0f),
                "bool" or "boolean" => (typeof(bool), false),
                "list" or "array" => (typeof(string[]), new string[0]),
                "dict" or "object" => (typeof(object), null),
                "image" => (typeof(string), ""), // Image path or base64
                "audio" => (typeof(string), ""), // Audio path or data
                "video" => (typeof(string), ""), // Video path or data
                _ => (typeof(string), "")
            };
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
            var parts = new List<string>();
            parts.Add($"NodeTool Type: {nodeMetadata.NodeType}");
            
            if (!string.IsNullOrWhiteSpace(nodeMetadata.Title) && 
                nodeMetadata.Title != nodeMetadata.NodeType)
                parts.Add($"Title: {nodeMetadata.Title}");
                
            var inputCount = nodeMetadata.Properties?.Count ?? 0;
            var outputCount = nodeMetadata.Outputs?.Count ?? 0;
            parts.Add($"📌 {inputCount} inputs, {outputCount} outputs");
            
            if (!string.IsNullOrWhiteSpace(nodeMetadata.Description))
                parts.Add($"Description: {nodeMetadata.Description}");
            
            return string.Join("\n", parts);
        }
    }
} 
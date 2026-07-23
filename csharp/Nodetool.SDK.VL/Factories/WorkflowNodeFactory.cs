using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using VL.Core;
using VL.Core.CompilerServices;
using Nodetool.SDK.VL.Models;
using Nodetool.SDK.VL.Services;
using Nodetool.SDK.VL.Nodes;
using Nodetool.SDK.Configuration;
using Microsoft.Extensions.Logging;
using Nodetool.SDK.VL.Utilities;

namespace Nodetool.SDK.VL.Factories
{
    /// <summary>
    /// Factory that creates VL node descriptions from Nodetool workflow metadata
    /// </summary>
    internal static class WorkflowNodeFactory
    {
        private static NodeBuilding.FactoryImpl? _factoryImpl = null;
        private static bool _isInitialized = false;
        private static DateTimeOffset _retryAfter = DateTimeOffset.MinValue;
        private static readonly object _lock = new object();
        private static readonly TimeSpan DiscoveryTimeout = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

        // Cached data from the Nodetool API
        private static ImmutableList<WorkflowDetail> _fetchedWorkflows = ImmutableList<WorkflowDetail>.Empty;
        private static string _apiStatusMessage = "API data not fetched.";
        private static string _processingSummary = "Workflow processing summary not yet available.";

        // Public getters for status/debugging
        public static string CurrentApiStatusMessage => _apiStatusMessage;
        public static string CurrentProcessingSummary => _processingSummary;

        public static void Reset()
        {
            lock (_lock)
            {
                _factoryImpl = null;
                _isInitialized = false;
                _retryAfter = DateTimeOffset.MinValue;
                _fetchedWorkflows = ImmutableList<WorkflowDetail>.Empty;
                _apiStatusMessage = "API data not fetched.";
                _processingSummary = "Workflow processing summary not yet available.";
            }
        }

        /// <summary>
        /// Gets the VL node factory, initializing if necessary
        /// </summary>
        public static NodeBuilding.FactoryImpl GetFactory(IVLNodeDescriptionFactory vlSelfFactory)
        {
            // Add safety check for vlSelfFactory
            if (vlSelfFactory == null)
            {
                VlLog.Error("WorkflowNodeFactory: vlSelfFactory is null");
                return NodeBuilding.NewFactoryImpl(ImmutableArray<IVLNodeDescription>.Empty);
            }
            
            lock (_lock)
            {
                if (_factoryImpl != null &&
                    (_isInitialized || DateTimeOffset.UtcNow < _retryAfter))
                {
                    return _factoryImpl;
                }
                
                try
                {
                    var fetchSucceeded = PerformGlobalDataFetchAndStore();

                    var allDescriptions = new List<IVLNodeDescription>();
                    var nodeNames = BuildStableNodeNames(_fetchedWorkflows);
                    int successfullyProcessedCount = 0;
                    int failedToProcessCount = 0;

                    // Process each workflow definition from the metadata
                    foreach (var workflow in _fetchedWorkflows)
                    {
                        if (workflow == null)
                        {
                            failedToProcessCount++;
                            continue;
                        }

                        try
                        {
                            // Generate unique VL node name
                            string vlNodeName = nodeNames[workflow];

                            // Create WorkflowNodeDescription (following VL.NodetoolNodes pattern)
                            try
                            {
                                var category = "Nodetool Workflows";
                                
                                var workflowNodeDesc = new Nodes.WorkflowNodeDescription(
                                    workflow, 
                                    vlNodeName, 
                                    category, 
                                    vlSelfFactory);

                                allDescriptions.Add(workflowNodeDesc);
                                successfullyProcessedCount++;
                            }
                            catch (Exception ex)
                            {
                                failedToProcessCount++;
                                VlLog.Error($"WorkflowNodeFactory: error creating node for '{workflow.Name}': {ex.Message}");
                            }
                        }
                        catch (Exception ex)
                        {
                            failedToProcessCount++;
                            VlLog.Error($"WorkflowNodeFactory: error processing workflow '{workflow.Name}': {ex.Message}");
                        }
                    }

                    _processingSummary = $"Processed {successfullyProcessedCount} workflows successfully (Failed: {failedToProcessCount}) from {_fetchedWorkflows.Count} total definitions.";
                    VlLog.Info(_processingSummary);

                    // Add diagnostic status node using lambda-based factory approach
                    try
                    {
                        var statusNode = vlSelfFactory?.NewNodeDescription(
                            name: "WorkflowAPIStatus",
                            category: "Nodetool Workflows",
                            fragmented: false,
                            bc =>
                            {
                                var statusPin = bc.Pin("Status", typeof(string));
                                var summaryPin = bc.Pin("ProcessingSummary", typeof(string));
                                var workflowCountPin = bc.Pin("WorkflowCount", typeof(int));

                                return bc.Node(
                                    inputs: Enumerable.Empty<IVLPinDescription>(),
                                    outputs: new IVLPinDescription[] { statusPin, summaryPin, workflowCountPin },
                                    newNode: ibc => ibc.Node(
                                        inputs: Enumerable.Empty<IVLPin>(),
                                        outputs: new IVLPin[] {
                                            ibc.Output<string>(() => _apiStatusMessage),
                                            ibc.Output<string>(() => _processingSummary),
                                            ibc.Output<int>(() => _fetchedWorkflows.Count)
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
                            VlLog.Error("WorkflowNodeFactory: failed to create WorkflowAPIStatus node (vlSelfFactory returned null)");
                        }
                    }
                    catch (Exception ex)
                    {
                        VlLog.Error($"WorkflowNodeFactory: error creating WorkflowAPIStatus node: {ex.Message}");
                    }

                    _factoryImpl = NodeBuilding.NewFactoryImpl(ImmutableArray.CreateRange(allDescriptions));
                    _isInitialized = fetchSucceeded;
                    _retryAfter = fetchSucceeded
                        ? DateTimeOffset.MinValue
                        : DateTimeOffset.UtcNow.Add(RetryDelay);
                    return _factoryImpl;
                }
                catch (Exception ex)
                {
                    VlLog.Error($"WorkflowNodeFactory: initialization failed: {ex.GetType().Name}: {ex.Message}");
                    
                    // Return empty factory to prevent VL from considering it "not found"
                    _factoryImpl = NodeBuilding.NewFactoryImpl(ImmutableArray<IVLNodeDescription>.Empty);
                    _isInitialized = false;
                    _retryAfter = DateTimeOffset.UtcNow.Add(RetryDelay);
                    return _factoryImpl;
                }
            }
        }

        /// <summary>
        /// Fetches workflow metadata from the API and stores it
        /// </summary>
        private static bool PerformGlobalDataFetchAndStore()
        {
            var apiBase = NodeToolClientProvider.CurrentApiBaseUrl?.ToString().TrimEnd('/')
                          ?? NodetoolConstants.Defaults.BaseUrl;
            VlLog.Debug($"WorkflowNodeFactory: Target URL: {apiBase}{NodetoolConstants.Endpoints.Workflows}");
            
            try
            {
                using var metadataService = new WorkflowMetadataService();

                // Ensure the metadata service uses the same API base URL as the Connect node.
                metadataService.Configure(new NodetoolOptions
                {
                    BaseUrl = apiBase,
                    ApiKey = NodeToolClientProvider.CurrentAuthToken
                });
                
                using var timeout = new CancellationTokenSource(DiscoveryTimeout);
                var workflows = metadataService
                    .FetchWorkflowMetadataAsync(timeout.Token)
                    .GetAwaiter()
                    .GetResult();
                _fetchedWorkflows = workflows?.ToImmutableList() ?? ImmutableList<WorkflowDetail>.Empty;
                _apiStatusMessage = metadataService.StatusMessage;
                VlLog.Debug($"WorkflowNodeFactory: {_apiStatusMessage} ({_fetchedWorkflows.Count} workflows)");
                return true;
            }
            catch (AggregateException aggEx)
            {
                var innerEx = aggEx.InnerException ?? aggEx;
                HandleWorkflowApiError(innerEx);
                return false;
            }
            catch (Exception ex)
            {
                HandleWorkflowApiError(ex);
                return false;
            }
        }

        /// <summary>
        /// Handle workflow API errors with detailed logging and user guidance
        /// </summary>
        private static void HandleWorkflowApiError(Exception ex)
        {
            string errorCategory = "Unknown";
            string userGuidance = "";
            
            // Categorize the error and provide specific guidance
            switch (ex)
            {
                case HttpRequestException httpEx:
                    errorCategory = "HTTP Request Failed";
                    _apiStatusMessage = $"🔌 Workflow API Connection Error: Cannot reach Nodetool API";
                    userGuidance = GetWorkflowNetworkErrorGuidance();
                    break;
                    
                case OperationCanceledException:
                    errorCategory = "Request Timeout";
                    _apiStatusMessage = $"⏱️ Workflow API Timeout: Nodetool API did not respond in time";
                    userGuidance = GetWorkflowTimeoutErrorGuidance();
                    break;
                    
                case System.Net.Sockets.SocketException:
                    errorCategory = "Network Connection Failed";
                    _apiStatusMessage = $"🔌 Workflow Network Error: Cannot establish connection to Nodetool API";
                    userGuidance = GetWorkflowNetworkErrorGuidance();
                    break;
                    
                case System.Net.WebException webEx:
                    errorCategory = "Web Request Failed";
                    _apiStatusMessage = $"🌐 Workflow Web Error: {webEx.Message}";
                    userGuidance = GetWorkflowNetworkErrorGuidance();
                    break;
                    
                default:
                    errorCategory = "Workflow API Error";
                    _apiStatusMessage = $"❌ Workflow Fetch Error: {ex.Message}";
                    userGuidance = "Check the console output for detailed error information.";
                    break;
            }

            // Log comprehensive error information
            // Keep default startup logs concise; show full troubleshooting only in verbose mode.
            VlLog.Error($"Workflows API error ({errorCategory}): {_apiStatusMessage}");

            if (VlLog.Verbose)
            {
                Console.WriteLine("");
                Console.WriteLine("================= NODETOOL WORKFLOW API ERROR =================");
                Console.WriteLine("🚨 WORKFLOW NODES CANNOT BE CREATED - API UNREACHABLE");
                Console.WriteLine("");
                Console.WriteLine($"Error Category: {errorCategory}");
                Console.WriteLine($"Status: {_apiStatusMessage}");
                Console.WriteLine("");
                Console.WriteLine("📋 USER ACTION REQUIRED:");
                Console.WriteLine(userGuidance);
                Console.WriteLine("");
                Console.WriteLine("🔧 Technical Details:");
                Console.WriteLine($"   Error Type: {ex.GetType().Name}");
                Console.WriteLine($"   Message: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Inner Error: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                }
                Console.WriteLine("");
                Console.WriteLine("🔍 Troubleshooting Steps:");
                Console.WriteLine("   1. Verify Nodetool server is running");
                Console.WriteLine("   2. Check workflow API endpoint accessibility");
                Console.WriteLine("   3. Verify workflow metadata service configuration");
                Console.WriteLine("   4. Check firewall/network settings");
                Console.WriteLine("   5. Verify Nodetool server health");
                Console.WriteLine("=================================================================");
                Console.WriteLine("");
            }
            
            _fetchedWorkflows = ImmutableList<WorkflowDetail>.Empty;
        }

        /// <summary>
        /// Get user guidance for workflow network-related errors
        /// </summary>
        private static string GetWorkflowNetworkErrorGuidance()
        {
            return @"1. Ensure Nodetool server is running and accessible
   2. Verify the workflow API endpoints are working
   3. Check your network connection and firewall settings
   4. Try accessing workflow API endpoints directly
   5. Confirm Nodetool server workflow service is healthy";
        }

        /// <summary>
        /// Get user guidance for workflow timeout errors
        /// </summary>
        private static string GetWorkflowTimeoutErrorGuidance()
        {
            return @"1. Check if Nodetool server workflow service is responding slowly
   2. Verify server resources (CPU, memory) are sufficient
   3. Check network latency to the server
   4. Verify workflow database/storage is accessible
   5. Restart Nodetool server if workflow service appears hung";
        }

        /// <summary>
        /// Generates a unique VL-compatible workflow name
        /// </summary>
        private static IReadOnlyDictionary<WorkflowDetail, string> BuildStableNodeNames(
            IEnumerable<WorkflowDetail> workflows)
        {
            var result = new Dictionary<WorkflowDetail, string>();
            var groups = workflows
                .Where(workflow => workflow != null)
                .GroupBy(GetBaseNodeName, StringComparer.Ordinal);

            foreach (var group in groups)
            {
                var items = group.ToArray();
                if (items.Length == 1)
                {
                    result[items[0]] = group.Key;
                    continue;
                }

                var idTokens = items.ToDictionary(
                    workflow => workflow,
                    workflow => GetStableIdToken(workflow.Id));
                if (idTokens.Values.Distinct(StringComparer.Ordinal).Count() != items.Length)
                {
                    throw new InvalidOperationException(
                        $"Duplicate workflow IDs found for node name '{group.Key}'.");
                }

                foreach (var workflow in items)
                {
                    var token = idTokens[workflow];
                    var prefixLength = Math.Min(8, token.Length);
                    while (prefixLength < token.Length && items.Any(other =>
                        !ReferenceEquals(other, workflow) &&
                        idTokens[other].StartsWith(token[..prefixLength], StringComparison.Ordinal)))
                    {
                        prefixLength++;
                    }

                    result[workflow] = $"{group.Key}_{token[..prefixLength]}";
                }
            }

            // Also handle the rare case where a generated duplicate name collides with
            // another workflow's literal display name. Full ID tokens keep this pass
            // deterministic and independent of discovery order.
            foreach (var collision in result
                .GroupBy(entry => entry.Value, StringComparer.Ordinal)
                .Where(group => group.Count() > 1))
            {
                foreach (var entry in collision)
                    result[entry.Key] = $"{GetBaseNodeName(entry.Key)}_{GetStableIdToken(entry.Key.Id)}";
            }

            if (result.Values.Distinct(StringComparer.Ordinal).Count() != result.Count)
                throw new InvalidOperationException("Workflow IDs and names do not produce unique VL node identities.");

            return result;
        }

        private static string GetBaseNodeName(WorkflowDetail workflow)
        {
            var baseName = SanitizeNodeName(
                !string.IsNullOrWhiteSpace(workflow.Name)
                    ? workflow.Name
                    : "UnknownWorkflow");
            return string.IsNullOrWhiteSpace(baseName) ? "UnknownWorkflow" : baseName;
        }

        private static string GetStableIdToken(string workflowId)
        {
            var token = new string((workflowId ?? string.Empty)
                .Where(char.IsLetterOrDigit)
                .Select(char.ToLowerInvariant)
                .ToArray());
            if (token.Length == 0)
                throw new InvalidOperationException("A workflow ID is required to disambiguate duplicate node names.");
            return token;
        }

        /// <summary>
        /// Sanitizes a node name to be VL-compatible
        /// </summary>
        private static string SanitizeNodeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            var chars = name.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '_')
                {
                    chars[i] = '_';
                }
            }

            var result = new string(chars);
            
            // Remove multiple consecutive underscores
            while (result.Contains("__"))
            {
                result = result.Replace("__", "_");
            }

            return result.Trim('_');
        }
        
        private static string BuildWorkflowRemarks(WorkflowDetail workflow)
        {
            var parts = new List<string>();
            parts.Add($"Nodetool Workflow ID: {workflow.Id}");
            parts.Add($"Name: {workflow.Name}");
            
            if (!string.IsNullOrWhiteSpace(workflow.Description) && 
                workflow.Description != workflow.Name)
                parts.Add($"Description: {workflow.Description}");
                
            var inputCount = workflow.GetInputProperties().Count();
            var outputCount = workflow.GetOutputProperties().Count();
            parts.Add($"📌 {inputCount} inputs, {outputCount} outputs");
            
            parts.Add($"Created: {workflow.CreatedAt:yyyy-MM-dd}");
            parts.Add($"Updated: {workflow.UpdatedAt:yyyy-MM-dd}");
            
            return string.Join("\n", parts);
        }
        
        private static string BuildWorkflowInputRemarks(dynamic property)
        {
            var parts = new List<string>();
            
            if (property.Type != null)
                parts.Add($"Type: {property.Type}");
                
            if (property.DefaultValue != null)
                parts.Add($"Default: {property.DefaultValue}");
                
            parts.Add("Workflow input");
            
            return string.Join(" | ", parts);
        }
        
        private static string BuildWorkflowOutputRemarks(dynamic property)
        {
            var parts = new List<string>();
            
            if (property.Type != null)
                parts.Add($"Type: {property.Type}");
                
            parts.Add("Workflow output");
            
            return string.Join(" | ", parts);
        }
        
        private static object GetDefaultValueForType(string? type)
        {
            return type?.ToLowerInvariant() switch
            {
                "string" or "str" => "",
                "int" or "integer" => 0,
                "float" or "number" => 0.0f,
                "bool" or "boolean" => false,
                _ => ""
            };
        }
    }
}

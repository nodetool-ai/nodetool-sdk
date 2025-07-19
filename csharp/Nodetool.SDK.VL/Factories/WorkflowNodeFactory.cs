using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using VL.Core;
using VL.Core.CompilerServices;
using Nodetool.SDK.VL.Models;
using Nodetool.SDK.VL.Services;
using Nodetool.SDK.VL.Nodes;
using Nodetool.SDK.Configuration;
using Microsoft.Extensions.Logging;

namespace Nodetool.SDK.VL.Factories
{
    /// <summary>
    /// Factory that creates VL node descriptions from Nodetool workflow metadata
    /// </summary>
    internal static class WorkflowNodeFactory
    {
        private static NodeBuilding.FactoryImpl? _factoryImpl = null;
        private static bool _isInitialized = false;
        private static readonly object _lock = new object();

        // Cached data from the Nodetool API
        private static ImmutableList<WorkflowDetail> _fetchedWorkflows = ImmutableList<WorkflowDetail>.Empty;
        private static string _apiStatusMessage = "API data not fetched.";
        private static string _processingSummary = "Workflow processing summary not yet available.";

        // Public getters for status/debugging
        public static string CurrentApiStatusMessage => _apiStatusMessage;
        public static string CurrentProcessingSummary => _processingSummary;

        /// <summary>
        /// Gets the VL node factory, initializing if necessary
        /// </summary>
        public static NodeBuilding.FactoryImpl GetFactory(IVLNodeDescriptionFactory vlSelfFactory)
        {
            Console.WriteLine("=== WorkflowNodeFactory.GetFactory called ===");
            Console.WriteLine($"WorkflowNodeFactory: vlSelfFactory type: {vlSelfFactory?.GetType().Name ?? "null"}");
            
            // Add safety check for vlSelfFactory
            if (vlSelfFactory == null)
            {
                Console.WriteLine("WorkflowNodeFactory: ERROR - vlSelfFactory is null!");
                return NodeBuilding.NewFactoryImpl(ImmutableArray<IVLNodeDescription>.Empty);
            }
            
            lock (_lock)
            {
                if (_isInitialized && _factoryImpl != null)
                {
                    Console.WriteLine("WorkflowNodeFactory: Returning cached factory instance");
                    return _factoryImpl;
                }

                Console.WriteLine("=== WorkflowNodeFactory: Performing one-time initialization ===");
                
                try
                {
                    PerformGlobalDataFetchAndStore();

                    var allDescriptions = new List<IVLNodeDescription>();
                    var usedNodeNames = new HashSet<string>();
                    var nameCounters = new Dictionary<string, int>();
                    int successfullyProcessedCount = 0;
                    int failedToProcessCount = 0;

                    Console.WriteLine($"WorkflowNodeFactory: Processing {_fetchedWorkflows.Count} fetched workflow definitions...");
                    
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
                            string vlNodeName = GenerateUniqueNodeName(workflow, usedNodeNames, nameCounters);
                            usedNodeNames.Add(vlNodeName);

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
                                
                                var inputCount = workflow.GetInputProperties().Count();
                                var outputCount = workflow.GetOutputProperties().Count();
                                Console.WriteLine($"✅ WorkflowNodeFactory: Created WorkflowNodeDescription '{vlNodeName}' from '{workflow.Name}' ({inputCount + 1} inputs, {outputCount + 2} outputs)");
                            }
                            catch (Exception ex)
                            {
                                failedToProcessCount++;
                                Console.WriteLine($"WorkflowNodeFactory: Error creating WorkflowNodeDescription for '{workflow.Name}': {ex.Message}");
                                Console.WriteLine($"WorkflowNodeFactory: Stack trace: {ex.StackTrace}");
                            }
                        }
                        catch (Exception ex)
                        {
                            failedToProcessCount++;
                            Console.WriteLine($"WorkflowNodeFactory: Error processing workflow '{workflow.Name}': {ex.Message}");
                        }
                    }

                    _processingSummary = $"Processed {successfullyProcessedCount} workflows successfully (Failed: {failedToProcessCount}) from {_fetchedWorkflows.Count} total definitions.";
                    Console.WriteLine($"WorkflowNodeFactory: {_processingSummary}");

                    // Add diagnostic status node using lambda-based factory approach
                    Console.WriteLine("WorkflowNodeFactory: Creating diagnostic status node...");
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
                            Console.WriteLine("WorkflowNodeFactory: Diagnostic status node created successfully");
                        }
                        else
                        {
                            Console.WriteLine("WorkflowNodeFactory: Failed to create diagnostic status node - vlSelfFactory returned null");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"WorkflowNodeFactory: Error creating status node: {ex.Message}");
                        Console.WriteLine($"WorkflowNodeFactory: Status node stack trace: {ex.StackTrace}");
                    }

                    Console.WriteLine($"WorkflowNodeFactory: Creating factory with {allDescriptions.Count} node descriptions...");
                    _factoryImpl = NodeBuilding.NewFactoryImpl(ImmutableArray.CreateRange(allDescriptions));
                    _isInitialized = true;
                    Console.WriteLine($"=== WorkflowNodeFactory: Factory created successfully with {allDescriptions.Count} node descriptions ===");
                    return _factoryImpl;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"=== WorkflowNodeFactory: CRITICAL ERROR during factory initialization ===");
                    Console.WriteLine($"WorkflowNodeFactory: Error message: {ex.Message}");
                    Console.WriteLine($"WorkflowNodeFactory: Error type: {ex.GetType().Name}");
                    Console.WriteLine($"WorkflowNodeFactory: Stack trace: {ex.StackTrace}");
                    
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"WorkflowNodeFactory: Inner exception: {ex.InnerException.Message}");
                        Console.WriteLine($"WorkflowNodeFactory: Inner stack trace: {ex.InnerException.StackTrace}");
                    }
                    
                    // Return empty factory to prevent VL from considering it "not found"
                    _factoryImpl = NodeBuilding.NewFactoryImpl(ImmutableArray<IVLNodeDescription>.Empty);
                    _isInitialized = true;
                    Console.WriteLine("WorkflowNodeFactory: Returning empty factory due to initialization error");
                    return _factoryImpl;
                }
            }
        }

        /// <summary>
        /// Fetches workflow metadata from the API and stores it
        /// </summary>
        private static void PerformGlobalDataFetchAndStore()
        {
            Console.WriteLine("=== WorkflowNodeFactory: Fetching workflow metadata from API ===");
            Console.WriteLine($"WorkflowNodeFactory: Target URL: {NodetoolConstants.Defaults.BaseUrl}{NodetoolConstants.Endpoints.Workflows}");
            
            try
            {
                var metadataService = new WorkflowMetadataService();
                Console.WriteLine("WorkflowNodeFactory: Created WorkflowMetadataService instance");
                
                // Since we can't use async in static constructor context, we need to handle this differently
                // For now, we'll use Task.Run to block synchronously - this isn't ideal but works for initialization
                Console.WriteLine("WorkflowNodeFactory: Starting async metadata fetch...");
                var task = Task.Run(async () => await metadataService.FetchWorkflowMetadataAsync());
                
                Console.WriteLine($"WorkflowNodeFactory: Waiting for API response ({NodetoolConstants.Defaults.TimeoutSeconds} second timeout)...");
                bool completed = task.Wait(TimeSpan.FromSeconds(NodetoolConstants.Defaults.TimeoutSeconds)); // Use constant for timeout
                
                if (!completed)
                {
                    _apiStatusMessage = $"Timeout waiting for API response after {NodetoolConstants.Defaults.TimeoutSeconds} seconds";
                    Console.WriteLine($"WorkflowNodeFactory: {_apiStatusMessage}");
                    _fetchedWorkflows = ImmutableList<WorkflowDetail>.Empty;
                    return;
                }
                
                var workflows = task.Result;
                _fetchedWorkflows = workflows?.ToImmutableList() ?? ImmutableList<WorkflowDetail>.Empty;
                _apiStatusMessage = metadataService.StatusMessage;
                
                Console.WriteLine($"=== WorkflowNodeFactory: Successfully fetched {_fetchedWorkflows.Count} workflow definitions ===");
                Console.WriteLine($"WorkflowNodeFactory: API Status: {_apiStatusMessage}");
            }
            catch (AggregateException aggEx)
            {
                var innerEx = aggEx.InnerException ?? aggEx;
                HandleWorkflowApiError(innerEx);
            }
            catch (Exception ex)
            {
                HandleWorkflowApiError(ex);
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
                    
                case TaskCanceledException when ex.Message.Contains("timeout"):
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
            Console.WriteLine($"");
            Console.WriteLine($"================= NODETOOL WORKFLOW API ERROR =================");
            Console.WriteLine($"🚨 WORKFLOW NODES CANNOT BE CREATED - API UNREACHABLE");
            Console.WriteLine($"");
            Console.WriteLine($"Error Category: {errorCategory}");
            Console.WriteLine($"Status: {_apiStatusMessage}");
            Console.WriteLine($"");
            Console.WriteLine($"📋 USER ACTION REQUIRED:");
            Console.WriteLine($"{userGuidance}");
            Console.WriteLine($"");
            Console.WriteLine($"🔧 Technical Details:");
            Console.WriteLine($"   Error Type: {ex.GetType().Name}");
            Console.WriteLine($"   Message: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   Inner Error: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
            Console.WriteLine($"");
            Console.WriteLine($"🔍 Troubleshooting Steps:");
            Console.WriteLine($"   1. Verify Nodetool server is running");
            Console.WriteLine($"   2. Check workflow API endpoint accessibility");
            Console.WriteLine($"   3. Verify workflow metadata service configuration");
            Console.WriteLine($"   4. Check firewall/network settings");
            Console.WriteLine($"   5. Verify Nodetool server health");
            Console.WriteLine($"=================================================================");
            Console.WriteLine($"");
            
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
        private static string GenerateUniqueNodeName(WorkflowDetail workflow, HashSet<string> usedNames, Dictionary<string, int> nameCounters)
        {
            // Use the workflow name as the base name
            string baseName = !string.IsNullOrWhiteSpace(workflow.Name) 
                ? workflow.Name 
                : "UnknownWorkflow";

            // Sanitize for VL compatibility
            baseName = SanitizeNodeName(baseName);

            if (string.IsNullOrWhiteSpace(baseName))
            {
                baseName = "UnknownWorkflow";
            }

            // Ensure uniqueness
            string finalName = baseName;
            if (usedNames.Contains(finalName))
            {
                int counter = nameCounters.TryGetValue(baseName, out int lastCounter) ? lastCounter + 1 : 1;
                do
                {
                    finalName = $"{baseName}_{counter:D2}";
                    counter++;
                } while (usedNames.Contains(finalName));
                
                nameCounters[baseName] = counter - 1;
            }

            return finalName;
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
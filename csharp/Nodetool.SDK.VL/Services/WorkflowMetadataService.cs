using Microsoft.Extensions.Logging;
using Nodetool.SDK.Api;
using Nodetool.SDK.Api.Models;
using Nodetool.SDK.VL.Models;
using Nodetool.SDK.Configuration;

namespace Nodetool.SDK.VL.Services;

/// <summary>
/// Service for fetching and caching workflow metadata from Nodetool API
/// </summary>
public class WorkflowMetadataService : IDisposable
{
    private readonly INodetoolClient _client;
    private readonly ILogger<WorkflowMetadataService>? _logger;
    
    // Cache management
    private List<WorkflowDetail>? _cachedWorkflows;
    private DateTime _lastFetch = DateTime.MinValue;
    private readonly TimeSpan CacheValidTime = TimeSpan.FromMinutes(NodetoolConstants.Defaults.CacheValidTimeMinutes);
    
    public string StatusMessage { get; private set; } = "Not initialized";

    public WorkflowMetadataService(ILogger<WorkflowMetadataService>? logger = null)
    {
        _logger = logger;
        _client = new NodetoolClient();
        
        // Configure with default base URL - can be overridden by calling Configure
        _client.Configure(NodetoolConstants.Defaults.BaseUrl);
        
        _logger?.LogDebug("WorkflowMetadataService initialized with base URL: {BaseUrl}", NodetoolConstants.Defaults.BaseUrl);
    }

    /// <summary>
    /// Configure the service with custom options
    /// </summary>
    public void Configure(NodetoolOptions options)
    {
        _client.Configure(options.BaseUrl, options.ApiKey);
        _logger?.LogDebug("WorkflowMetadataService configured with: {BaseUrl}", options.BaseUrl);
    }

    /// <summary>
    /// Fetch workflow metadata from the API with caching
    /// </summary>
    public async Task<List<WorkflowDetail>> FetchWorkflowMetadataAsync()
    {
        // Check cache first
        if (_cachedWorkflows != null && DateTime.Now - _lastFetch < CacheValidTime)
        {
            _logger?.LogDebug("Using cached workflow metadata ({Count} workflows)", _cachedWorkflows.Count);
            StatusMessage = $"Using cached metadata ({_cachedWorkflows.Count} workflows)";
            return _cachedWorkflows;
        }

        StatusMessage = "Fetching workflow metadata...";
        _logger?.LogInformation("Fetching workflow metadata from API");

        try
        {
            // Fetch workflow list
            var workflows = await _client.GetWorkflowsAsync();
            _logger?.LogDebug("Retrieved {Count} workflows from API", workflows.Count);

            // Convert API responses to our detailed models
            var workflowDetails = new List<WorkflowDetail>();
            
            foreach (var workflow in workflows)
            {
                try
                {
                    // Get detailed workflow information
                    var detailedWorkflow = await _client.GetWorkflowAsync(workflow.Id);
                    
                    // Convert to our model
                    var workflowDetail = new WorkflowDetail
                    {
                        Id = detailedWorkflow.Id,
                        Name = detailedWorkflow.Name,
                        Description = detailedWorkflow.Description,
                        CreatedAt = detailedWorkflow.CreatedAt,
                        UpdatedAt = detailedWorkflow.UpdatedAt,
                        InputSchema = ConvertToWorkflowSchema(detailedWorkflow.InputSchema),
                        OutputSchema = ConvertToWorkflowSchema(detailedWorkflow.OutputSchema)
                    };

                    workflowDetails.Add(workflowDetail);
                    _logger?.LogDebug("Processed workflow: {Name} ({Id})", workflowDetail.Name, workflowDetail.Id);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to fetch details for workflow {Id}: {Name}", workflow.Id, workflow.Name);
                    // Continue with other workflows
                }
            }

            // Update cache
            _cachedWorkflows = workflowDetails;
            _lastFetch = DateTime.Now;
            
            StatusMessage = $"Successfully fetched {workflowDetails.Count} workflow definitions";
            _logger?.LogInformation("Successfully fetched {Count} workflow definitions", workflowDetails.Count);

            return workflowDetails;
        }
        catch (Exception ex)
        {
            var errorMessage = $"Failed to fetch workflow metadata: {ex.Message}";
            StatusMessage = errorMessage;
            _logger?.LogError(ex, "Error fetching workflow metadata");
            throw new InvalidOperationException(errorMessage, ex);
        }
    }

    /// <summary>
    /// Get a specific workflow by ID
    /// </summary>
    public async Task<WorkflowDetail?> GetWorkflowByIdAsync(string workflowId)
    {
        try
        {
            // Check cache first
            if (_cachedWorkflows != null)
            {
                var cached = _cachedWorkflows.FirstOrDefault(w => w.Id == workflowId);
                if (cached != null)
                {
                    _logger?.LogDebug("Found workflow {Id} in cache", workflowId);
                    return cached;
                }
            }

            // Fetch from API
            _logger?.LogDebug("Fetching workflow {Id} from API", workflowId);
            var workflow = await _client.GetWorkflowAsync(workflowId);

            var workflowDetail = new WorkflowDetail
            {
                Id = workflow.Id,
                Name = workflow.Name,
                Description = workflow.Description,
                CreatedAt = workflow.CreatedAt,
                UpdatedAt = workflow.UpdatedAt,
                InputSchema = ConvertToWorkflowSchema(workflow.InputSchema),
                OutputSchema = ConvertToWorkflowSchema(workflow.OutputSchema)
            };

            return workflowDetail;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching workflow {Id}", workflowId);
            return null;
        }
    }

    /// <summary>
    /// Execute a workflow with the provided parameters
    /// </summary>
    public async Task<Dictionary<string, object>> ExecuteWorkflowAsync(string workflowId, Dictionary<string, object> parameters)
    {
        _logger?.LogInformation("Executing workflow {WorkflowId} with {ParamCount} parameters", workflowId, parameters.Count);
        
        try
        {
            var result = await _client.ExecuteWorkflowAsync(workflowId, parameters);
            _logger?.LogInformation("Workflow {WorkflowId} executed successfully with {OutputCount} outputs", workflowId, result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error executing workflow {WorkflowId}", workflowId);
            throw;
        }
    }

    /// <summary>
    /// Clear the workflow cache
    /// </summary>
    public void ClearCache()
    {
        _cachedWorkflows = null;
        _lastFetch = DateTime.MinValue;
    }

    private WorkflowSchemaDefinition? ConvertToWorkflowSchema(Nodetool.SDK.Api.Models.SchemaDefinition? apiSchema)
    {
        if (apiSchema?.Properties == null || apiSchema.Properties.Count == 0)
            return null;

        var schema = new WorkflowSchemaDefinition
        {
            Type = apiSchema.Type ?? "object",
            Properties = new Dictionary<string, WorkflowPropertyDefinition>(),
            Required = apiSchema.Required ?? new List<string>(),
            Title = apiSchema.Title,
            Description = apiSchema.Description
        };

        foreach (var prop in apiSchema.Properties)
        {
            if (string.IsNullOrEmpty(prop.Key) || prop.Value == null)
                continue;

            var propertyDef = new WorkflowPropertyDefinition
            {
                Type = prop.Value.Type,
                Title = prop.Value.Title,
                Description = prop.Value.Description,
                Default = prop.Value.Default,
                Minimum = prop.Value.Minimum,
                Maximum = prop.Value.Maximum,
                Format = prop.Value.Format,
                Enum = prop.Value.Enum,
                Const = prop.Value.Const
            };

            // Handle nested properties for object types
            if (prop.Value.Properties != null && prop.Value.Properties.Count > 0)
            {
                propertyDef.Properties = new Dictionary<string, WorkflowPropertyDefinition>();
                foreach (var nestedProp in prop.Value.Properties)
                {
                    if (string.IsNullOrEmpty(nestedProp.Key) || nestedProp.Value == null)
                        continue;

                    var nestedPropertyDef = new WorkflowPropertyDefinition
                    {
                        Type = nestedProp.Value.Type,
                        Title = nestedProp.Value.Title,
                        Description = nestedProp.Value.Description,
                        Default = nestedProp.Value.Default
                    };
                    propertyDef.Properties[nestedProp.Key] = nestedPropertyDef;
                }
            }

            // Handle array items
            if (prop.Value.Items != null)
            {
                propertyDef.Items = new WorkflowPropertyDefinition
                {
                    Type = prop.Value.Items.Type,
                    Title = prop.Value.Items.Title,
                    Description = prop.Value.Items.Description,
                    Default = prop.Value.Items.Default
                };
            }

            schema.Properties[prop.Key] = propertyDef;
        }

        return schema;
    }

    public void Dispose()
    {
        _client?.Dispose();
        GC.SuppressFinalize(this);
    }
} 
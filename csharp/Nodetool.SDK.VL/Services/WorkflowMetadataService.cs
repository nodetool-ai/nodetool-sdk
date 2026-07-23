using Microsoft.Extensions.Logging;
using System.Text.Json;
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
    public async Task<List<WorkflowDetail>> FetchWorkflowMetadataAsync(
        CancellationToken cancellationToken = default)
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
            var workflows = await _client.GetWorkflowSummariesAsync(cancellationToken);
            _logger?.LogDebug("Retrieved {Count} compact workflow summaries from API", workflows.Count);

            var workflowDetails = new List<WorkflowDetail>();
            var interfacesById = new Dictionary<string, WorkflowInterfaceResponse>(
                StringComparer.Ordinal);
            var skippedInterfaceCount = 0;
            foreach (var batch in workflows.Chunk(100))
            {
                var result = await _client.GetWorkflowInterfacesAsync(
                    batch.Select(workflow => workflow.Id).ToArray(),
                    cancellationToken);
                foreach (var workflowInterface in result.Interfaces)
                {
                    var errors = workflowInterface.Diagnostics
                        .Where(diagnostic => string.Equals(
                            diagnostic.Severity,
                            "error",
                            StringComparison.OrdinalIgnoreCase))
                        .ToArray();
                    if (errors.Length > 0)
                    {
                        skippedInterfaceCount++;
                        foreach (var error in errors)
                        {
                            _logger?.LogWarning(
                                "Workflow interface {Id} was skipped ({Code}, pin {Pin}): {Message}",
                                workflowInterface.WorkflowId,
                                error.Code,
                                error.PinName ?? "n/a",
                                error.Message);
                        }
                        continue;
                    }
                    interfacesById[workflowInterface.WorkflowId] = workflowInterface;
                }
                foreach (var error in result.Errors)
                {
                    skippedInterfaceCount++;
                    _logger?.LogWarning(
                        "Workflow interface {Id} was skipped ({Code}): {Message}",
                        error.WorkflowId,
                        error.Code,
                        error.Message);
                }
            }

            foreach (var workflow in workflows)
            {
                if (!interfacesById.TryGetValue(workflow.Id, out var workflowInterface))
                    continue;
                var workflowDetail = CreateWorkflowDetail(workflow, workflowInterface);
                workflowDetails.Add(workflowDetail);
                _logger?.LogDebug(
                    "Processed workflow: {Name} ({Id})",
                    workflowDetail.Name,
                    workflowDetail.Id);
            }

            // Update cache
            _cachedWorkflows = workflowDetails;
            _lastFetch = DateTime.Now;
            
            StatusMessage = skippedInterfaceCount == 0
                ? $"Successfully fetched {workflowDetails.Count} workflow definitions"
                : $"Fetched {workflowDetails.Count} workflow definitions; skipped {skippedInterfaceCount} invalid interfaces";
            _logger?.LogInformation(
                "Fetched {Count} workflow definitions; skipped {SkippedCount} invalid interfaces",
                workflowDetails.Count,
                skippedInterfaceCount);

            return workflowDetails;
        }
        catch (WorkflowInterfaceUnavailableException)
        {
            throw;
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

            var summary = (await _client.GetWorkflowSummariesAsync())
                .FirstOrDefault(workflow => workflow.Id == workflowId);
            if (summary == null)
                return null;
            var workflowInterface = await _client.GetWorkflowInterfaceAsync(workflowId);
            return CreateWorkflowDetail(summary, workflowInterface);
        }
        catch (WorkflowInterfaceUnavailableException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching workflow {Id}", workflowId);
            return null;
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

    private static WorkflowDetail CreateWorkflowDetail(
        WorkflowSummaryResponse summary,
        WorkflowInterfaceResponse workflowInterface)
    {
        var updatedAt = DateTime.TryParse(summary.Revision, out var parsedRevision)
            ? parsedRevision
            : DateTime.MinValue;
        return new WorkflowDetail
        {
            Id = summary.Id,
            Name = summary.Name,
            Description = summary.Description,
            UpdatedAt = updatedAt,
            Interface = workflowInterface,
            InputSchema = CreateInterfaceSchema(workflowInterface.Inputs),
            OutputSchema = CreateInterfaceSchema(workflowInterface.Outputs)
        };
    }

    private static WorkflowSchemaDefinition CreateInterfaceSchema(
        IEnumerable<WorkflowInterfacePin> pins)
    {
        var schema = new WorkflowSchemaDefinition();
        foreach (var pin in pins)
        {
            schema.Properties[pin.Name] = ConvertInterfaceType(
                pin.Type,
                pin.Description,
                pin is WorkflowInterfaceInput input &&
                input.Default.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined)
                    ? input.Default
                    : null);
            if (pin is WorkflowInterfaceInput { Required: true })
                schema.Required.Add(pin.Name);
        }
        return schema;
    }

    private static WorkflowPropertyDefinition ConvertInterfaceType(
        NodeTypeDefinition type,
        string description,
        object? defaultValue)
    {
        var property = new WorkflowPropertyDefinition
        {
            Type = type.Type,
            Title = type.TypeName,
            Description = description,
            Default = defaultValue,
            Enum = type.Values
        };
        if (string.Equals(type.Type, "list", StringComparison.OrdinalIgnoreCase))
        {
            property.Type = "array";
            property.Items = type.TypeArgs?.Count > 0
                ? ConvertInterfaceType(type.TypeArgs[0], "", null)
                : new WorkflowPropertyDefinition { Type = "any" };
        }
        return property;
    }

    private WorkflowSchemaDefinition? ConvertToWorkflowSchema(Nodetool.SDK.Api.Models.SchemaDefinition? apiSchema)
    {
        if (apiSchema == null)
            return null;

        var schema = new WorkflowSchemaDefinition
        {
            Type = apiSchema.Type ?? "object",
            Properties = new Dictionary<string, WorkflowPropertyDefinition>(StringComparer.Ordinal),
            Required = apiSchema.Required ?? new List<string>(),
            Title = apiSchema.Title,
            Description = apiSchema.Description,
            Ref = apiSchema.Ref,
            Definitions = apiSchema.Definitions != null && apiSchema.Definitions.Count > 0
                ? apiSchema.Definitions.ToDictionary(kvp => kvp.Key, kvp => ConvertProperty(kvp.Value), StringComparer.Ordinal)
                : null,
            Defs = apiSchema.DollarDefs != null && apiSchema.DollarDefs.Count > 0
                ? apiSchema.DollarDefs.ToDictionary(kvp => kvp.Key, kvp => ConvertProperty(kvp.Value), StringComparer.Ordinal)
                : null,
            AnyOf = apiSchema.AnyOf?.Select(ConvertProperty).ToList(),
            OneOf = apiSchema.OneOf?.Select(ConvertProperty).ToList(),
            AllOf = apiSchema.AllOf?.Select(ConvertProperty).ToList(),
        };

        // Preserve direct properties if present
        if (apiSchema.Properties != null)
        {
            foreach (var prop in apiSchema.Properties)
            {
                if (string.IsNullOrEmpty(prop.Key) || prop.Value == null)
                    continue;
                schema.Properties[prop.Key] = ConvertProperty(prop.Value);
            }
        }

        return schema;
    }

    private static WorkflowPropertyDefinition ConvertProperty(Nodetool.SDK.Api.Models.PropertyDefinition apiProp)
    {
        var def = new WorkflowPropertyDefinition
        {
            Type = apiProp.Type,
            Title = apiProp.Title,
            Description = apiProp.Description,
            Default = apiProp.Default,
            Minimum = apiProp.Minimum,
            Maximum = apiProp.Maximum,
            Format = apiProp.Format,
            Enum = apiProp.Enum,
            Const = apiProp.Const,
            Required = apiProp.Required,
            Ref = apiProp.Ref,
            AnyOf = apiProp.AnyOf?.Select(ConvertProperty).ToList(),
            OneOf = apiProp.OneOf?.Select(ConvertProperty).ToList(),
            AllOf = apiProp.AllOf?.Select(ConvertProperty).ToList(),
        };

        if (apiProp.Properties != null && apiProp.Properties.Count > 0)
        {
            def.Properties = new Dictionary<string, WorkflowPropertyDefinition>();
            foreach (var nested in apiProp.Properties)
            {
                if (string.IsNullOrEmpty(nested.Key) || nested.Value == null)
                    continue;
                def.Properties[nested.Key] = ConvertProperty(nested.Value);
            }
        }

        if (apiProp.Items != null)
        {
            def.Items = ConvertProperty(apiProp.Items);
        }

        return def;
    }

    public void Dispose()
    {
        _client?.Dispose();
        GC.SuppressFinalize(this);
    }
}

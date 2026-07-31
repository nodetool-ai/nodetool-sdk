using Nodetool.SDK.Assets;
using Nodetool.SDK.Configuration;
using Nodetool.SDK.Connection;
using Nodetool.SDK.Execution;

namespace Nodetool.SDK.VL.Services;

/// <summary>
/// Mutable Connect-node settings scoped to one vvvv AppHost.
/// Network clients and catalogs remain separate host-owned services.
/// </summary>
internal sealed class VlNodeToolHostSettings
{
    private readonly object _gate = new();
    private string _workerUrl = "ws://localhost:7777";
    private string? _apiKey;
    private Uri _apiBaseUrl =
        new(NodetoolConstants.Defaults.BaseUrl);
    private int _executionTimeoutSeconds =
        NodeToolClientProvider.DefaultExecutionTimeoutSeconds;
    private int _inlineMediaLimitBytes =
        MediaInputPreparer.DefaultInlineLimitBytes;
    private bool _autoReconnect = true;
    private bool _useWebSocketDiscovery;
    private bool _loadNodes = true;
    private bool _showAllNodes;
    private bool _loadWorkflows = true;
    private WorkflowExecutionOptions _executionOptions = new();
    private string? _configurationError;

    public string WorkerUrl
    {
        get
        {
            lock (_gate)
                return _workerUrl;
        }
    }

    public string? ApiKey
    {
        get
        {
            lock (_gate)
                return _apiKey;
        }
    }

    public Uri ApiBaseUrl
    {
        get
        {
            lock (_gate)
                return _apiBaseUrl;
        }
    }

    public int ExecutionTimeoutSeconds
    {
        get
        {
            lock (_gate)
                return _executionTimeoutSeconds;
        }
    }

    public int InlineMediaLimitBytes
    {
        get
        {
            lock (_gate)
                return _inlineMediaLimitBytes;
        }
    }

    public bool AutoReconnect
    {
        get
        {
            lock (_gate)
                return _autoReconnect;
        }
    }

    public bool UseWebSocketDiscovery
    {
        get
        {
            lock (_gate)
                return _useWebSocketDiscovery;
        }
    }

    public bool LoadNodes
    {
        get
        {
            lock (_gate)
                return _loadNodes;
        }
    }

    public bool LoadWorkflows
    {
        get
        {
            lock (_gate)
                return _loadWorkflows;
        }
    }

    public bool ShowAllNodes
    {
        get
        {
            lock (_gate)
                return _showAllNodes;
        }
    }

    public WorkflowExecutionOptions ExecutionOptions
    {
        get
        {
            lock (_gate)
                return _executionOptions;
        }
    }

    public string? ConfigurationError
    {
        get
        {
            lock (_gate)
                return _configurationError;
        }
    }

    public bool Configure(string workerUrl, string? apiKey)
    {
        var workerUri = new Uri(workerUrl);
        var apiBaseUrl =
            NodeToolEndpointResolver.DeriveApiBaseUrl(workerUri);
        _ = NodeToolEndpointResolver.DeriveWebSocketUrl(workerUri);

        lock (_gate)
        {
            var changed =
                !string.Equals(
                    workerUrl,
                    _workerUrl,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    apiKey,
                    _apiKey,
                    StringComparison.Ordinal);
            if (!changed)
            {
                _configurationError = null;
                return false;
            }

            _workerUrl = workerUrl;
            _apiKey = apiKey;
            _apiBaseUrl = apiBaseUrl;
            _configurationError = null;
            return true;
        }
    }

    public void SetConfigurationError(string error)
    {
        lock (_gate)
            _configurationError = error;
    }

    public bool SetAutoReconnect(bool enabled)
    {
        lock (_gate)
        {
            if (_autoReconnect == enabled)
                return false;
            _autoReconnect = enabled;
            return true;
        }
    }

    public bool SetUseWebSocketDiscovery(bool enabled)
    {
        lock (_gate)
        {
            if (_useWebSocketDiscovery == enabled)
                return false;
            _useWebSocketDiscovery = enabled;
            return true;
        }
    }

    public bool SetLoadNodes(bool enabled)
    {
        lock (_gate)
        {
            if (_loadNodes == enabled)
                return false;
            _loadNodes = enabled;
            return true;
        }
    }

    public bool SetLoadWorkflows(bool enabled)
    {
        lock (_gate)
        {
            if (_loadWorkflows == enabled)
                return false;
            _loadWorkflows = enabled;
            return true;
        }
    }

    public bool SetShowAllNodes(bool enabled)
    {
        lock (_gate)
        {
            if (_showAllNodes == enabled)
                return false;
            _showAllNodes = enabled;
            return true;
        }
    }

    public void SetWorkflowPersistence(WorkflowPersistence value)
    {
        lock (_gate)
            _executionOptions = _executionOptions with
            {
                Persistence = value
            };
    }

    public void SetWorkflowEventDetail(WorkflowEventDetail value)
    {
        lock (_gate)
            _executionOptions = _executionOptions with
            {
                EventDetail = value
            };
    }

    public void SetWorkflowAssetPersistence(
        WorkflowAssetPersistence value)
    {
        lock (_gate)
            _executionOptions = _executionOptions with
            {
                AssetPersistence = value
            };
    }

    public void SetExecutionTimeoutSeconds(int timeoutSeconds)
    {
        lock (_gate)
        {
            _executionTimeoutSeconds = Math.Clamp(
                timeoutSeconds,
                1,
                NodeToolClientProvider.MaximumExecutionTimeoutSeconds);
        }
    }

    public void SetInlineMediaLimitBytes(int limitBytes)
    {
        lock (_gate)
        {
            _inlineMediaLimitBytes = Math.Clamp(
                limitBytes,
                0,
                NodeToolClientProvider.MaximumInlineMediaLimitBytes);
        }
    }

    public NodeToolConnectionProfile CreateProfile()
    {
        lock (_gate)
        {
            var workerUri = new Uri(_workerUrl);
            return new NodeToolConnectionProfile
            {
                ServerUrl = workerUri,
                ApiBaseUrl = _apiBaseUrl,
                WorkerWebSocketUrl =
                    NodeToolEndpointResolver.DeriveWebSocketUrl(workerUri),
                TokenProvider =
                    new StaticNodeToolTokenProvider(_apiKey),
                AutoReconnect = _autoReconnect
            };
        }
    }
}

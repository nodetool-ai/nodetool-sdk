using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nodetool.SDK.Api;
using Nodetool.SDK.Api.Models;
using Nodetool.SDK.Diagnostics;
using Nodetool.SDK.Execution;

namespace Nodetool.SDK.Connection;

/// <summary>
/// Owns the replaceable connection manager for one host connection slot.
/// Host adapters can change profiles without implementing client disposal,
/// stale-connect rejection, or connection status projection themselves.
/// </summary>
public sealed class NodeToolConnectionSession :
    INodeToolExecutionConnection,
    IAsyncDisposable,
    IDisposable
{
    private readonly object _gate = new();
    private readonly HttpClient? _httpClient;
    private readonly ILoggerFactory _loggerFactory;
    private readonly Func<
        NodeToolConnectionProfile,
        HttpClient?,
        ILoggerFactory,
        NodeToolConnectionManager> _managerFactory;
    private NodeToolConnectionProfile _profile;
    private NodeToolConnectionManager? _manager;
    private INodeToolExecutionClient? _client;
    private long _generation;
    private bool _disposed;

    public NodeToolConnectionProfile Profile
    {
        get
        {
            lock (_gate)
                return _profile;
        }
    }

    public Uri? ApiBaseUrl => Profile.ResolveApiBaseUrl();
    public string? AuthToken
    {
        get
        {
            lock (_gate)
                return _manager?.AuthToken;
        }
    }

    public INodeToolExecutionClient? CurrentClient
    {
        get
        {
            lock (_gate)
                return _client;
        }
    }

    public bool IsConnected => CurrentClient?.IsConnected == true;
    public bool IsDisposed
    {
        get
        {
            lock (_gate)
                return _disposed;
        }
    }
    public string Status { get; private set; } = "disconnected";
    public string? LastError { get; private set; }

    public event Action<string>? StatusChanged;
    public event Action<NodeToolConnectionProfile>? ProfileChanged;

    public NodeToolConnectionSession(
        NodeToolConnectionProfile profile,
        HttpClient? httpClient = null,
        ILoggerFactory? loggerFactory = null)
        : this(
            profile,
            httpClient,
            loggerFactory,
            static (value, client, factory) =>
                new NodeToolConnectionManager(value, client, factory))
    {
    }

    internal NodeToolConnectionSession(
        NodeToolConnectionProfile profile,
        HttpClient? httpClient,
        ILoggerFactory? loggerFactory,
        Func<
            NodeToolConnectionProfile,
            HttpClient?,
            ILoggerFactory,
            NodeToolConnectionManager> managerFactory)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(managerFactory);
        ValidateProfile(profile);
        _profile = profile;
        _httpClient = httpClient;
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _managerFactory = managerFactory;
    }

    /// <summary>
    /// Replaces the active profile and deterministically disposes the old
    /// clients. Returns false when the profile is unchanged.
    /// </summary>
    public bool Configure(NodeToolConnectionProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ValidateProfile(profile);

        NodeToolConnectionManager? previousManager;
        INodeToolExecutionClient? previousClient;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (Equals(_profile, profile))
                return false;

            previousManager = _manager;
            previousClient = _client;
            _profile = profile;
            _manager = null;
            _client = null;
            _generation++;
            Status = "disconnected";
            LastError = null;
        }

        Unsubscribe(previousClient);
        previousManager?.Dispose();
        ProfileChanged?.Invoke(profile);
        StatusChanged?.Invoke(Status);
        return true;
    }

    /// <summary>
    /// Updates reconnect policy without tearing down an active connection.
    /// The updated profile is used when the manager is next recreated.
    /// </summary>
    public bool SetAutoReconnect(bool enabled)
    {
        NodeToolConnectionProfile profile;
        INodeToolExecutionClient? client;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_profile.AutoReconnect == enabled)
                return false;
            _profile = _profile with { AutoReconnect = enabled };
            profile = _profile;
            client = _client;
        }

        if (client is NodeToolExecutionClient concreteClient)
            concreteClient.AutoReconnectEnabled = enabled;
        ProfileChanged?.Invoke(profile);
        return true;
    }

    public async Task<INodetoolClient> GetApiClientAsync(
        CancellationToken cancellationToken = default)
    {
        var (manager, generation) = GetOrCreateManager();
        var client = await manager.GetApiClientAsync(cancellationToken)
            .ConfigureAwait(false);
        ThrowIfStale(manager, generation);
        return client;
    }

    public async Task<SdkCapabilitiesResponse> GetSdkCapabilitiesAsync(
        CancellationToken cancellationToken = default)
    {
        var (manager, generation) = GetOrCreateManager();
        var capabilities = await manager
            .GetSdkCapabilitiesAsync(cancellationToken)
            .ConfigureAwait(false);
        ThrowIfStale(manager, generation);
        return capabilities;
    }

    public async Task<INodeToolExecutionClient> GetConnectedClientAsync(
        CancellationToken cancellationToken = default)
    {
        var (manager, generation) = GetOrCreateManager();
        SetStatus("connecting", null);
        try
        {
            var client = await manager.GetConnectedClientAsync(cancellationToken)
                .ConfigureAwait(false);
            ThrowIfStale(manager, generation);
            lock (_gate)
            {
                if (!ReferenceEquals(_client, client))
                {
                    Unsubscribe(_client);
                    _client = client;
                    _client.ConnectionStatusChanged += OnClientStatusChanged;
                }
            }
            SetStatus("connected", null);
            return client;
        }
        catch (Exception ex)
        {
            if (IsCurrent(manager, generation))
            {
                SetStatus(
                    "error",
                    NodeToolDiagnosticRedactor.RedactText(
                        ex.Message,
                        manager.AuthToken));
            }
            throw;
        }
    }

    public async Task<bool> ConnectAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            await GetConnectedClientAsync(cancellationToken)
                .ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException) when (
            cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        NodeToolConnectionManager? manager;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            manager = _manager;
        }

        if (manager != null)
            await manager.DisconnectAsync().ConfigureAwait(false);
        SetStatus("disconnected", null);
    }

    public async Task<bool> ReconnectAsync(
        CancellationToken cancellationToken = default)
    {
        await DisconnectAsync().ConfigureAwait(false);
        return await ConnectAsync(cancellationToken).ConfigureAwait(false);
    }

    public void Reset()
    {
        NodeToolConnectionManager? manager;
        INodeToolExecutionClient? client;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            manager = _manager;
            client = _client;
            _manager = null;
            _client = null;
            _generation++;
            Status = "disconnected";
            LastError = null;
        }

        Unsubscribe(client);
        manager?.Dispose();
        StatusChanged?.Invoke(Status);
    }

    private (NodeToolConnectionManager Manager, long Generation)
        GetOrCreateManager()
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _manager ??= _managerFactory(
                _profile,
                _httpClient,
                _loggerFactory);
            return (_manager, _generation);
        }
    }

    private bool IsCurrent(
        NodeToolConnectionManager manager,
        long generation)
    {
        lock (_gate)
        {
            return !_disposed &&
                   generation == _generation &&
                   ReferenceEquals(manager, _manager);
        }
    }

    private void ThrowIfStale(
        NodeToolConnectionManager manager,
        long generation)
    {
        if (!IsCurrent(manager, generation))
        {
            throw new OperationCanceledException(
                "The NodeTool connection profile changed while connecting.");
        }
    }

    private void OnClientStatusChanged(string status)
    {
        var error = string.Equals(status, "error", StringComparison.Ordinal)
            ? CurrentClient?.LastError
            : null;
        SetStatus(status, error);
    }

    private void SetStatus(string status, string? error)
    {
        lock (_gate)
        {
            if (_disposed)
                return;
            Status = status;
            LastError = NodeToolDiagnosticRedactor.RedactText(
                error ?? "",
                AuthToken);
        }
        StatusChanged?.Invoke(status);
    }

    private static void ValidateProfile(NodeToolConnectionProfile profile)
    {
        _ = profile.ResolveApiBaseUrl();
        _ = profile.ResolveWorkerWebSocketUrl();
    }

    private void Unsubscribe(INodeToolExecutionClient? client)
    {
        if (client != null)
            client.ConnectionStatusChanged -= OnClientStatusChanged;
    }

    public void Dispose()
    {
        NodeToolConnectionManager? manager;
        INodeToolExecutionClient? client;
        lock (_gate)
        {
            if (_disposed)
                return;
            _disposed = true;
            manager = _manager;
            client = _client;
            _manager = null;
            _client = null;
            _generation++;
        }
        Unsubscribe(client);
        manager?.Dispose();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        NodeToolConnectionManager? manager;
        INodeToolExecutionClient? client;
        lock (_gate)
        {
            if (_disposed)
                return;
            _disposed = true;
            manager = _manager;
            client = _client;
            _manager = null;
            _client = null;
            _generation++;
        }
        Unsubscribe(client);
        if (manager != null)
            await manager.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
}

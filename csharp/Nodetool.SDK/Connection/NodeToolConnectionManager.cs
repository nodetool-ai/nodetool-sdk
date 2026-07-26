using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nodetool.SDK.Api;
using Nodetool.SDK.Api.Models;
using Nodetool.SDK.Execution;

namespace Nodetool.SDK.Connection;

/// <summary>
/// Owns the HTTP and WebSocket clients for one immutable connection profile.
/// Hosts own the manager and dispose it when their connection scope ends.
/// </summary>
public sealed class NodeToolConnectionManager :
    INodeToolExecutionConnection,
    IAsyncDisposable,
    IDisposable
{
    private readonly NodeToolConnectionProfile _profile;
    private readonly HttpClient? _httpClient;
    private readonly ILoggerFactory _loggerFactory;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private readonly SemaphoreSlim _capabilitiesLock = new(1, 1);
    private NodetoolClient? _apiClient;
    private NodeToolExecutionClient? _executionClient;
    private SdkCapabilitiesResponse? _capabilities;
    private long _connectionGeneration;
    private long _capabilitiesGeneration = -1;
    private string? _resolvedToken;
    private bool _disposed;

    public NodeToolConnectionProfile Profile => _profile;
    public Uri ApiBaseUrl => _profile.ResolveApiBaseUrl();
    public string? AuthToken => _resolvedToken;

    public NodeToolConnectionManager(
        NodeToolConnectionProfile profile,
        HttpClient? httpClient = null,
        ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(profile);
        // Resolve both endpoints eagerly so invalid profiles fail before any
        // client or network resource is created.
        _ = profile.ResolveApiBaseUrl();
        _ = profile.ResolveWorkerWebSocketUrl();
        _profile = profile;
        _httpClient = httpClient;
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
    }

    public async Task<INodetoolClient> GetApiClientAsync(
        CancellationToken cancellationToken = default)
    {
        var wasInitialized = _apiClient != null;
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        if (wasInitialized)
            await RefreshHttpTokenAsync(cancellationToken).ConfigureAwait(false);
        return _apiClient!;
    }

    public async Task<SdkCapabilitiesResponse> GetSdkCapabilitiesAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (TryGetCachedCapabilities(out var cached))
            return cached;

        await _capabilitiesLock.WaitAsync(cancellationToken)
            .ConfigureAwait(false);
        try
        {
            while (true)
            {
                if (TryGetCachedCapabilities(out var resolved))
                    return resolved;

                var generation = Volatile.Read(ref _connectionGeneration);
                var client = await GetApiClientAsync(cancellationToken)
                    .ConfigureAwait(false);
                var capabilities = await client
                    .GetSdkCapabilitiesAsync(cancellationToken)
                    .ConfigureAwait(false);
                if (generation != Volatile.Read(ref _connectionGeneration))
                    continue;

                Volatile.Write(ref _capabilities, capabilities);
                Volatile.Write(ref _capabilitiesGeneration, generation);
                return capabilities;
            }
        }
        finally
        {
            _capabilitiesLock.Release();
        }
    }

    public async Task<INodeToolExecutionClient> GetConnectedClientAsync(
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        if (!_executionClient!.IsConnected &&
            !await _executionClient.ConnectAsync(cancellationToken)
                .ConfigureAwait(false))
        {
            throw new InvalidOperationException(
                _executionClient.LastError ??
                "Failed to connect to NodeTool server.");
        }
        return _executionClient;
    }

    public async Task DisconnectAsync()
    {
        if (_executionClient != null)
            await _executionClient.DisconnectAsync().ConfigureAwait(false);
        InvalidateCapabilities();
    }

    private async Task EnsureInitializedAsync(
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_apiClient != null)
            return;

        await _initializationLock.WaitAsync(cancellationToken)
            .ConfigureAwait(false);
        try
        {
            if (_apiClient != null)
                return;

            _resolvedToken = NormalizeToken(
                _profile.TokenProvider == null
                    ? null
                    : await _profile.TokenProvider
                        .GetTokenAsync(cancellationToken)
                        .ConfigureAwait(false));
            _apiClient = new NodetoolClient(
                _profile.ResolveApiBaseUrl(),
                _resolvedToken,
                _httpClient,
                _loggerFactory.CreateLogger<NodetoolClient>(),
                _profile.ReadRetryPolicy);
            _executionClient = new NodeToolExecutionClient(
                _profile.ToClientOptions(),
                _resolvedToken,
                _loggerFactory.CreateLogger<NodeToolExecutionClient>());
            _executionClient.ConnectionStatusChanged +=
                OnExecutionConnectionStatusChanged;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    private static string? NormalizeToken(string? token)
        => string.IsNullOrWhiteSpace(token) ? null : token.Trim();

    private async Task RefreshHttpTokenAsync(
        CancellationToken cancellationToken)
    {
        if (_profile.TokenProvider == null)
            return;

        var token = NormalizeToken(
            await _profile.TokenProvider
                .GetTokenAsync(cancellationToken)
                .ConfigureAwait(false));
        _resolvedToken = token;
        _apiClient?.SetAuthToken(token);
    }

    private void OnExecutionConnectionStatusChanged(string status)
    {
        if (!string.Equals(status, "connected", StringComparison.Ordinal))
            return;

        var token = _executionClient?.ResolvedAuthToken;
        _resolvedToken = token;
        _apiClient?.SetAuthToken(token);
        InvalidateCapabilities();
    }

    private bool TryGetCachedCapabilities(
        out SdkCapabilitiesResponse capabilities)
    {
        var generation = Volatile.Read(ref _connectionGeneration);
        var cachedGeneration = Volatile.Read(ref _capabilitiesGeneration);
        var cached = Volatile.Read(ref _capabilities);
        if (cached != null && cachedGeneration == generation)
        {
            capabilities = cached;
            return true;
        }

        capabilities = null!;
        return false;
    }

    private void InvalidateCapabilities()
        => Interlocked.Increment(ref _connectionGeneration);

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        if (_executionClient != null)
            _executionClient.ConnectionStatusChanged -=
                OnExecutionConnectionStatusChanged;
        _executionClient?.Dispose();
        _apiClient?.Dispose();
        _capabilitiesLock.Dispose();
        _initializationLock.Dispose();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        _disposed = true;
        if (_executionClient != null)
            _executionClient.ConnectionStatusChanged -=
                OnExecutionConnectionStatusChanged;
        if (_executionClient != null)
            await _executionClient.DisposeAsync().ConfigureAwait(false);
        _apiClient?.Dispose();
        _capabilitiesLock.Dispose();
        _initializationLock.Dispose();
        GC.SuppressFinalize(this);
    }
}

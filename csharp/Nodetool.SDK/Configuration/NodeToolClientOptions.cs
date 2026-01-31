namespace Nodetool.SDK.Configuration;

/// <summary>
/// Shared SDK configuration (discovery + execution).
///
/// Important: library code should not hardcode localhost defaults.
/// Callers (console/Unity/VL) must provide explicit endpoints.
/// </summary>
public sealed class NodeToolClientOptions
{
    public Uri WorkerWebSocketUrl { get; init; } = null!;

    /// <summary>
    /// Optional HTTP API base URL (for discovery endpoints).
    /// </summary>
    public Uri? ApiBaseUrl { get; init; }

    /// <summary>
    /// Optional auth token forwarded to the worker (depends on server config).
    /// </summary>
    public string? AuthToken { get; init; }

    /// <summary>
    /// Optional user id forwarded to the worker (depends on server config).
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Optional API URL forwarded to the worker (used by some server deployments).
    /// </summary>
    public string? ApiUrl { get; init; }

    /// <summary>
    /// Default execution strategy; server may ignore/override.
    /// </summary>
    public string ExecutionStrategy { get; init; } = "threaded";

    /// <summary>
    /// Prefer explicit types for predictable primitive/list wrapping.
    /// </summary>
    public bool ExplicitTypes { get; init; } = true;

    public Uri GetNormalizedWorkerWebSocketUrl()
    {
        if (WorkerWebSocketUrl == null)
        {
            throw new InvalidOperationException("WorkerWebSocketUrl must be provided.");
        }

        // Accept http/https inputs as a convenience, but keep caller in control of host/port/path.
        if (WorkerWebSocketUrl.Scheme == "http")
        {
            var builder = new UriBuilder(WorkerWebSocketUrl) { Scheme = "ws", Port = WorkerWebSocketUrl.Port };
            return builder.Uri;
        }
        if (WorkerWebSocketUrl.Scheme == "https")
        {
            var builder = new UriBuilder(WorkerWebSocketUrl) { Scheme = "wss", Port = WorkerWebSocketUrl.Port };
            return builder.Uri;
        }
        return WorkerWebSocketUrl;
    }
}

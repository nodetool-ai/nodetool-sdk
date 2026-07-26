using Nodetool.SDK.Configuration;

namespace Nodetool.SDK.Connection;

/// <summary>
/// Immutable, host-neutral configuration for one NodeTool server.
/// </summary>
public sealed record NodeToolConnectionProfile
{
    public required Uri ServerUrl { get; init; }
    public Uri? ApiBaseUrl { get; init; }
    public Uri? WorkerWebSocketUrl { get; init; }
    public INodeToolTokenProvider? TokenProvider { get; init; }
    public string? UserId { get; init; }
    public bool AutoReconnect { get; init; } = true;
    public bool ExplicitTypes { get; init; } = true;
    public string ExecutionStrategy { get; init; } = "threaded";
    public NodeToolReadRetryPolicy ReadRetryPolicy { get; init; } =
        NodeToolReadRetryPolicy.Default;

    public Uri ResolveApiBaseUrl()
        => ApiBaseUrl ?? NodeToolEndpointResolver.DeriveApiBaseUrl(ServerUrl);

    public Uri ResolveWorkerWebSocketUrl()
        => WorkerWebSocketUrl ??
           NodeToolEndpointResolver.DeriveWebSocketUrl(ServerUrl);

    public NodeToolClientOptions ToClientOptions()
        => new()
        {
            WorkerWebSocketUrl = ResolveWorkerWebSocketUrl(),
            ApiBaseUrl = ResolveApiBaseUrl(),
            TokenProvider = TokenProvider,
            UserId = UserId,
            AutoReconnect = AutoReconnect,
            ExplicitTypes = ExplicitTypes,
            ExecutionStrategy = ExecutionStrategy,
            ReadRetryPolicy = ReadRetryPolicy
        };
}

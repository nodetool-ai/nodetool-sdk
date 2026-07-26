using System.Net;
using System.Text;
using Nodetool.SDK.Connection;

namespace Nodetool.SDK.Tests.Connection;

public sealed class NodeToolConnectionTests
{
    [Theory]
    [InlineData(
        "ws://localhost:7777/ws?ignored=1",
        "http://localhost:7777/",
        "ws://localhost:7777/ws")]
    [InlineData(
        "https://cloud.example/nodetool/",
        "https://cloud.example/nodetool/",
        "wss://cloud.example/nodetool/ws")]
    [InlineData(
        "wss://cloud.example/nodetool/ws",
        "https://cloud.example/nodetool/",
        "wss://cloud.example/nodetool/ws")]
    public void Profile_DerivesMatchingHttpAndWebSocketEndpoints(
        string server,
        string expectedApi,
        string expectedWebSocket)
    {
        var profile = new NodeToolConnectionProfile
        {
            ServerUrl = new Uri(server)
        };

        Assert.Equal(new Uri(expectedApi), profile.ResolveApiBaseUrl());
        Assert.Equal(
            new Uri(expectedWebSocket),
            profile.ResolveWorkerWebSocketUrl());
    }

    [Fact]
    public void Profile_RejectsUnsupportedSchemes()
    {
        var profile = new NodeToolConnectionProfile
        {
            ServerUrl = new Uri("file:///tmp/nodetool")
        };

        Assert.Throws<ArgumentException>(profile.ResolveApiBaseUrl);
        Assert.Throws<ArgumentException>(profile.ResolveWorkerWebSocketUrl);
    }

    [Fact]
    public async Task Manager_ConfiguresHttpEndpointAndBearerToken()
    {
        var handler = new RecordingHandler();
        using var httpClient = new HttpClient(handler);
        await using var manager = new NodeToolConnectionManager(
            new NodeToolConnectionProfile
            {
                ServerUrl = new Uri("wss://cloud.example/ws"),
                TokenProvider = new StaticNodeToolTokenProvider(" secret ")
            },
            httpClient);

        var api = await manager.GetApiClientAsync();
        var capabilities = await api.GetSdkCapabilitiesAsync();

        Assert.Equal("1", capabilities.ProtocolVersion);
        Assert.Equal(
            new Uri("https://cloud.example/api/sdk/v1/capabilities"),
            handler.RequestUri);
        Assert.Equal("Bearer", handler.AuthorizationScheme);
        Assert.Equal("secret", handler.AuthorizationParameter);
        Assert.Equal("secret", manager.AuthToken);
    }

    [Fact]
    public async Task Manager_RefreshesHttpBearerTokenWhenClientIsBorrowedAgain()
    {
        var handler = new RecordingHandler();
        using var httpClient = new HttpClient(handler);
        var tokenProvider = new CountingTokenProvider();
        await using var manager = new NodeToolConnectionManager(
            new NodeToolConnectionProfile
            {
                ServerUrl = new Uri("https://cloud.example/nodetool"),
                TokenProvider = tokenProvider
            },
            httpClient);

        var first = await manager.GetApiClientAsync();
        await first.GetSdkCapabilitiesAsync();
        var second = await manager.GetApiClientAsync();
        await second.GetSdkCapabilitiesAsync();

        Assert.Same(first, second);
        Assert.Equal(2, tokenProvider.Calls);
        Assert.Equal("token-2", handler.AuthorizationParameter);
        Assert.Equal("token-2", manager.AuthToken);
    }

    [Fact]
    public async Task Manager_RetriesReadOnlySdkRequestWithStableRequestId()
    {
        var handler = new RecordingHandler(failuresBeforeSuccess: 2);
        using var httpClient = new HttpClient(handler);
        await using var manager = new NodeToolConnectionManager(
            new NodeToolConnectionProfile
            {
                ServerUrl = new Uri("http://localhost:7777"),
                ReadRetryPolicy = new NodeToolReadRetryPolicy
                {
                    MaximumAttempts = 3,
                    InitialDelay = TimeSpan.Zero,
                    MaximumDelay = TimeSpan.Zero
                }
            },
            httpClient);

        var api = await manager.GetApiClientAsync();
        await api.GetSdkCapabilitiesAsync();

        Assert.Equal(3, handler.Attempts);
        Assert.Single(handler.RequestIds.Distinct(StringComparer.Ordinal));
        Assert.DoesNotContain(
            handler.RequestIds,
            string.IsNullOrWhiteSpace);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly int _failuresBeforeSuccess;

        public Uri? RequestUri { get; private set; }
        public string? AuthorizationScheme { get; private set; }
        public string? AuthorizationParameter { get; private set; }
        public int Attempts { get; private set; }
        public List<string?> RequestIds { get; } = [];

        public RecordingHandler(int failuresBeforeSuccess = 0)
        {
            _failuresBeforeSuccess = failuresBeforeSuccess;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Attempts++;
            RequestUri = request.RequestUri;
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            AuthorizationParameter = request.Headers.Authorization?.Parameter;
            RequestIds.Add(
                request.Headers.TryGetValues(
                    "X-NodeTool-Request-Id",
                    out var values)
                    ? values.SingleOrDefault()
                    : null);
            if (Attempts <= _failuresBeforeSuccess)
            {
                return Task.FromResult(
                    new HttpResponseMessage(
                        HttpStatusCode.ServiceUnavailable));
            }
            const string json = """
                {
                  "protocol_version": "1",
                  "nodetool_version": "test",
                  "server_time": "2026-07-26T00:00:00Z",
                  "supported_encodings": ["messagepack"],
                  "default_encoding": "messagepack",
                  "profiles": {},
                  "registry_revision": 1,
                  "python_bridge": "ready",
                  "auth_modes": ["bearer"],
                  "asset_uri_schemes": ["asset"],
                  "limits": {
                    "max_rpc_batch": 100,
                    "max_inline_bytes": 0,
                    "max_upload_bytes": 1024,
                    "max_queued_jobs": 0,
                    "max_job_event_replay": 0,
                    "request_timeout_seconds": 30
                  }
                }
                """;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json")
            });
        }
    }

    private sealed class CountingTokenProvider : INodeToolTokenProvider
    {
        public int Calls { get; private set; }

        public ValueTask<string?> GetTokenAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            return ValueTask.FromResult<string?>($"token-{Calls}");
        }
    }
}

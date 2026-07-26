using System.Net;
using System.Text;
using Nodetool.SDK.Connection;

namespace Nodetool.SDK.Tests.Connection;

public sealed class NodeToolConnectionSessionTests
{
    [Fact]
    public void Configure_ReplacesProfileAndReportsDisconnected()
    {
        var initial = Profile("http://localhost:7777");
        using var session = new NodeToolConnectionSession(initial);
        var statuses = new List<string>();
        var profiles = new List<NodeToolConnectionProfile>();
        session.StatusChanged += statuses.Add;
        session.ProfileChanged += profiles.Add;

        Assert.False(session.Configure(initial));
        var replacement = Profile("https://cloud.example/nodetool");
        Assert.True(session.Configure(replacement));

        Assert.Same(replacement, session.Profile);
        Assert.Equal("disconnected", session.Status);
        Assert.Null(session.LastError);
        Assert.Equal(["disconnected"], statuses);
        Assert.Equal([replacement], profiles);
    }

    [Fact]
    public async Task Configure_RecreatesHttpClientForNewProfileAndToken()
    {
        var handler = new RecordingHandler();
        using var httpClient = new HttpClient(handler);
        await using var session = new NodeToolConnectionSession(
            Profile("http://first.example", "first-secret"),
            httpClient);

        var firstClient = await session.GetApiClientAsync();
        await firstClient.GetSdkCapabilitiesAsync();

        session.Configure(
            Profile("https://second.example/root", "second-secret"));
        var secondClient = await session.GetApiClientAsync();
        await secondClient.GetSdkCapabilitiesAsync();

        Assert.Equal(
            [
                new Uri("http://first.example/api/sdk/v1/capabilities"),
                new Uri(
                    "https://second.example/root/api/sdk/v1/capabilities")
            ],
            handler.RequestUris);
        Assert.Equal(
            ["first-secret", "second-secret"],
            handler.BearerTokens);
    }

    [Fact]
    public async Task GetSdkCapabilitiesAsync_CachesOncePerProfile()
    {
        var handler = new RecordingHandler();
        using var httpClient = new HttpClient(handler);
        await using var session = new NodeToolConnectionSession(
            Profile("http://first.example", "first-secret"),
            httpClient);

        var first = await session.GetSdkCapabilitiesAsync();
        var cached = await session.GetSdkCapabilitiesAsync();

        Assert.Same(first, cached);
        Assert.Single(handler.RequestUris);

        session.Configure(
            Profile("https://second.example/root", "second-secret"));
        var replacement = await session.GetSdkCapabilitiesAsync();

        Assert.NotSame(first, replacement);
        Assert.Equal(2, handler.RequestUris.Count);
        Assert.Equal(
            new Uri("https://second.example/root/api/sdk/v1/capabilities"),
            handler.RequestUris[1]);
    }

    [Fact]
    public async Task GetSdkCapabilitiesAsync_CoalescesConcurrentRequests()
    {
        var handler = new RecordingHandler();
        using var httpClient = new HttpClient(handler);
        await using var session = new NodeToolConnectionSession(
            Profile("http://localhost:7777"),
            httpClient);

        var requests = Enumerable
            .Range(0, 8)
            .Select(_ => session.GetSdkCapabilitiesAsync())
            .ToArray();
        var results = await Task.WhenAll(requests);

        Assert.All(
            results,
            result => Assert.Same(results[0], result));
        Assert.Single(handler.RequestUris);
    }

    [Fact]
    public async Task Disconnect_InvalidatesCachedCapabilities()
    {
        var handler = new RecordingHandler();
        using var httpClient = new HttpClient(handler);
        await using var session = new NodeToolConnectionSession(
            Profile("http://localhost:7777"),
            httpClient);

        var first = await session.GetSdkCapabilitiesAsync();
        await session.DisconnectAsync();
        var refreshed = await session.GetSdkCapabilitiesAsync();

        Assert.NotSame(first, refreshed);
        Assert.Equal(2, handler.RequestUris.Count);
    }

    [Fact]
    public async Task Reset_DropsResolvedClientWithoutChangingProfile()
    {
        var handler = new RecordingHandler();
        using var httpClient = new HttpClient(handler);
        var profile = Profile("http://localhost:7777", "secret");
        await using var session = new NodeToolConnectionSession(
            profile,
            httpClient);

        var first = await session.GetApiClientAsync();
        session.Reset();
        var second = await session.GetApiClientAsync();

        Assert.NotSame(first, second);
        Assert.Same(profile, session.Profile);
        Assert.Equal("disconnected", session.Status);
        Assert.Null(session.CurrentClient);
    }

    [Fact]
    public void SetAutoReconnect_UpdatesProfileWithoutResettingSession()
    {
        using var session = new NodeToolConnectionSession(
            Profile("http://localhost:7777"));
        var profiles = new List<NodeToolConnectionProfile>();
        session.ProfileChanged += profiles.Add;

        Assert.True(session.SetAutoReconnect(false));
        Assert.False(session.SetAutoReconnect(false));

        Assert.False(session.Profile.AutoReconnect);
        Assert.Single(profiles);
        Assert.False(profiles[0].AutoReconnect);
        Assert.Equal("disconnected", session.Status);
    }

    [Fact]
    public void Constructor_RejectsUnsupportedProfileBeforeCreatingClients()
    {
        Assert.Throws<ArgumentException>(
            () => new NodeToolConnectionSession(
                Profile("file:///tmp/nodetool")));
    }

    [Fact]
    public async Task DisposedSession_RejectsFurtherUse()
    {
        var session = new NodeToolConnectionSession(
            Profile("http://localhost:7777"));
        await session.DisposeAsync();

        Assert.Throws<ObjectDisposedException>(
            () => session.Configure(Profile("http://localhost:7778")));
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => session.GetApiClientAsync());
    }

    private static NodeToolConnectionProfile Profile(
        string url,
        string? token = null)
        => new()
        {
            ServerUrl = new Uri(url),
            TokenProvider = token == null
                ? null
                : new StaticNodeToolTokenProvider(token)
        };

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<Uri?> RequestUris { get; } = [];
        public List<string?> BearerTokens { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUris.Add(request.RequestUri);
            BearerTokens.Add(
                request.Headers.Authorization?.Parameter);
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
}

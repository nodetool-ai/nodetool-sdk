using System.Net;
using Nodetool.SDK.Api;

namespace Nodetool.SDK.Tests.Api;

public class NodetoolClientContractTests
{
    [Fact]
    public async Task GetNodeTypesAsync_ReadsCurrentBareArray()
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "node-metadata-response.json");
        var fixture = await File.ReadAllTextAsync(fixturePath);
        Uri? requestedUri = null;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            requestedUri = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(fixture) };
        }));
        var client = new NodetoolClient(httpClient);

        var nodes = await client.GetNodeTypesAsync();

        var node = Assert.Single(nodes);
        Assert.Equal("nodetool.constant.String", node.NodeType);
        Assert.Equal("/api/nodes/metadata?fields=full", requestedUri?.PathAndQuery);
    }

    [Fact]
    public async Task GetWorkflowsAsync_FollowsCursorUntilTheLastPage()
    {
        var requestedUris = new List<string>();
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            var pathAndQuery = request.RequestUri!.PathAndQuery;
            requestedUris.Add(pathAndQuery);
            var body = pathAndQuery.Contains("cursor=wf-1", StringComparison.Ordinal)
                ? """{"workflows":[{"id":"wf-2","name":"Second"}],"next":null}"""
                : """{"workflows":[{"id":"wf-1","name":"First"}],"next":"wf-1"}""";
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) };
        }));
        var client = new NodetoolClient(httpClient);

        var workflows = await client.GetWorkflowsAsync();

        Assert.Equal(new[] { "wf-1", "wf-2" }, workflows.Select(workflow => workflow.Id));
        Assert.Equal(
            new[] { "/api/workflows?limit=25", "/api/workflows?limit=25&cursor=wf-1" },
            requestedUris);
    }

    [Fact]
    public async Task GetWorkflowsAsync_RejectsARepeatedCursor()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"workflows":[],"next":"stuck"}""")
            }));
        var client = new NodetoolClient(httpClient);

        var error = await Assert.ThrowsAsync<InvalidDataException>(() => client.GetWorkflowsAsync());

        Assert.Contains("cursor repeated", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(respond(request));
    }
}

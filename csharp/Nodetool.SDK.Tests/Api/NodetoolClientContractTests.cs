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

    [Fact]
    public async Task GetWorkflowSummariesAsync_UsesCompactPaginatedEndpoint()
    {
        var requestedUris = new List<string>();
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            var pathAndQuery = request.RequestUri!.PathAndQuery;
            requestedUris.Add(pathAndQuery);
            var body = pathAndQuery.Contains("cursor=wf-1", StringComparison.Ordinal)
                ? """{"workflows":[{"id":"wf-2","name":"Second","description":"","revision":"r2","run_mode":"workflow"}],"next":null}"""
                : """{"workflows":[{"id":"wf-1","name":"First","description":"","revision":"r1","run_mode":"workflow"}],"next":"wf-1"}""";
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) };
        }));
        var client = new NodetoolClient(httpClient);

        var workflows = await client.GetWorkflowSummariesAsync();

        Assert.Equal(new[] { "wf-1", "wf-2" }, workflows.Select(workflow => workflow.Id));
        Assert.Equal(
            new[] { "/api/sdk/v1/workflows?limit=50", "/api/sdk/v1/workflows?limit=50&cursor=wf-1" },
            requestedUris);
    }

    [Fact]
    public async Task GetWorkflowInterfaceAsync_ReadsAuthoritativeTypedPins()
    {
        Uri? requestedUri = null;
        const string body = """
            {
              "version": 1,
              "workflow_id": "wf/one",
              "etag": "etag-1",
              "source": "server",
              "inputs": [{
                "node_id": "input-1",
                "name": "prompt",
                "description": "Prompt text",
                "required": true,
                "default": "hello",
                "type": { "type": "str", "optional": false, "values": ["hello", "world"], "type_args": [], "type_name": "Prompt" }
              }],
              "outputs": [{
                "node_id": "output-1",
                "name": "image",
                "description": "Result",
                "stream": false,
                "type": { "type": "image", "optional": false, "type_args": [], "type_name": "ImageRef" }
              }],
              "diagnostics": []
            }
            """;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            requestedUri = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) };
        }));
        var client = new NodetoolClient(httpClient);

        var result = await client.GetWorkflowInterfaceAsync("wf/one");

        Assert.Equal("/api/workflows/wf%2Fone/interface?version=1", requestedUri?.PathAndQuery);
        Assert.Equal("str", Assert.Single(result.Inputs).Type.Type);
        Assert.Equal("hello", result.Inputs[0].Default.GetString());
        Assert.Equal("image", Assert.Single(result.Outputs).Type.Type);
        Assert.Equal("ImageRef", result.Outputs[0].Type.TypeName);
    }

    [Fact]
    public async Task GetWorkflowSummariesAsync_ReportsDisabledBackendFeature()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent(
                    """{"code":"SDK_WORKFLOW_INTERFACE_DISABLED","detail":"SDK workflow interface v1 is disabled"}""")
            }));
        var client = new NodetoolClient(httpClient);

        var error = await Assert.ThrowsAsync<WorkflowInterfaceUnavailableException>(
            () => client.GetWorkflowSummariesAsync());

        Assert.Equal(HttpStatusCode.ServiceUnavailable, error.StatusCode);
        Assert.Equal("SDK_WORKFLOW_INTERFACE_DISABLED", error.ApiCode);
        Assert.Contains("disabled", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(respond(request));
    }
}

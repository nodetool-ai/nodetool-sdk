using System.Net;
using System.Text;
using Nodetool.SDK.Api;
using Nodetool.SDK.Configuration;
using Nodetool.SDK.VL.Services;
using Xunit;

namespace Nodetool.SDK.VL.UnitTests;

public class WorkflowMetadataServiceTests
{
    [Fact]
    public async Task Fetch_ProjectsPortableCatalogSnapshotToExistingVlModel()
    {
        var handler = new DiscoveryHandler();
        using var httpClient = new HttpClient(handler);
        using var apiClient = new NodetoolClient(httpClient);
        using var service = new WorkflowMetadataService(apiClient);
        service.Configure(new NodetoolOptions
        {
            BaseUrl = "http://localhost:7777"
        });

        var workflows = await service.FetchWorkflowMetadataAsync();

        var workflow = Assert.Single(workflows);
        Assert.Equal("workflow-1", workflow.Id);
        Assert.Equal("workflow-1", workflow.Descriptor?.Id);
        Assert.Equal("prompt", Assert.Single(workflow.Descriptor!.Inputs).Name);
        Assert.Equal("etag-1", workflow.Interface?.Etag);
        var input = Assert.Single(workflow.GetInputProperties());
        Assert.Equal("prompt", input.Name);
        Assert.True(input.Required);
        Assert.Equal("hello", input.DefaultValue?.ToString());
        Assert.Equal("image", Assert.Single(workflow.GetOutputProperties()).Type.Type);
        Assert.Equal("server", service.InterfaceSource);
        Assert.Equal("0.7.0", service.ServerVersion);
        Assert.Null(service.LastError);

        service.Configure(new NodetoolOptions
        {
            BaseUrl = "http://localhost:7777"
        });
        await service.FetchWorkflowMetadataAsync();

        Assert.Equal(1, handler.WorkflowSummaryRequestCount);
    }

    private sealed class DiscoveryHandler : HttpMessageHandler
    {
        public int WorkflowSummaryRequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath;
            if (request.Method == HttpMethod.Get &&
                path == "/api/sdk/v1/workflows")
            {
                WorkflowSummaryRequestCount++;
            }
            var json = (request.Method.Method, path) switch
            {
                ("GET", "/api/health") => """{"version":"0.7.0"}""",
                ("GET", "/api/sdk/v1/workflows") => """
                    {
                      "workflows": [{
                        "id": "workflow-1",
                        "name": "Workflow One",
                        "description": "Test",
                        "revision": "revision-1",
                        "registry_revision": 7,
                        "run_mode": "workflow"
                      }],
                      "next": null
                    }
                    """,
                ("POST", "/api/sdk/v1/workflow-interfaces") => """
                    {
                      "interfaces": [{
                        "version": 1,
                        "workflow_id": "workflow-1",
                        "etag": "etag-1",
                        "source": "server",
                        "inputs": [{
                          "node_id": "input-1",
                          "name": "prompt",
                          "description": "Prompt",
                          "type": {"type": "string"},
                          "required": true,
                          "default": "hello"
                        }],
                        "outputs": [{
                          "node_id": "output-1",
                          "name": "image",
                          "description": "Image",
                          "type": {"type": "image"},
                          "stream": false
                        }],
                        "diagnostics": []
                      }],
                      "errors": []
                    }
                    """,
                _ => throw new InvalidOperationException(
                    $"Unexpected request: {request.Method} {request.RequestUri}")
            };

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }
}

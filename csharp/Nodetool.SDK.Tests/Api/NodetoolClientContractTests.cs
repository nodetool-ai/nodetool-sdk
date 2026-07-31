using System.Net;
using Nodetool.SDK.Api;
using Nodetool.SDK.Api.Models;

namespace Nodetool.SDK.Tests.Api;

public class NodetoolClientContractTests
{
    [Fact]
    public async Task InjectedHttpClient_RemainsCallerConfigured()
    {
        using var httpClient = new HttpClient(
            new StubHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"version":"test","uptime":1}""")
                }))
        {
            BaseAddress = new Uri("https://caller.example/"),
            Timeout = TimeSpan.FromSeconds(9)
        };
        httpClient.DefaultRequestHeaders.Add("X-Caller", "retained");
        var client = new NodetoolClient(
            new Uri("https://server.example/nodetool"),
            "secret",
            httpClient);

        await client.GetHealthAsync();

        Assert.Equal(
            new Uri("https://caller.example/"),
            httpClient.BaseAddress);
        Assert.Equal(TimeSpan.FromSeconds(9), httpClient.Timeout);
        Assert.Equal(
            "retained",
            Assert.Single(
                httpClient.DefaultRequestHeaders.GetValues("X-Caller")));
    }

    [Fact]
    public async Task ExplicitBaseAddress_PreservesDeploymentSubpathAndAddsHeaders()
    {
        Uri? requestedUri = null;
        string? authorization = null;
        string? userAgent = null;
        using var httpClient = new HttpClient(
            new StubHttpMessageHandler(request =>
            {
                requestedUri = request.RequestUri;
                authorization =
                    request.Headers.Authorization?.ToString();
                userAgent = request.Headers.UserAgent.ToString();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"version":"test","uptime":1}""")
                };
            }));
        var client = new NodetoolClient(
            new Uri("https://server.example/nodetool/"),
            "secret",
            httpClient);

        await client.GetHealthAsync();

        Assert.Equal(
            new Uri("https://server.example/nodetool/api/health"),
            requestedUri);
        Assert.Equal("Bearer secret", authorization);
        Assert.Contains("Nodetool.SDK", userAgent);
    }

    [Fact]
    public async Task GetHealthAsync_ReadsServerVersionAndUptime()
    {
        Uri? requestedUri = null;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            requestedUri = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"version":"0.7.0","uptime":123}""")
            };
        }));
        var client = new NodetoolClient(httpClient);

        var health = await client.GetHealthAsync();

        Assert.Equal("/api/health", requestedUri?.AbsolutePath);
        Assert.Equal("0.7.0", health.Version);
        Assert.Equal(123, health.UptimeSeconds);
    }

    [Fact]
    public async Task GetSdkCapabilitiesAsync_ReadsExecutionOptionSupport()
    {
        Uri? requestedUri = null;
        const string body = """
            {
              "protocol_version": "1",
              "nodetool_version": "0.7.0-rc.32",
              "server_time": "2026-07-26T00:00:00.000Z",
              "supported_encodings": ["messagepack", "json-text"],
              "default_encoding": "messagepack",
              "profiles": { "discovery": "available", "execution": "available" },
              "registry_revision": 2528,
              "python_bridge": "ready",
              "auth_modes": ["trusted_local"],
              "asset_uri_schemes": ["asset"],
              "execution_options": {
                "persistence": ["job", "session"],
                "event_detail": ["full", "outputs", "terminal"],
                "asset_persistence": ["auto", "temporary"],
                "defaults": {
                  "persistence": "job",
                  "event_detail": "full",
                  "asset_persistence": "auto"
                }
              },
              "limits": {
                "max_rpc_batch": 100,
                "max_inline_bytes": 0,
                "max_upload_bytes": 104857600,
                "max_queued_jobs": 0,
                "max_job_event_replay": 0,
                "request_timeout_seconds": 30
              }
            }
            """;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            requestedUri = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body)
            };
        }));
        var client = new NodetoolClient(httpClient);

        var capabilities = await client.GetSdkCapabilitiesAsync();

        Assert.Equal("/api/sdk/v1/capabilities", requestedUri?.AbsolutePath);
        Assert.Equal("1", capabilities.ProtocolVersion);
        Assert.Contains("session", capabilities.ExecutionOptions!.Persistence);
        Assert.Contains("terminal", capabilities.ExecutionOptions.EventDetail);
        Assert.Equal(104857600, capabilities.Limits.MaxUploadBytes);
    }

    [Fact]
    public async Task PreflightWorkflowAsync_PostsTypedRequestAndReadsSummary()
    {
        Uri? requestedUri = null;
        string? requestedBody = null;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            requestedUri = request.RequestUri;
            requestedBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "version": 1,
                      "level": "availability",
                      "workflow_id": "wf-1",
                      "workflow_etag": "rev-2",
                      "runnable": false,
                      "issues": [{
                        "severity": "error",
                        "code": "MISSING_INPUT",
                        "message": "Prompt is required.",
                        "node_id": "input-1",
                        "pin_name": "prompt"
                      }],
                      "requirements": [{
                        "kind": "provider",
                        "id": "openai",
                        "name": "OpenAI",
                        "status": "missing",
                        "blocking": true,
                        "message": "Provider is not configured."
                      }],
                      "cost": {
                        "amount": null,
                        "currency": null,
                        "confidence": "unknown",
                        "unknown_cost_nodes": ["node-1"],
                        "approval_required": false
                      }
                    }
                    """)
            };
        }));
        var client = new NodetoolClient(httpClient);

        var result = await client.PreflightWorkflowAsync(new SdkPreflightRequest
        {
            WorkflowId = "wf-1",
            WorkspaceId = "workspace-1",
            WorkflowEtag = "rev-2",
            Level = SdkPreflightLevels.Availability,
            Inputs = new Dictionary<string, object?> { ["prompt"] = "hello" }
        });

        Assert.Equal("/api/sdk/v1/preflight", requestedUri?.AbsolutePath);
        Assert.Contains("\"workflow_id\":\"wf-1\"", requestedBody);
        Assert.Contains("\"interface_version\":1", requestedBody);
        Assert.False(result.Runnable);
        Assert.Equal("MISSING_INPUT", Assert.Single(result.Issues).Code);
        Assert.Equal("openai", Assert.Single(result.Requirements).Id);
        Assert.Equal("node-1", Assert.Single(result.Cost!.UnknownCostNodes));
    }

    [Fact]
    public async Task PreflightWorkflowAsync_ExposesStructuredServerFailure()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent(
                    """{"code":"PREFLIGHT_LEVEL_UNAVAILABLE","detail":"Execution readiness unavailable.","retryable":false}""")
            }));
        var client = new NodetoolClient(httpClient);

        var error = await Assert.ThrowsAsync<SdkApiException>(() =>
            client.PreflightWorkflowAsync(new SdkPreflightRequest
            {
                WorkflowId = "wf-1",
                Level = SdkPreflightLevels.Execution
            }));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, error.StatusCode);
        Assert.Equal("PREFLIGHT_LEVEL_UNAVAILABLE", error.ApiCode);
        Assert.False(error.Retryable);
    }

    [Fact]
    public async Task PreflightWorkflowAsync_SendsExplicitExecutionTarget()
    {
        string? requestedBody = null;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(
            request =>
            {
                requestedBody = request.Content!
                    .ReadAsStringAsync()
                    .GetAwaiter()
                    .GetResult();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """
                        {
                          "version": 1,
                          "level": "execution",
                          "workflow_id": "wf-1",
                          "workflow_etag": null,
                          "runnable": true,
                          "issues": [],
                          "requirements": [],
                          "cost": null
                        }
                        """)
                };
            }));
        var client = new NodetoolClient(httpClient);

        await client.PreflightWorkflowAsync(new SdkPreflightRequest
        {
            WorkflowId = "wf-1",
            Level = SdkPreflightLevels.Execution,
            ExecutionTarget = new SdkExecutionTarget
            {
                Kind = SdkExecutionTargetKinds.Worker,
                WorkerId = "worker-1",
                Concurrent = true
            }
        });

        Assert.Contains(
            "\"execution_target\":{\"kind\":\"worker\"," +
            "\"worker_id\":\"worker-1\",\"concurrent\":true}",
            requestedBody);
    }

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
        Assert.True(node.SupportsDynamicInputs);
        Assert.True(node.SupportsDynamicOutputs);
        Assert.True(node.IsStreamingInput);
        Assert.True(node.IsStreamingOutput);
        Assert.Equal("python", Assert.Single(node.RequiredRuntimes));
        Assert.Equal("TEST_TOKEN", Assert.Single(node.RequiredSettings));
        var property = Assert.Single(node.Properties);
        Assert.True(property.Required);
        Assert.Equal("list", property.Type.Type);
        Assert.Equal("image", Assert.Single(property.Type.TypeArgs!).Type);
        Assert.True(Assert.Single(node.Outputs).Stream);
        Assert.Equal("/api/nodes/metadata?fields=full", requestedUri?.PathAndQuery);
    }

    [Fact]
    public async Task GetNodeTypesAsync_TreatsNullOptionalCapabilitiesAsFalse()
    {
        const string body = """
            [{
              "node_type": "python.OptionalCapabilities",
              "title": "Optional capabilities",
              "namespace": "python",
              "description": "",
              "properties": [],
              "outputs": [],
              "supports_dynamic_inputs": null,
              "supports_dynamic_outputs": null,
              "is_streaming_input": null,
              "is_streaming_output": null,
              "hidden": null,
              "deprecated": null
            }]
            """;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body)
            }));
        var client = new NodetoolClient(httpClient);

        var node = Assert.Single(await client.GetNodeTypesAsync());

        Assert.False(node.SupportsDynamicInputs);
        Assert.False(node.SupportsDynamicOutputs);
        Assert.False(node.IsStreamingInput);
        Assert.False(node.IsStreamingOutput);
        Assert.False(node.Hidden);
        Assert.False(node.Deprecated);
    }

    [Fact]
    public async Task GetNodeTypeInventoryAsync_ReadsBoundedHybridTypeUsage()
    {
        Uri? requestedUri = null;
        const string body = """
            {
              "version": 1,
              "registry_revision": 12,
              "registry_ready": true,
              "python_bridge_ready": true,
              "node_count": 2527,
              "type_count": 100,
              "provenance_counts": { "typescript": 2412, "python-bridge": 115 },
              "cursor": 20,
              "next_cursor": 21,
              "types": [{
                "signature": "list[image]",
                "type": "list",
                "type_name": null,
                "optional": false,
                "type_args": ["image"],
                "values": [],
                "values_truncated": false,
                "input_uses": 10,
                "output_uses": 2,
                "node_count": 9,
                "sources": { "typescript": 8, "python-bridge": 4 },
                "examples": [{
                  "node_type": "python.Images",
                  "pin": "images",
                  "direction": "input"
                }]
              }],
              "unavailable_packs": [{
                "id": "transformers-js",
                "name": "Transformers.js",
                "reason": "disabled by built-in pack configuration"
              }]
            }
            """;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            requestedUri = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body)
            };
        }));
        var client = new NodetoolClient(httpClient);

        var inventory = await client.GetNodeTypeInventoryAsync(cursor: 20, limit: 1);

        Assert.Equal("/api/sdk/v1/node-types?cursor=20&limit=1", requestedUri?.PathAndQuery);
        Assert.Equal(12, inventory.RegistryRevision);
        Assert.True(inventory.PythonBridgeReady);
        Assert.Equal(115, inventory.ProvenanceCounts["python-bridge"]);
        Assert.Equal("list[image]", Assert.Single(inventory.Types).Signature);
        Assert.Equal("python.Images", Assert.Single(inventory.Types[0].Examples).NodeType);
        Assert.Equal("transformers-js", Assert.Single(inventory.UnavailablePacks).Id);
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
                ? """{"workflows":[{"id":"wf-2","name":"Second","description":"","revision":"r2","registry_revision":8,"run_mode":"workflow"}],"next":null}"""
                : """{"workflows":[{"id":"wf-1","name":"First","description":"","revision":"r1","registry_revision":7,"run_mode":"workflow"}],"next":"wf-1"}""";
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) };
        }));
        var client = new NodetoolClient(httpClient);

        var workflows = await client.GetWorkflowSummariesAsync();

        Assert.Equal(new[] { "wf-1", "wf-2" }, workflows.Select(workflow => workflow.Id));
        Assert.Equal(new long?[] { 7, 8 }, workflows.Select(workflow => workflow.RegistryRevision));
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
                "stream": true,
                "stream_kind": "image",
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
        Assert.True(result.Outputs[0].Stream);
        Assert.Equal("image", result.Outputs[0].StreamKind);
    }

    [Fact]
    public async Task GetWorkflowInterfacesAsync_PostsBoundedBatchAndReadsItemErrors()
    {
        HttpMethod? requestedMethod = null;
        string? requestedBody = null;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            requestedMethod = request.Method;
            requestedBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"interfaces":[{"version":1,"workflow_id":"wf-2","etag":null,"source":"server","inputs":[],"outputs":[],"diagnostics":[]}],"errors":[{"workflow_id":"wf-1","code":"invalid_graph","message":"Workflow graph is invalid"}]}""")
            };
        }));
        var client = new NodetoolClient(httpClient);

        var result = await client.GetWorkflowInterfacesAsync(new[] { "wf-2", "wf-1" });

        Assert.Equal(HttpMethod.Post, requestedMethod);
        Assert.Contains("\"ids\":[\"wf-2\",\"wf-1\"]", requestedBody);
        Assert.Contains("\"version\":1", requestedBody);
        Assert.Equal("wf-2", Assert.Single(result.Interfaces).WorkflowId);
        Assert.Equal("invalid_graph", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task GetWorkflowInterfacesAsync_RejectsMoreThanOneHundredIdsLocally()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
            throw new InvalidOperationException("HTTP should not be called")));
        var client = new NodetoolClient(httpClient);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            client.GetWorkflowInterfacesAsync(
                Enumerable.Range(0, 101).Select(index => $"wf-{index}").ToArray()));
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

        var error = await Assert.ThrowsAsync<SdkApiException>(
            () => client.GetWorkflowSummariesAsync());

        Assert.Equal(HttpStatusCode.ServiceUnavailable, error.StatusCode);
        Assert.Equal(SdkApiTransport.Http, error.Transport);
        Assert.Equal("SDK_WORKFLOW_INTERFACE_DISABLED", error.ApiCode);
        Assert.Contains("disabled", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetModelCatalogAsync_SendsFiltersAndPreservesStructuredWireValue()
    {
        Uri? requestedUri = null;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            requestedUri = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "version":"1",
                      "catalog_revision":"revision-1",
                      "scope":"local",
                      "entries":[{
                        "key":"language_model|openai|gpt-test|",
                        "display_name":"GPT Test",
                        "compatibility":"language_model",
                        "availability":"ready_remote",
                        "recommended":false,
                        "scope":"local",
                        "provider":"openai",
                        "id":"gpt-test",
                        "repo_id":null,
                        "path":null,
                        "supported_tasks":["text_generation"],
                        "size_on_disk":null,
                        "wire_value":{"type":"language_model","provider":"openai","id":"gpt-test","name":"GPT Test"}
                      }],
                      "next_cursor":null
                    }
                    """)
            };
        }));
        var client = new NodetoolClient(httpClient);

        var result = await client.GetModelCatalogAsync(
            new SdkModelCatalogQuery(
                Compatibility: "language_model",
                Availability: SdkModelAvailability.ReadyRemote,
                Provider: "openai",
                Limit: 25));

        Assert.Equal(
            "/api/sdk/v1/models?compatibility=language_model&availability=ready_remote&provider=openai&scope=local&limit=25",
            requestedUri?.PathAndQuery);
        var model = Assert.Single(result.Entries);
        Assert.Equal("openai", model.WireValue.GetProperty("provider").GetString());
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(respond(request));
    }
}

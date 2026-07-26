using System.Text.Json;
using Nodetool.SDK.Api.Models;
using Nodetool.SDK.Workflows;

namespace Nodetool.SDK.Tests.Workflows;

public class WorkflowCatalogTests
{
    [Fact]
    public async Task Refresh_MapsAuthoritativeInterfaceToImmutableDescriptor()
    {
        var client = new FakeDiscoveryClient
        {
            Summaries = [Summary()],
            Interfaces = [Interface()]
        };
        using var catalog = CreateCatalog(client);

        var snapshot = await catalog.RefreshAsync();

        var workflow = Assert.Single(snapshot.Workflows);
        Assert.Equal("workflow-1", workflow.Id);
        Assert.Equal("server", workflow.InterfaceSource);
        var input = Assert.Single(workflow.Inputs);
        Assert.Equal("prompt", input.Name);
        Assert.True(input.Required);
        Assert.Equal("hello", input.DefaultValue?.GetString());
        Assert.Equal("image", Assert.Single(workflow.Outputs).Type.Type);
        Assert.False(snapshot.IsStale);
    }

    [Fact]
    public async Task Refresh_ReusesRevisionAndRegistryScopedDescriptor()
    {
        var scope = $"test://{Guid.NewGuid():N}";
        var firstClient = new FakeDiscoveryClient
        {
            Summaries = [Summary()],
            Interfaces = [Interface()]
        };
        using (var first = new WorkflowCatalog(
                   firstClient,
                   scope,
                   TimeSpan.Zero))
        {
            await first.RefreshAsync();
        }

        var secondClient = new FakeDiscoveryClient
        {
            Summaries = [Summary()],
            ThrowIfInterfacesRequested = true
        };
        using var second = new WorkflowCatalog(
            secondClient,
            scope,
            TimeSpan.Zero);

        var snapshot = await second.RefreshAsync();

        Assert.Single(snapshot.Workflows);
        Assert.Equal(1, snapshot.CacheHitCount);
        Assert.Equal(0, secondClient.InterfaceRequestCount);
    }

    [Fact]
    public async Task FailedRefresh_PreservesLastKnownGoodSnapshotAsStale()
    {
        var client = new FakeDiscoveryClient
        {
            Summaries = [Summary()],
            Interfaces = [Interface()]
        };
        using var catalog = CreateCatalog(client);
        await catalog.RefreshAsync();
        client.RefreshError = new HttpRequestException("offline");

        var snapshot = await catalog.RefreshAsync(force: true);

        Assert.Single(snapshot.Workflows);
        Assert.True(snapshot.IsStale);
        Assert.Equal("offline", snapshot.LastError);
    }

    [Fact]
    public async Task InvalidChangedInterface_PreservesPreviousDescriptor()
    {
        var client = new FakeDiscoveryClient
        {
            Summaries = [Summary()],
            Interfaces = [Interface()]
        };
        using var catalog = CreateCatalog(client);
        await catalog.RefreshAsync();
        client.Summaries = [Summary(revision: "revision-2")];
        client.Interfaces =
        [
            Interface(diagnostics:
            [
                new WorkflowInterfaceDiagnostic
                {
                    Severity = "error",
                    Code = "invalid_graph",
                    Message = "broken"
                }
            ])
        ];

        var snapshot = await catalog.RefreshAsync(force: true);

        Assert.Equal("revision-1", Assert.Single(snapshot.Workflows).Revision);
        Assert.Equal(1, snapshot.SkippedCount);
    }

    private static WorkflowCatalog CreateCatalog(FakeDiscoveryClient client)
        => new(
            client,
            $"test://{Guid.NewGuid():N}",
            TimeSpan.Zero);

    private static WorkflowSummaryResponse Summary(
        string revision = "revision-1")
        => new()
        {
            Id = "workflow-1",
            Name = "Workflow One",
            Description = "Test workflow",
            Revision = revision,
            RegistryRevision = 7,
            RunMode = "workflow"
        };

    private static WorkflowInterfaceResponse Interface(
        List<WorkflowInterfaceDiagnostic>? diagnostics = null)
        => new()
        {
            Version = 1,
            WorkflowId = "workflow-1",
            Etag = "etag-1",
            Source = "server",
            Inputs =
            [
                new WorkflowInterfaceInput
                {
                    NodeId = "input-1",
                    Name = "prompt",
                    Description = "Prompt",
                    Type = new NodeTypeDefinition { Type = "string" },
                    Required = true,
                    Default = Json("\"hello\"")
                }
            ],
            Outputs =
            [
                new WorkflowInterfaceOutput
                {
                    NodeId = "output-1",
                    Name = "image",
                    Description = "Image",
                    Type = new NodeTypeDefinition { Type = "image" }
                }
            ],
            Diagnostics = diagnostics ?? []
        };

    private static JsonElement Json(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private sealed class FakeDiscoveryClient : IWorkflowDiscoveryClient
    {
        public List<WorkflowSummaryResponse> Summaries { get; set; } = [];
        public List<WorkflowInterfaceResponse> Interfaces { get; set; } = [];
        public Exception? RefreshError { get; set; }
        public bool ThrowIfInterfacesRequested { get; set; }
        public int InterfaceRequestCount { get; private set; }

        public Task<List<WorkflowSummaryResponse>> GetWorkflowSummariesAsync(
            CancellationToken cancellationToken = default)
            => RefreshError is null
                ? Task.FromResult(Summaries)
                : Task.FromException<List<WorkflowSummaryResponse>>(RefreshError);

        public Task<WorkflowInterfaceResponse> GetWorkflowInterfaceAsync(
            string workflowId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Interfaces.Single(
                item => item.WorkflowId == workflowId));

        public Task<WorkflowInterfacesResponse> GetWorkflowInterfacesAsync(
            IReadOnlyCollection<string> workflowIds,
            CancellationToken cancellationToken = default)
        {
            InterfaceRequestCount++;
            if (ThrowIfInterfacesRequested)
                throw new InvalidOperationException("Interfaces should be cached.");

            return Task.FromResult(new WorkflowInterfacesResponse
            {
                Interfaces = Interfaces
                    .Where(item => workflowIds.Contains(item.WorkflowId))
                    .ToList()
            });
        }
    }
}

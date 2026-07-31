using System.Text.Json;
using Nodetool.SDK.Api.Models;
using Nodetool.SDK.Models;

namespace Nodetool.SDK.Tests.Models;

public class ModelDownloadServiceTests
{
    [Fact]
    public async Task Start_UsesCatalogDescriptorAndStoresInitialState()
    {
        var client = new FakeDownloadClient();
        var service = new ModelDownloadService(client);

        var state = await service.StartAsync(Model());

        Assert.Equal("org/model", client.StartRequests.Single().RepositoryId);
        Assert.Equal("hf.text_generation", client.StartRequests.Single().ModelType);
        Assert.Equal(state, Assert.Single(service.Snapshot.Downloads));
        Assert.Equal(0.25, state.Progress);
    }

    [Fact]
    public async Task Monitor_EmitsChangesOnlyAndRefreshesCatalogOnCompletion()
    {
        var client = new FakeDownloadClient();
        client.Snapshots.Enqueue(Snapshot(State("progress", 25, 1)));
        client.Snapshots.Enqueue(Snapshot(State("progress", 25, 1)));
        client.Snapshots.Enqueue(Snapshot(State("completed", 100, 2)));
        var catalog = new FakeCatalog();
        var service = new ModelDownloadService(client, catalog: catalog);
        var updates = new List<ModelDownloadState>();

        await foreach (var update in service.MonitorAsync(
            "mdl_test",
            TimeSpan.FromMilliseconds(1)))
        {
            updates.Add(update);
        }

        Assert.Equal(
            [SdkModelDownloadStatuses.Progress, SdkModelDownloadStatuses.Completed],
            updates.Select(update => update.Status));
        Assert.Equal(1, catalog.RefreshCount);
    }

    [Fact]
    public async Task Retry_RequiresTerminalStateAndPreservesDownloadIdentity()
    {
        var client = new FakeDownloadClient();
        var service = new ModelDownloadService(client);
        var failed = Map(State("error", 25, 2));

        var retried = await service.RetryAsync(failed);

        Assert.Equal(failed.OperationId, retried.OperationId);
        Assert.Equal(failed.RepositoryId, client.StartRequests.Single().RepositoryId);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RetryAsync(retried));
    }

    private static ModelDescriptor Model()
    {
        using var document = JsonDocument.Parse(
            """{"type":"hf.text_generation","repo_id":"org/model","path":null}""");
        return new ModelDescriptor(
            "hf.text_generation||org/model|",
            "Test model",
            "hf.text_generation",
            SdkModelAvailability.Downloadable,
            true,
            SdkModelScopes.Local,
            null,
            "org/model",
            "org/model",
            null,
            [],
            null,
            document.RootElement.Clone());
    }

    private static SdkModelDownloadSnapshotResponse Snapshot(
        SdkModelDownloadStateResponse state)
        => new() { Version = "1", Downloads = [state] };

    private static SdkModelDownloadStateResponse State(
        string status,
        long bytes,
        int second)
        => new()
        {
            Version = "1",
            OperationId = "mdl_test",
            Scope = "local",
            RepositoryId = "org/model",
            ModelType = "hf.text_generation",
            Status = status,
            DownloadedBytes = bytes,
            TotalBytes = 100,
            CurrentFiles = status == "completed" ? [] : ["model.bin"],
            TotalFiles = 1,
            StartedAt = DateTimeOffset.Parse("2026-07-31T00:00:00Z"),
            UpdatedAt = DateTimeOffset.Parse($"2026-07-31T00:00:0{second}Z")
        };

    private static ModelDownloadState Map(SdkModelDownloadStateResponse state)
        => new(
            state.OperationId,
            state.Scope,
            state.RepositoryId,
            state.Path,
            state.ModelType,
            state.Status,
            state.DownloadedBytes,
            state.TotalBytes,
            state.DownloadedFiles,
            state.CurrentFiles,
            state.TotalFiles,
            state.Error,
            state.StartedAt,
            state.UpdatedAt);

    private sealed class FakeDownloadClient : IModelDownloadClient
    {
        public List<SdkModelDownloadStartRequest> StartRequests { get; } = [];
        public Queue<SdkModelDownloadSnapshotResponse> Snapshots { get; } = [];

        public Task<SdkModelDownloadStateResponse> StartModelDownloadAsync(
            SdkModelDownloadStartRequest request,
            CancellationToken cancellationToken = default)
        {
            StartRequests.Add(request);
            return Task.FromResult(State("start", 25, 1));
        }

        public Task<SdkModelDownloadSnapshotResponse> GetModelDownloadsAsync(
            SdkModelDownloadQuery? query = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Snapshots.Dequeue());

        public Task<SdkModelDownloadStateResponse> CancelModelDownloadAsync(
            string operationId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(State("cancelled", 25, 2));
    }

    private sealed class FakeCatalog : IModelCatalog
    {
        public int RefreshCount { get; private set; }
        public ModelCatalogSnapshot Snapshot => ModelCatalogSnapshot.Empty;

        public Task<ModelCatalogSnapshot> RefreshAsync(
            bool force = false,
            CancellationToken cancellationToken = default)
        {
            RefreshCount++;
            return Task.FromResult(Snapshot);
        }

        public IReadOnlyList<ModelDescriptor> FindCompatible(
            string compatibility,
            bool readyOnly = true) => [];

        public ModelDescriptor? GetByKey(string key) => null;
        public void Clear() { }
    }
}

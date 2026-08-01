using System.Text.Json;
using Nodetool.SDK.Api.Models;
using Nodetool.SDK.Models;

namespace Nodetool.SDK.Tests.Models;

public class ModelCatalogTests
{
    [Fact]
    public async Task Refresh_PaginatesAndMapsReadyCompatibleModels()
    {
        var client = new FakeModelCatalogClient(
            Response("revision-1", Entry("openai", "ready_remote"), "next"),
            Response("revision-1", Entry("local", "ready_local")));
        using var catalog = new ModelCatalog(
            client,
            "https://server|alice|local",
            cacheDuration: TimeSpan.Zero);

        var snapshot = await catalog.RefreshAsync();

        Assert.Equal("revision-1", snapshot.Revision);
        Assert.Equal(2, snapshot.Models.Count);
        Assert.Equal(2, catalog.FindCompatible("language_model").Count);
        Assert.Equal("openai", snapshot.Models[0].Select()
            .ToInputValue()
            .As<Dictionary<string, object?>>()?["provider"]);
        Assert.Equal(new[] { null, "next" }, client.Cursors);
    }

    [Fact]
    public async Task FailedRefresh_PreservesLastKnownGoodSnapshot()
    {
        var client = new FakeModelCatalogClient(
            Response("revision-1", Entry("openai", "ready_remote")));
        using var catalog = new ModelCatalog(
            client,
            "https://server|alice|local",
            cacheDuration: TimeSpan.Zero);
        await catalog.RefreshAsync();
        client.Error = new HttpRequestException("offline");

        var stale = await catalog.RefreshAsync(force: true);

        Assert.Single(stale.Models);
        Assert.True(stale.IsStale);
        Assert.Equal("offline", stale.LastError);
    }

    [Fact]
    public async Task Refresh_RecoversAfterTransientFailure()
    {
        var client = new FakeModelCatalogClient(
            Response("revision-1", Entry("openai", "ready_remote")),
            Response("revision-2", Entry("anthropic", "ready_remote")));
        using var catalog = new ModelCatalog(
            client,
            "https://server|alice|local",
            cacheDuration: TimeSpan.Zero);
        await catalog.RefreshAsync();
        client.Error = new HttpRequestException("offline");
        await catalog.RefreshAsync(force: true);

        client.Error = null;
        var recovered = await catalog.RefreshAsync(force: true);

        Assert.Equal("revision-2", recovered.Revision);
        Assert.False(recovered.IsStale);
        Assert.Null(recovered.LastError);
        Assert.Equal("anthropic", Assert.Single(recovered.Models).Provider);
    }

    [Fact]
    public async Task CatalogInstances_DoNotShareSnapshotsAcrossCacheScopes()
    {
        var client = new FakeModelCatalogClient(
            Response("revision-a", Entry("openai", "ready_remote")),
            Response("revision-b", Entry("local", "ready_local")));
        using var first = new ModelCatalog(
            client,
            "https://server-a|alice|local",
            cacheDuration: TimeSpan.FromMinutes(5));
        using var second = new ModelCatalog(
            client,
            "https://server-b|alice|local",
            cacheDuration: TimeSpan.FromMinutes(5));

        var firstSnapshot = await first.RefreshAsync();
        var secondSnapshot = await second.RefreshAsync();

        Assert.Equal("revision-a", firstSnapshot.Revision);
        Assert.Equal("revision-b", secondSnapshot.Revision);
        Assert.Equal(2, client.Cursors.Count);
    }

    [Fact]
    public async Task Dispose_DoesNotBreakAnInFlightRefresh()
    {
        var response = Response(
            "revision-1",
            Entry("openai", "ready_remote"));
        var completion = new TaskCompletionSource<SdkModelCatalogResponse>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var client = new BlockingModelCatalogClient(completion.Task);
        var catalog = new ModelCatalog(
            client,
            "https://server|alice|local",
            cacheDuration: TimeSpan.Zero);

        var refresh = catalog.RefreshAsync();
        await client.Started;
        catalog.Dispose();
        completion.SetResult(response);

        var snapshot = await refresh;

        Assert.Equal("revision-1", snapshot.Revision);
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => catalog.RefreshAsync(force: true));
    }

    [Fact]
    public void Selection_PreservesCompleteStructuredWireValue()
    {
        var descriptor = Map(Entry("openai", "ready_remote")) with
        {
            WireValue = Json(
                """{"type":"language_model","provider":"openai","id":"model","settings":{"quantization":"q4"},"tags":["chat","tools"]}""")
        };

        var value = Assert.IsType<Dictionary<string, object?>>(
            descriptor.Select().ToInputValue());
        var settings = Assert.IsType<Dictionary<string, object?>>(
            value["settings"]);
        var tags = Assert.IsAssignableFrom<IReadOnlyList<object?>>(
            value["tags"]);

        Assert.Equal("q4", settings["quantization"]);
        Assert.Equal(new object?[] { "chat", "tools" }, tags);
    }

    [Fact]
    public void Selection_RejectsMismatchedWireType()
    {
        var descriptor = Map(Entry("openai", "ready_remote")) with
        {
            WireValue = Json("""{"type":"image_model"}""")
        };

        Assert.Throws<InvalidDataException>(() => descriptor.Select());
    }

    private static SdkModelCatalogResponse Response(
        string revision,
        SdkModelCatalogEntryResponse entry,
        string? nextCursor = null)
        => new()
        {
            Version = "1",
            CatalogRevision = revision,
            Scope = "local",
            Entries = [entry],
            NextCursor = nextCursor
        };

    private static SdkModelCatalogEntryResponse Entry(
        string provider,
        string availability)
        => new()
        {
            Key = $"language_model|{provider}|model|",
            DisplayName = $"{provider} model",
            Compatibility = "language_model",
            Availability = availability,
            Scope = "local",
            Provider = provider,
            Id = "model",
            WireValue = Json(
                $$"""{"type":"language_model","provider":"{{provider}}","id":"model","name":"{{provider}} model"}""")
        };

    private static ModelDescriptor Map(SdkModelCatalogEntryResponse entry)
        => new(
            entry.Key,
            entry.DisplayName,
            entry.Compatibility,
            entry.Availability,
            entry.Recommended,
            entry.Scope,
            entry.Provider,
            entry.Id,
            entry.RepositoryId,
            entry.Path,
            entry.SupportedTasks,
            entry.SizeOnDisk,
            entry.WireValue);

    private static JsonElement Json(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private sealed class FakeModelCatalogClient(
        params SdkModelCatalogResponse[] responses) : IModelCatalogClient
    {
        private readonly Queue<SdkModelCatalogResponse> _responses = new(responses);
        public List<string?> Cursors { get; } = [];
        public Exception? Error { get; set; }

        public Task<SdkModelCatalogResponse> GetModelCatalogAsync(
            SdkModelCatalogQuery? query = null,
            CancellationToken cancellationToken = default)
        {
            Cursors.Add(query?.Cursor);
            return Error == null
                ? Task.FromResult(_responses.Dequeue())
                : Task.FromException<SdkModelCatalogResponse>(Error);
        }
    }

    private sealed class BlockingModelCatalogClient(
        Task<SdkModelCatalogResponse> response) : IModelCatalogClient
    {
        private readonly TaskCompletionSource _started = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Started => _started.Task;

        public Task<SdkModelCatalogResponse> GetModelCatalogAsync(
            SdkModelCatalogQuery? query = null,
            CancellationToken cancellationToken = default)
        {
            _started.TrySetResult();
            return response.WaitAsync(cancellationToken);
        }
    }
}

internal static class ObjectAssertionExtensions
{
    public static T? As<T>(this object value) where T : class => value as T;
}

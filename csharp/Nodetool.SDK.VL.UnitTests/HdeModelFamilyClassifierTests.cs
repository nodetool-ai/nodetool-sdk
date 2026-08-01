using System.Text.Json;
using Nodetool.SDK.Api.Models;
using Nodetool.SDK.Models;
using Nodetool.SDK.VL.Hde;
using Xunit;

namespace Nodetool.SDK.VL.UnitTests;

public sealed class HdeModelFamilyClassifierTests
{
    [Theory]
    [InlineData("language_model", "Language")]
    [InlineData("embedding_model", "Language")]
    [InlineData("hf.text_generation", "Language")]
    [InlineData("image_model", "Image")]
    [InlineData("hf.flux", "Image")]
    [InlineData("hf.depth_estimation", "Image")]
    [InlineData("asr_model", "Audio")]
    [InlineData("hf.text_to_audio", "Audio")]
    [InlineData("video_model", "Video")]
    [InlineData("hf.audio_to_video", "Video")]
    [InlineData("mesh_model", "ThreeD")]
    [InlineData("hf.point_cloud_generation", "ThreeD")]
    [InlineData("custom_model", "Other")]
    public void CompatibilityMapsToBroadEditorFamily(
        string compatibility,
        string expected)
        => Assert.Equal(
            expected,
            HdeModelFamilyClassifier.Classify(compatibility).ToString());

    [Fact]
    public void Projection_FiltersBeforePaginatingAndClampsPage()
    {
        var models = Enumerable.Range(1, 205)
            .Select(index => Model(
                $"image-{index:000}",
                $"Image {index:000}",
                "image_model"))
            .ToArray();
        var catalog = Catalog(models);

        var thirdPage = HdeModelListProjector.Project(
            catalog,
            ModelDownloadSnapshot.Empty(SdkModelScopes.Local),
            HdeModelFamily.Image,
            "",
            pageIndex: 2,
            pageSize: 100);

        Assert.Equal(3, thirdPage.PageNumber);
        Assert.Equal(3, thirdPage.PageCount);
        Assert.Equal(201, thirdPage.RangeStart);
        Assert.Equal(205, thirdPage.RangeEnd);
        Assert.Equal(5, thirdPage.Rows.Count);

        var search = HdeModelListProjector.Project(
            catalog,
            ModelDownloadSnapshot.Empty(SdkModelScopes.Local),
            HdeModelFamily.All,
            "Image 205",
            pageIndex: 99,
            pageSize: 100);

        Assert.Single(search.Rows);
        Assert.Equal("image-205", search.Rows[0].Key);
        Assert.Equal(1, search.PageNumber);
        Assert.Equal(1, search.PageCount);
    }

    [Fact]
    public void Projection_MapsIndependentDownloadActionsPerModel()
    {
        var downloadable = Model(
            "downloadable",
            "Downloadable",
            "image_model",
            SdkModelAvailability.Downloadable);
        var running = Model(
            "running",
            "Running",
            "image_model",
            SdkModelAvailability.Downloadable);
        var completed = Model(
            "completed",
            "Completed",
            "image_model",
            SdkModelAvailability.Downloadable);
        var download = new ModelDownloadState(
            "operation",
            SdkModelScopes.Local,
            running.RepositoryId!,
            running.Path,
            running.Compatibility,
            SdkModelDownloadStatuses.Progress,
            50,
            100,
            1,
            ["weights.bin"],
            2,
            null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
        var completedDownload = download with
        {
            OperationId = "completed-operation",
            RepositoryId = completed.RepositoryId!,
            Status = SdkModelDownloadStatuses.Completed,
            DownloadedBytes = 100,
            UpdatedAt = DateTimeOffset.UtcNow.AddMilliseconds(1)
        };

        var view = HdeModelListProjector.Project(
            Catalog(downloadable, running, completed),
            new ModelDownloadSnapshot(
                SdkModelScopes.Local,
                [download, completedDownload],
                DateTimeOffset.UtcNow),
            HdeModelFamily.Image,
            "",
            pageIndex: 0,
            pageSize: 100);

        var downloadRow = Assert.Single(view.Rows, row => row.Key == downloadable.Key);
        Assert.Equal("Download", downloadRow.ActionLabel);
        Assert.True(downloadRow.CanAct);

        var runningRow = Assert.Single(view.Rows, row => row.Key == running.Key);
        Assert.Equal("Cancel", runningRow.ActionLabel);
        Assert.True(runningRow.IsDownloading);
        Assert.Equal(0.5f, runningRow.Progress);

        var completedRow = Assert.Single(view.Rows, row => row.Key == completed.Key);
        Assert.Equal("Finalizing", completedRow.ActionLabel);
        Assert.False(completedRow.CanAct);
    }

    [Fact]
    public void Search_MatchesProviderRepositoryTypeAndTask()
    {
        var model = Model("searchable", "Plain name", "video_model") with
        {
            Provider = "provider-name",
            RepositoryId = "org/special-repository",
            SupportedTasks = ["text-to-video"]
        };
        var catalog = Catalog(model);

        foreach (var search in new[]
                 {
                     "provider-name",
                     "special-repository",
                     "video_model",
                     "text-to-video"
                 })
        {
            var view = HdeModelListProjector.Project(
                catalog,
                ModelDownloadSnapshot.Empty(SdkModelScopes.Local),
                HdeModelFamily.All,
                search,
                pageIndex: 0,
                pageSize: 100);
            Assert.Single(view.Rows);
        }
    }

    private static ModelCatalogSnapshot Catalog(params ModelDescriptor[] models)
        => new(
            "revision",
            SdkModelScopes.Local,
            models,
            DateTimeOffset.UtcNow,
            false,
            null);

    private static ModelDescriptor Model(
        string key,
        string name,
        string compatibility,
        string availability = SdkModelAvailability.ReadyLocal)
    {
        using var document = JsonDocument.Parse(
            $$"""{"type":"{{compatibility}}","id":"{{key}}","name":"{{name}}"}""");
        return new ModelDescriptor(
            key,
            name,
            compatibility,
            availability,
            false,
            SdkModelScopes.Local,
            "test-provider",
            key,
            $"test/{key}",
            null,
            Array.Empty<string>(),
            null,
            document.RootElement.Clone());
    }
}

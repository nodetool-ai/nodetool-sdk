using Nodetool.SDK.Assets;
using Nodetool.SDK.Workflows;

namespace Nodetool.SDK.Tests.Workflows;

public class WorkflowInputPreparerTests
{
    [Fact]
    public async Task RecursivelyPreparesMediaListsFromDescriptor()
    {
        var first = CreateTemporaryFile([1, 2]);
        var second = CreateTemporaryFile([3, 4]);
        try
        {
            var workflow = Descriptor(new WorkflowInputDescriptor(
                "input-node",
                "tracks",
                "",
                Type(
                    "list",
                    Type("audio")),
                Required: true,
                DefaultValue: null,
                Minimum: null,
                Maximum: null));
            var preparer = new WorkflowInputPreparer(
                new MediaInputPreparer(inlineLimitBytes: 100));

            var result = await preparer.PrepareAsync(
                workflow,
                new Dictionary<string, object?>
                {
                    ["tracks"] = new[] { first, second }
                });

            var tracks = Assert.IsType<object?[]>(result["tracks"]);
            Assert.Equal(2, tracks.Length);
            Assert.Equal(
                new byte[] { 1, 2 },
                Assert.IsType<Dictionary<string, object?>>(
                    tracks[0])["data"]);
            Assert.Equal(
                new byte[] { 3, 4 },
                Assert.IsType<Dictionary<string, object?>>(
                    tracks[1])["data"]);
        }
        finally
        {
            File.Delete(first);
            File.Delete(second);
        }
    }

    [Fact]
    public async Task HostAdapterConvertsEngineMediaBeforePortablePreparation()
    {
        var adapterCalls = 0;
        var workflow = Descriptor(new WorkflowInputDescriptor(
            "image-node",
            "image",
            "",
            Type("image"),
            Required: true,
            DefaultValue: null,
            Minimum: null,
            Maximum: null));
        var preparer = new WorkflowInputPreparer(
            new MediaInputPreparer(inlineLimitBytes: 100),
            (name, mediaType, value, _) =>
            {
                adapterCalls++;
                Assert.Equal("image", name);
                Assert.Equal("image", mediaType);
                Assert.Equal("engine-image", value);
                return ValueTask.FromResult<object?>(
                    new byte[] { 0x89, 0x50, 0x4E, 0x47 });
            });

        var result = await preparer.PrepareAsync(
            workflow,
            new Dictionary<string, object?>
            {
                ["image"] = "engine-image"
            });

        Assert.Equal(1, adapterCalls);
        Assert.Equal(
            new byte[] { 0x89, 0x50, 0x4E, 0x47 },
            Assert.IsType<Dictionary<string, object?>>(
                result["image"])["data"]);
    }

    [Fact]
    public async Task UnknownAndOrdinaryInputsUsePortableNormalization()
    {
        var workflow = Descriptor(new WorkflowInputDescriptor(
            "count-node",
            "count",
            "",
            Type("int"),
            Required: true,
            DefaultValue: null,
            Minimum: null,
            Maximum: null));
        var preparer = new WorkflowInputPreparer(
            new MediaInputPreparer());

        var result = await preparer.PrepareAsync(
            workflow,
            new Dictionary<string, object?>
            {
                ["count"] = 3,
                ["future_input"] = new[] { "a", "b" },
                ["nil"] = null
            });

        Assert.Equal(3, result["count"]);
        Assert.Equal(
            new object?[] { "a", "b" },
            Assert.IsType<object?[]>(result["future_input"]));
        Assert.Equal("", result["nil"]);
    }

    [Fact]
    public async Task ConnectionScopedService_PreparesInlineHostMediaWithoutHttp()
    {
        var workflow = Descriptor(new WorkflowInputDescriptor(
            "image-node",
            "image",
            "",
            Type("image"),
            Required: true,
            DefaultValue: null,
            Minimum: null,
            Maximum: null));
        var service = new WorkflowInputPreparationService(
            inlineMediaLimitBytes: 1024,
            adaptHostMediaValue: (_, mediaType, value, _) =>
            {
                Assert.Equal("image", mediaType);
                Assert.Equal("host-image", value);
                return ValueTask.FromResult<object?>(
                    new byte[] { 0x89, 0x50, 0x4E, 0x47 });
            });

        var result = await service.PrepareAsync(
            workflow,
            new Dictionary<string, object?>
            {
                ["image"] = "host-image"
            });

        var image = Assert.IsType<Dictionary<string, object?>>(
            result["image"]);
        Assert.Equal("image", image["type"]);
        Assert.Equal(
            new byte[] { 0x89, 0x50, 0x4E, 0x47 },
            image["data"]);
    }

    private static WorkflowDescriptor Descriptor(
        params WorkflowInputDescriptor[] inputs)
        => new(
            "workflow-1",
            "Workflow",
            "",
            "revision",
            null,
            null,
            1,
            null,
            "server",
            inputs,
            [],
            []);

    private static WorkflowTypeDescriptor Type(
        string type,
        params WorkflowTypeDescriptor[] typeArguments)
        => new(
            type,
            Optional: false,
            TypeName: null,
            Values: [],
            TypeArguments: typeArguments);

    private static string CreateTemporaryFile(byte[] bytes)
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"nodetool-workflow-input-{Guid.NewGuid():N}.wav");
        File.WriteAllBytes(path, bytes);
        return path;
    }
}

using Nodetool.SDK.Types;
using Nodetool.SDK.Assets;
using Nodetool.SDK.Types.Assets;
using Nodetool.SDK.Values;
using Nodetool.SDK.VL.Factories;
using Nodetool.SDK.VL.Nodes;
using Nodetool.SDK.VL.Utilities;
using Nodetool.SDK.VL.Models;
using Nodetool.SDK.Api.Models;
using Nodetool.SDK.VL.Services;
using Nodetool.SDK.Workflows;
using Nodetool.Types.Core;
using SkiaSharp;
using System.Text.Json;
using VL.Core;
using VL.Lib.Collections;
using Xunit;
using AssetAudioRef = Nodetool.SDK.Types.Assets.AudioRef;
using AssetDocumentRef = Nodetool.SDK.Types.Assets.DocumentRef;
using AssetVideoRef = Nodetool.SDK.Types.Assets.VideoRef;
using VlPath = VL.Lib.IO.Path;

namespace Nodetool.SDK.VL.UnitTests;

public class WorkflowVlTypeMappingTests
{
    [Fact]
    public void OptionConstrainedWorkflowString_UsesDynamicVlEnum()
    {
        using var defaultDocument = JsonDocument.Parse("\"words\"");
        var workflow = new WorkflowDetail(new WorkflowDescriptor(
            "workflow-1",
            "Workflow",
            "",
            "revision-1",
            1,
            "workflow",
            1,
            "etag-1",
            "server",
            [
                new WorkflowInputDescriptor(
                    "input-1",
                    "measure",
                    "",
                    new WorkflowTypeDescriptor(
                        "str",
                        false,
                        null,
                        ["characters", "words", "lines"],
                        []),
                    true,
                    defaultDocument.RootElement.Clone(),
                    null,
                    null)
            ],
            [],
            []));

        var input = Assert.Single(workflow.GetInputProperties());
        Assert.Equal("enum", input.Type.Type);

        var (type, _) =
            WorkflowVlTypeMapping.GetInputTypeAndDefault(input.Type);
        Assert.True(typeof(global::VL.Lib.Collections.IDynamicEnum).IsAssignableFrom(type));
    }

    [Fact]
    public void DynamicWorkflowEnum_PreservesNonIdentifierWireLiteral()
    {
        var metadata = new TypeMetadata
        {
            Type = "enum",
            Values = ["1:1", "16:9", "16-9"]
        };

        var (type, _) = WorkflowVlTypeMapping.GetInputTypeAndDefault(metadata);
        Assert.True(typeof(global::VL.Lib.Collections.IDynamicEnum).IsAssignableFrom(type));
    }

    [Fact]
    public void DynamicWorkflowEnum_OutputRestoresWireSelection()
    {
        var metadata = new TypeMetadata
        {
            Type = "enum",
            TypeName = "AspectRatio",
            Values = ["1:1", "16:9"]
        };
        var (type, _) = WorkflowVlTypeMapping.GetTypeAndDefault(metadata);

        var converted = WorkflowNodeBase.ConvertNodeToolValueToExpectedType(
            NodeToolValue.From("16:9"),
            type);
        var dynamicEnum = Assert.IsAssignableFrom<IDynamicEnum>(converted);

        Assert.Equal("16:9", dynamicEnum.Value);
        Assert.Equal("16:9", VlValueConversion.NormalizeForTransport(converted));
    }

    [Fact]
    public void DynamicWorkflowEnum_JsonDefaultRestoresWireSelection()
    {
        var metadata = new TypeMetadata
        {
            Type = "enum",
            TypeName = "Measure",
            Values = ["characters", "words", "lines"]
        };
        var (type, fallback) =
            WorkflowVlTypeMapping.GetInputTypeAndDefault(metadata);
        using var document = JsonDocument.Parse("\"words\"");

        var converted = VlValueConversion.ConvertOrFallback(
            document.RootElement.Clone(),
            type,
            fallback);

        Assert.Equal(
            "words",
            Assert.IsAssignableFrom<IDynamicEnum>(converted).Value);
    }

    [Fact]
    public void WorkflowTupleAndBytes_UseNativeVlTypes()
    {
        var tuple = new TypeMetadata
        {
            Type = "tuple",
            TypeArgs = [new TypeMetadata { Type = "int" }]
        };

        Assert.Equal(
            typeof(Spread<int>),
            WorkflowVlTypeMapping.GetTypeAndDefault(tuple).Type);
        Assert.Equal(
            typeof(byte[]),
            WorkflowVlTypeMapping.GetTypeAndDefault(
                new TypeMetadata { Type = "bytes" }).Type);
    }

    [Theory]
    [InlineData("file")]
    [InlineData("file_path")]
    public void WorkflowFileInput_UsesNativeVlPath(string typeName)
    {
        var (type, defaultValue) =
            WorkflowVlTypeMapping.GetInputTypeAndDefault(
                new TypeMetadata { Type = typeName });

        Assert.Equal(typeof(VlPath), type);
        Assert.IsType<VlPath>(defaultValue);
    }

    [Fact]
    public async System.Threading.Tasks.Task RepeatedSkImageAdaptation_ReusesEncodedBytes()
    {
        using var bitmap = new SKBitmap(4, 4);
        using var image = SKImage.FromBitmap(bitmap);

        var first = Assert.IsType<byte[]>(
            await VlMediaInputAdapter.AdaptValueAsync(
                "image",
                "image",
                image,
                CancellationToken.None));
        var second = Assert.IsType<byte[]>(
            await VlMediaInputAdapter.AdaptValueAsync(
                "image",
                "image",
                image,
                CancellationToken.None));

        Assert.Same(first, second);
        Assert.NotEmpty(first);
    }

    [Fact]
    public void StructuredTypeName_ResolvesGeneratedClrType()
    {
        var metadata = new TypeMetadata
        {
            Type = "object",
            TypeName = "CalendarEvent"
        };

        var (type, defaultValue) = WorkflowVlTypeMapping.GetTypeAndDefault(metadata);

        Assert.Equal(typeof(CalendarEvent), type);
        Assert.Null(defaultValue);
        Assert.False(WorkflowVlTypeMapping.UsesObjectFallback(metadata));
    }

    [Fact]
    public void StructuredDiscriminator_ResolvesGeneratedClrType()
    {
        var metadata = new TypeMetadata { Type = "calendar_event" };

        var (type, _) = WorkflowVlTypeMapping.GetTypeAndDefault(metadata);

        Assert.Equal(typeof(CalendarEvent), type);
    }

    [Fact]
    public void StructuredOutputMap_ConvertsToGeneratedClrType()
    {
        var raw = new Dictionary<string, object?>
        {
            ["id"] = "call-1",
            ["error"] = null,
            ["result"] = "ok",
            ["type"] = "tool_result"
        };

        var converted = VlValueConversion.ConvertOrFallback(raw, typeof(ToolResultEvent), null);
        var result = Assert.IsType<ToolResultEvent>(converted);

        Assert.Equal("call-1", result.id);
        Assert.Equal("tool_result", result.type?.ToString());
    }

    [Theory]
    [InlineData(3, typeof(int), 3)]
    [InlineData(0.5, typeof(float), 0.5f)]
    [InlineData(true, typeof(bool), true)]
    public void TerminalSingletonList_UnwrapsForScalarPins(
        object terminalValue,
        Type expectedType,
        object expected)
    {
        var value = NodeToolValue.From(new[] { terminalValue });

        var converted = WorkflowNodeBase.ConvertNodeToolValueToExpectedType(value, expectedType);

        Assert.Equal(expected, converted);
    }

    [Fact]
    public void TerminalList_BecomesAVlSpread()
    {
        var value = NodeToolValue.From(new object[] { "alpha", "beta" });
        var spreadType = typeof(Spread<string>);

        var converted = WorkflowNodeBase.ConvertNodeToolValueToExpectedType(value, spreadType);

        Assert.Equal(
            new[] { "alpha", "beta" },
            Assert.IsType<Spread<string>>(converted).ToArray());
    }

    [Fact]
    public void TerminalNestedListEnvelope_BecomesAFlatVlSpread()
    {
        var terminalValue = NodeToolValue.From(new object[]
        {
            new object[] { "1", "2", "2" }
        });
        var value = VlValueConversion.UnwrapTerminalResultEnvelope(terminalValue);

        var converted = WorkflowNodeBase.ConvertNodeToolValueToExpectedType(
            value,
            typeof(Spread<string>));

        Assert.Equal(
            new[] { "1", "2", "2" },
            Assert.IsType<Spread<string>>(converted).ToArray());
    }

    [Fact]
    public void TerminalEmptyListEnvelope_BecomesAnEmptyVlSpread()
    {
        var terminalValue = NodeToolValue.From(new object[]
        {
            Array.Empty<object>()
        });
        var value = VlValueConversion.UnwrapTerminalResultEnvelope(terminalValue);

        var converted = WorkflowNodeBase.ConvertNodeToolValueToExpectedType(
            value,
            typeof(Spread<string>));

        Assert.Empty(Assert.IsType<Spread<string>>(converted));
    }

    [Fact]
    public void WorkflowListType_UsesVlSpread()
    {
        var metadata = new TypeMetadata
        {
            Type = "list",
            TypeArgs = [new TypeMetadata { Type = "int" }]
        };

        var (type, defaultValue) = WorkflowVlTypeMapping.GetTypeAndDefault(metadata);

        Assert.Equal(typeof(Spread<int>), type);
        Assert.Empty(Assert.IsType<Spread<int>>(defaultValue));
    }

    [Fact]
    public void AudioWorkflowInput_UsesNativeVlPathWhileOutputStaysTyped()
    {
        var metadata = new TypeMetadata { Type = "audio" };

        var (inputType, inputDefault) =
            WorkflowVlTypeMapping.GetInputTypeAndDefault(metadata);
        var (outputType, outputDefault) =
            WorkflowVlTypeMapping.GetTypeAndDefault(metadata);

        Assert.Equal(typeof(VlPath), inputType);
        Assert.IsType<VlPath>(inputDefault);
        Assert.Equal(typeof(AssetAudioRef), outputType);
        Assert.IsType<AssetAudioRef>(outputDefault);
    }

    [Fact]
    public void AudioWorkflowInputList_UsesNativeVlPathSpread()
    {
        var metadata = new TypeMetadata
        {
            Type = "list",
            TypeArgs = [new TypeMetadata { Type = "audio" }]
        };

        var (type, defaultValue) =
            WorkflowVlTypeMapping.GetInputTypeAndDefault(metadata);

        Assert.Equal(typeof(Spread<VlPath>), type);
        Assert.Empty(Assert.IsType<Spread<VlPath>>(defaultValue));
    }

    [Fact]
    public void NativeVlPath_ConvertsFromDefaultsAndNormalizesForTransport()
    {
        const string rawPath = @"C:\media\sound.wav";

        var converted = Assert.IsType<VlPath>(
            VlValueConversion.ConvertOrFallback(
                rawPath,
                typeof(VlPath),
                new VlPath("")));

        Assert.Equal(rawPath, converted.ToString());
        Assert.Equal(rawPath, VlValueConversion.NormalizeForTransport(converted));
    }

    [Fact]
    public async System.Threading.Tasks.Task NativeVlAudioPath_BecomesAnExecutionAssetPayload()
    {
        var filePath = Path.Combine(
            Path.GetTempPath(),
            $"nodetool-sdk-audio-{Guid.NewGuid():N}.wav");
        var expectedBytes = new byte[] { 1, 2, 3, 4 };
        await File.WriteAllBytesAsync(filePath, expectedBytes);

        try
        {
            var converted = await new MediaInputPreparer().PrepareAsync(
                "audio",
                "audio",
                new VlPath(filePath),
                CancellationToken.None);
            var payload = Assert.IsType<Dictionary<string, object?>>(converted);

            Assert.Equal("audio", payload["type"]);
            Assert.Equal(new Uri(filePath).AbsoluteUri, payload["uri"]);
            Assert.Equal(expectedBytes, Assert.IsType<byte[]>(payload["data"]));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void AudioOutput_PreservesReferenceMetadata()
    {
        var value = NodeToolValue.From(new Dictionary<string, object?>
        {
            ["type"] = "audio",
            ["uri"] = "asset://track.wav",
            ["asset_id"] = "audio-1",
            ["temp_id"] = "upload-1",
            ["duration"] = 2.5,
            ["metadata"] = new Dictionary<string, object?>
            {
                ["content_type"] = "audio/wav",
                ["sample_rate"] = 48000
            }
        });

        var converted = WorkflowNodeBase.ConvertNodeToolValueToExpectedType(
            value,
            typeof(AssetAudioRef));
        var audio = Assert.IsType<AssetAudioRef>(converted);

        Assert.Equal("asset://track.wav", audio.Uri);
        Assert.Equal("audio-1", audio.AssetId);
        Assert.Equal("upload-1", audio.TempId);
        Assert.Equal(2.5f, audio.Duration);
        Assert.Equal("audio/wav", audio.Metadata?["content_type"]);
        Assert.Equal(48000, audio.Metadata?["sample_rate"]);
    }

    [Fact]
    public void VideoOutput_ConvertsByteListsAndSpecificFields()
    {
        var value = NodeToolValue.From(new Dictionary<string, object?>
        {
            ["type"] = "video",
            ["uri"] = "",
            ["data"] = new[] { 1, 2, 255 },
            ["duration"] = 3.25,
            ["format"] = "mp4"
        });

        var converted = WorkflowNodeBase.ConvertNodeToolValueToExpectedType(
            value,
            typeof(AssetVideoRef));
        var video = Assert.IsType<AssetVideoRef>(converted);

        Assert.Equal(new byte[] { 1, 2, 255 }, Assert.IsType<byte[]>(video.Data));
        Assert.Equal(3.25f, video.Duration);
        Assert.Equal("mp4", video.Format);
    }

    [Fact]
    public void TypedAssetInput_NormalizesToCurrentTransportShape()
    {
        var video = new AssetVideoRef
        {
            Uri = "asset://clip.mp4",
            AssetId = "video-1",
            TempId = "upload-2",
            Data = new byte[] { 7, 8 },
            Duration = 1.5f,
            Format = "mp4",
            Metadata = new Dictionary<string, object?>
            {
                ["content_type"] = "video/mp4"
            }
        };

        var transport = Assert.IsType<Dictionary<string, object>>(
            VlValueConversion.NormalizeForTransport(video));

        Assert.Equal("video", transport["type"]);
        Assert.Equal("asset://clip.mp4", transport["uri"]);
        Assert.Equal("video-1", transport["asset_id"]);
        Assert.Equal("upload-2", transport["temp_id"]);
        Assert.Equal(new byte[] { 7, 8 }, transport["data"]);
        Assert.Equal(1.5f, transport["duration"]);
        Assert.Equal("mp4", transport["format"]);
    }

    [Fact]
    public void TemporaryAssetId_IsAUsableReferenceAndDocumentIdFallback()
    {
        var document = new AssetDocumentRef { TempId = "pending-upload" };

        Assert.False(document.IsEmpty());
        Assert.Equal("pending-upload", document.DocumentId);
        Assert.Equal("pending-upload", document.ToDict()["temp_id"]);
    }

    [Fact]
    public void LatchedWorkflowOutputs_AreReappliedAfterVlResetsValueTypes()
    {
        var count = new WorkflowNodeBase.InternalPin("count", typeof(int), 0);
        var ratio = new WorkflowNodeBase.InternalPin("ratio", typeof(float), 0.0f);
        var enabled = new WorkflowNodeBase.InternalPin("enabled", typeof(bool), false);
        IReadOnlyDictionary<string, IVLPin> pins = new Dictionary<string, IVLPin>
        {
            ["count"] = count,
            ["ratio"] = ratio,
            ["enabled"] = enabled
        };
        IReadOnlyDictionary<string, object?> latched = new Dictionary<string, object?>
        {
            ["count"] = 3,
            ["ratio"] = 0.5f,
            ["enabled"] = true
        };

        // Reproduce VL's next-frame defaults after the event frame.
        count.Value = 0;
        ratio.Value = 0.0f;
        enabled.Value = false;

        WorkflowNodeBase.ReapplyLatchedOutputs(latched, pins);

        Assert.Equal(3, count.Value);
        Assert.Equal(0.5f, ratio.Value);
        Assert.Equal(true, enabled.Value);
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(0, 1, false)]
    [InlineData(1, 0, false)]
    [InlineData(75, 0, false)]
    public void EmptyWorkflowSnapshots_MustBeConfirmedBeforePublication(
        int fetchedWorkflowCount,
        int consecutiveEmptySnapshots,
        bool expected)
    {
        Assert.Equal(
            expected,
            WorkflowNodeFactory.ShouldRetainSnapshotForConfirmation(
                fetchedWorkflowCount,
                consecutiveEmptySnapshots));
    }

}

using Nodetool.SDK.Api.Models;
using Nodetool.SDK.Execution;
using Nodetool.SDK.Values;
using Nodetool.SDK.VL.Streaming;
using Nodetool.SDK.Workflows;
using VL.Core;
using Xunit;

namespace Nodetool.SDK.VL.UnitTests;

public sealed class NodeToolAudioSourceTests
{
    [Fact]
    public void GrabAudioFrame_ResamplesMonoAndDuplicatesChannels()
    {
        var source = new NodeToolAudioSource();
        Assert.True(
            source.TryPush(
                AudioUpdate(
                    [0f, 1f, 0f, -1f],
                    sampleRate: 24_000,
                    channels: 1,
                    done: true),
                out var error),
            error);

        var provider = source.GrabAudioFrame(
            6,
            new Optional<int>(48_000),
            new Optional<int>(2),
            new Optional<bool>(false));
        using var handle = provider.GetHandle();
        var frame = handle.Resource;
        var left = new float[6];
        var right = new float[6];
        frame.CopyChannelTo(0, left);
        frame.CopyChannelTo(1, right);

        Assert.Equal(48_000, frame.SampleRate);
        Assert.Equal(2, frame.ChannelCount);
        Assert.Equal(6, frame.SampleCount);
        Assert.Equal(
            [0f, 0.5f, 1f, 0.5f, 0f, -0.5f],
            left);
        Assert.Equal(left, right);
        Assert.True(source.IsCompleted);
    }

    [Fact]
    public void GrabAudioFrame_FillsUnderrunWithSilence()
    {
        var source = new NodeToolAudioSource();
        Assert.True(
            source.TryPush(
                AudioUpdate(
                    [0.25f, -0.25f],
                    sampleRate: 48_000,
                    channels: 1),
                out _));

        var provider = source.GrabAudioFrame(
            4,
            new Optional<int>(48_000),
            new Optional<int>(1),
            new Optional<bool>(false));
        using var handle = provider.GetHandle();
        var samples = new float[4];
        handle.Resource.CopyChannelTo(0, samples);

        Assert.Equal([0.25f, -0.25f, 0f, 0f], samples);
        Assert.Equal(1, source.UnderrunCount);
    }

    [Fact]
    public void GrabAudioFrame_SteadyStateDoesNotAllocate()
    {
        var source = new NodeToolAudioSource();
        Assert.True(
            source.TryPush(
                AudioUpdate(
                    Enumerable.Repeat(0.25f, 4096).ToArray(),
                    sampleRate: 48_000,
                    channels: 1),
                out _));

        _ = source.GrabAudioFrame(
            64,
            new Optional<int>(48_000),
            new Optional<int>(1),
            new Optional<bool>(false));

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 32; index++)
        {
            _ = source.GrabAudioFrame(
                64,
                new Optional<int>(48_000),
                new Optional<int>(1),
                new Optional<bool>(false));
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
    }

    [Fact]
    public void TryPush_ReturnsExtremeFormatErrors()
    {
        var source = new NodeToolAudioSource();

        var accepted = source.TryPush(
            AudioUpdate(
                [0f],
                sampleRate: int.MaxValue,
                channels: 1),
            out var error);

        Assert.False(accepted);
        Assert.False(string.IsNullOrWhiteSpace(error));
        Assert.Equal(error, source.LastError);
    }

    [Fact]
    public void WorkflowAudioSourcePins_IncludeOnlyDeclaredAudioStreams()
    {
        var descriptor = new WorkflowDescriptor(
            "workflow-1",
            "Realtime",
            "",
            "revision-1",
            1,
            "workflow",
            1,
            "etag-1",
            "server",
            [],
            [
                Output(
                    "audio-out",
                    "Audio",
                    "chunk",
                    stream: true,
                    streamKind: "audio"),
                Output(
                    "text-out",
                    "Text",
                    "chunk",
                    stream: true,
                    streamKind: "text"),
                Output(
                    "unspecified-out",
                    "Unspecified",
                    "chunk",
                    stream: true),
                Output(
                    "collision-out",
                    "Audio Audio Source",
                    "str",
                    stream: false)
            ],
            []);

        var pin = Assert.Single(
            WorkflowAudioSourcePins.Create(
                descriptor,
                descriptor.Outputs.Select(output => output.Name)));

        Assert.Equal("audio-out", pin.Output.NodeId);
        Assert.Equal("Audio Audio Source 2", pin.PinName);
    }

    [Fact]
    public void StreamingAgentAudio_GetsRealtimeSourcePin()
    {
        var metadata = new NodeMetadataResponse
        {
            NodeType = "nodetool.agents.Agent",
            IsStreamingOutput = true,
            Outputs =
            [
                new NodeOutput
                {
                    Name = "text",
                    Type = new NodeTypeDefinition { Type = "str" }
                },
                new NodeOutput
                {
                    Name = "audio",
                    Type = new NodeTypeDefinition { Type = "audio" }
                }
            ]
        };

        var pin = Assert.Single(NodeAudioSourcePins.Create(
            metadata,
            metadata.Outputs.Select(output => output.Name)));

        Assert.Equal("audio", pin.Output.Name);
        Assert.Equal("Audio Source", pin.PinName);
    }

    private static WorkflowOutputDescriptor Output(
        string nodeId,
        string name,
        string type,
        bool stream,
        string? streamKind = null)
        => new(
            nodeId,
            name,
            "",
            new WorkflowTypeDescriptor(
                type,
                false,
                null,
                [],
                []),
            stream,
            streamKind);

    private static ExecutionStreamUpdate AudioUpdate(
        float[] samples,
        int sampleRate,
        int channels,
        bool done = false)
    {
        var bytes = new byte[samples.Length * sizeof(float)];
        Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);
        return new ExecutionStreamUpdate(
            JobId: "job-1",
            WorkflowId: "workflow-1",
            ThreadId: null,
            NodeId: "audio-out",
            NodeName: "audio",
            OutputName: "Audio",
            OutputType: "chunk",
            ContentType: "audio",
            Content: NodeToolValue.From(Convert.ToBase64String(bytes)),
            ContentMetadata: new Dictionary<string, NodeToolValue>
            {
                ["encoding"] = NodeToolValue.From("f32le"),
                ["sample_rate"] = NodeToolValue.From(sampleRate),
                ["channels"] = NodeToolValue.From(channels)
            },
            Disposition: "append",
            Done: done,
            Thinking: false,
            ReceivedAt: DateTimeOffset.UtcNow,
            Source: ExecutionStreamSource.OutputUpdate);
    }
}

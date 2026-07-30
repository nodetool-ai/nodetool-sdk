using System.Buffers.Binary;
using Nodetool.SDK.Execution;
using Nodetool.SDK.Streaming;
using Nodetool.SDK.Values;

namespace Nodetool.SDK.Tests.Execution;

public sealed class AudioStreamChunkTests
{
    [Fact]
    public void Float32Audio_ValidatesAndDecodes()
    {
        var bytes = new byte[sizeof(float) * 4];
        WriteFloat(bytes, 0, -1f);
        WriteFloat(bytes, 1, -0.25f);
        WriteFloat(bytes, 2, 0.5f);
        WriteFloat(bytes, 3, 1.5f);
        var update = AudioUpdate(
            Convert.ToBase64String(bytes),
            "f32le",
            sampleRate: 48_000,
            channels: 2);

        var parsed = AudioStreamChunk.TryCreate(
            update,
            out var chunk,
            out var error);

        Assert.True(parsed, error);
        Assert.NotNull(chunk);
        Assert.Equal(AudioStreamEncoding.Float32LittleEndian, chunk.Encoding);
        Assert.Equal(2, chunk.FrameCount);
        Assert.Equal(4, chunk.SampleCount);
        Assert.Equal(
            [-1f, -0.25f, 0.5f, 1.5f],
            chunk.DecodeInterleavedSamples());
    }

    [Theory]
    [InlineData("pcm16le")]
    [InlineData("pcm")]
    public void Pcm16Audio_DecodesToNormalizedSamples(string encoding)
    {
        var bytes = new byte[sizeof(short) * 3];
        BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(0, 2), short.MinValue);
        BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(2, 2), 0);
        BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(4, 2), short.MaxValue);
        var update = AudioUpdate(
            Convert.ToBase64String(bytes),
            encoding,
            sampleRate: 24_000,
            channels: 1);

        Assert.True(AudioStreamChunk.TryCreate(
            update,
            out var chunk,
            out var error), error);

        Assert.Equal(
            [-1f, 0f, short.MaxValue / 32768f],
            chunk!.DecodeInterleavedSamples());
    }

    [Theory]
    [InlineData("invalid-base64", "f32le", 24000, 1)]
    [InlineData("AAAA", "unknown", 24000, 1)]
    [InlineData("AAAA", "f32le", 0, 1)]
    [InlineData("AAAA", "f32le", 24000, 0)]
    public void MalformedAudio_IsRejected(
        string content,
        string encoding,
        int sampleRate,
        int channels)
    {
        var update = AudioUpdate(
            content,
            encoding,
            sampleRate,
            channels);

        Assert.False(AudioStreamChunk.TryCreate(
            update,
            out var chunk,
            out var error));
        Assert.Null(chunk);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void DoneMarker_AllowsEmptyAlignedPayload()
    {
        var update = AudioUpdate(
            "",
            "f32le",
            sampleRate: 24_000,
            channels: 1,
            done: true);

        Assert.True(AudioStreamChunk.TryCreate(
            update,
            out var chunk,
            out var error), error);
        Assert.True(chunk!.Done);
        Assert.Equal(0, chunk.FrameCount);
    }

    private static ExecutionStreamUpdate AudioUpdate(
        object content,
        string encoding,
        int sampleRate,
        int channels,
        bool done = false)
        => new(
            JobId: "job-1",
            WorkflowId: "workflow-1",
            ThreadId: null,
            NodeId: "audio-output",
            NodeName: "Audio Output",
            OutputName: "chunk",
            OutputType: "chunk",
            ContentType: "audio",
            Content: NodeToolValue.From(content),
            ContentMetadata: new Dictionary<string, NodeToolValue>
            {
                ["encoding"] = NodeToolValue.From(encoding),
                ["sample_rate"] = NodeToolValue.From(sampleRate),
                ["channels"] = NodeToolValue.From(channels)
            },
            Disposition: "append",
            Done: done,
            Thinking: false,
            ReceivedAt: DateTimeOffset.UtcNow,
            Source: ExecutionStreamSource.OutputUpdate);

    private static void WriteFloat(byte[] bytes, int index, float value)
        => BinaryPrimitives.WriteInt32LittleEndian(
            bytes.AsSpan(index * sizeof(float), sizeof(float)),
            BitConverter.SingleToInt32Bits(value));
}

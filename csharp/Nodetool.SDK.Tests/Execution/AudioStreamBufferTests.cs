using System.Buffers.Binary;
using Nodetool.SDK.Execution;
using Nodetool.SDK.Streaming;
using Nodetool.SDK.Values;

namespace Nodetool.SDK.Tests.Execution;

public sealed class AudioStreamBufferTests
{
    [Fact]
    public void DropOldest_KeepsMostRecentFrames()
    {
        var buffer = new AudioStreamBuffer(
            sampleRate: 24_000,
            channels: 1,
            capacityFrames: 3);

        buffer.Write(AudioChunk([1f, 2f]));
        var result = buffer.Write(AudioChunk([3f, 4f, 5f]));
        Span<float> destination = stackalloc float[3];
        var frames = buffer.Read(destination);

        Assert.Equal(3, frames);
        Assert.Equal(2, result.DroppedFrames);
        Assert.Equal([3f, 4f, 5f], destination.ToArray());
        Assert.Equal(2, buffer.DroppedFrames);
    }

    [Fact]
    public void DropNewest_PreservesAlreadyBufferedFrames()
    {
        var buffer = new AudioStreamBuffer(
            sampleRate: 24_000,
            channels: 1,
            capacityFrames: 3,
            AudioStreamOverflowPolicy.DropNewest);

        buffer.Write(AudioChunk([1f, 2f]));
        var result = buffer.Write(AudioChunk([3f, 4f, 5f]));
        Span<float> destination = stackalloc float[3];
        buffer.Read(destination);

        Assert.Equal(1, result.WrittenFrames);
        Assert.Equal(2, result.DroppedFrames);
        Assert.Equal([1f, 2f, 3f], destination.ToArray());
    }

    [Fact]
    public void CompletionAndUnderrun_AreExplicit()
    {
        var buffer = new AudioStreamBuffer(
            sampleRate: 24_000,
            channels: 2,
            capacityFrames: 4);
        buffer.Write(AudioChunk([1f, -1f], channels: 2, done: true));
        Span<float> first = stackalloc float[4];
        first.Clear();

        Assert.True(buffer.IsCompleted);
        Assert.Equal(1, buffer.Read(first));
        Assert.Equal([1f, -1f, 0f, 0f], first.ToArray());
        Assert.Equal(0, buffer.Read(first));
        Assert.Throws<InvalidOperationException>(
            () => buffer.Write(AudioChunk([0f, 0f], channels: 2)));

        buffer.Reset();
        Assert.False(buffer.IsCompleted);
        Assert.Equal(0, buffer.BufferedFrames);
    }

    [Fact]
    public void FormatChanges_AreRejected()
    {
        var buffer = new AudioStreamBuffer(
            sampleRate: 24_000,
            channels: 1,
            capacityFrames: 4);

        Assert.Throws<InvalidOperationException>(
            () => buffer.Write(AudioChunk([0f], sampleRate: 48_000)));
        Assert.Throws<InvalidOperationException>(
            () => buffer.Write(AudioChunk([0f, 0f], channels: 2)));
    }

    [Fact]
    public void PlaybackBuffer_ResamplesAndMapsPlanarChannels()
    {
        var buffer = new AudioStreamPlaybackBuffer(
            sampleRate: 24_000,
            channels: 1,
            capacityFrames: 16);
        buffer.Write(AudioChunk([0f, 1f, 0f, -1f], done: true));
        Span<float> planar = stackalloc float[12];
        planar.Clear();

        var frames = buffer.Read(
            planar,
            requestedFrames: 6,
            requestedSampleRate: 48_000,
            requestedChannels: 2,
            interleaved: false);

        Assert.Equal(6, frames);
        Assert.Equal(
            [0f, 0.5f, 1f, 0.5f, 0f, -0.5f],
            planar[..6].ToArray());
        Assert.Equal(planar[..6].ToArray(), planar[6..].ToArray());
        Assert.True(buffer.IsCompleted);
    }

    [Fact]
    public void PlaybackBuffer_ReadIsAllocationFreeAndDropsNewest()
    {
        var buffer = new AudioStreamPlaybackBuffer(
            sampleRate: 48_000,
            channels: 1,
            capacityFrames: 128);
        var write = buffer.Write(
            AudioChunk(
                Enumerable.Repeat(0.25f, 256).ToArray(),
                sampleRate: 48_000));
        var destination = new float[32];

        _ = buffer.Read(
            destination,
            requestedFrames: 32,
            requestedSampleRate: 48_000,
            requestedChannels: 1,
            interleaved: true);
        var before = GC.GetAllocatedBytesForCurrentThread();
        _ = buffer.Read(
            destination,
            requestedFrames: 32,
            requestedSampleRate: 48_000,
            requestedChannels: 1,
            interleaved: true);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(128, write.WrittenFrames);
        Assert.Equal(128, write.DroppedFrames);
        Assert.Equal(0, allocated);
    }

    private static AudioStreamChunk AudioChunk(
        float[] samples,
        int sampleRate = 24_000,
        int channels = 1,
        bool done = false)
    {
        var bytes = new byte[samples.Length * sizeof(float)];
        for (var index = 0; index < samples.Length; index++)
        {
            BinaryPrimitives.WriteInt32LittleEndian(
                bytes.AsSpan(index * sizeof(float), sizeof(float)),
                BitConverter.SingleToInt32Bits(samples[index]));
        }
        var update = new ExecutionStreamUpdate(
            JobId: "job-1",
            WorkflowId: "workflow-1",
            ThreadId: null,
            NodeId: "audio-output",
            NodeName: "Audio Output",
            OutputName: "chunk",
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
        Assert.True(AudioStreamChunk.TryCreate(
            update,
            out var chunk,
            out var error), error);
        return chunk!;
    }
}

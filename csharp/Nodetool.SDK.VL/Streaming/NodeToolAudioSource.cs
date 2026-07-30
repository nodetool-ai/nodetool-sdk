using System.Threading;
using CommunityToolkit.HighPerformance;
using Nodetool.SDK.Execution;
using Nodetool.SDK.Streaming;
using VL.Core;
using VL.Lib.Basics.Audio;
using VL.Lib.Basics.Resources;

namespace Nodetool.SDK.VL.Streaming;

/// <summary>
/// Bridges one NodeTool realtime audio stream to vvvv's host-neutral
/// <see cref="IAudioSource"/> contract.
/// </summary>
/// <remarks>
/// Network callbacks are the single producer and the VL.Audio callback is the
/// single consumer. The audio callback never waits for the producer and reuses
/// its frame storage after the requested format has stabilized.
/// </remarks>
public sealed class NodeToolAudioSource : IAudioSource
{
    private readonly int _capacitySeconds;
    private AudioStreamPlaybackBuffer? _state;
    private AudioFrameCache? _frameCache;
    private long _underrunCount;
    private long _formatChangeCount;
    private string? _lastError;

    public NodeToolAudioSource(int capacitySeconds = 4)
    {
        if (capacitySeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacitySeconds));

        _capacitySeconds = capacitySeconds;
    }

    public int SampleRate => Volatile.Read(ref _state)?.SampleRate ?? 0;

    public int Channels => Volatile.Read(ref _state)?.Channels ?? 0;

    public int BufferedFrames => Volatile.Read(ref _state)?.BufferedFrames ?? 0;

    public long DroppedFrames => Volatile.Read(ref _state)?.DroppedFrames ?? 0;

    public long UnderrunCount => Interlocked.Read(ref _underrunCount);

    public long FormatChangeCount => Interlocked.Read(ref _formatChangeCount);

    public bool IsCompleted => Volatile.Read(ref _state)?.IsCompleted ?? false;

    public string? LastError => Volatile.Read(ref _lastError);

    /// <summary>
    /// Parses and queues one normalized NodeTool audio update.
    /// </summary>
    public bool TryPush(
        ExecutionStreamUpdate update,
        out string? error)
    {
        if (!AudioStreamChunk.TryCreate(update, out var chunk, out error))
        {
            Volatile.Write(ref _lastError, error);
            return false;
        }

        try
        {
            var audioChunk = chunk!;
            var current = Volatile.Read(ref _state);
            if (current == null ||
                current.SampleRate != audioChunk.SampleRate ||
                current.Channels != audioChunk.Channels ||
                string.Equals(
                    update.Disposition,
                    "replace",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (current != null)
                    Interlocked.Increment(ref _formatChangeCount);

                current = new AudioStreamPlaybackBuffer(
                    audioChunk.SampleRate,
                    audioChunk.Channels,
                    checked(audioChunk.SampleRate * _capacitySeconds));
                Volatile.Write(ref _state, current);
            }

            current.Write(audioChunk);
            Volatile.Write(ref _lastError, null);
            error = null;
            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            InvalidOperationException or
            OverflowException)
        {
            error = exception.Message;
            Volatile.Write(ref _lastError, error);
            return false;
        }
    }

    /// <summary>
    /// Drops buffered audio and waits for the next stream update to establish
    /// the source format.
    /// </summary>
    public void Reset()
    {
        Volatile.Write(ref _state, null);
        Volatile.Write(ref _lastError, null);
    }

    public IResourceProvider<AudioFrame> GrabAudioFrame(
        int sampleCount,
        Optional<int> sampleRate,
        Optional<int> channelCount,
        Optional<bool> interleaved)
    {
        var state = Volatile.Read(ref _state);
        if (state == null || sampleCount <= 0)
            return null!;

        var requestedSampleRate =
            sampleRate.HasValue && sampleRate.Value > 0
                ? sampleRate.Value
                : state.SampleRate;
        var requestedChannelCount =
            channelCount.HasValue && channelCount.Value > 0
                ? channelCount.Value
                : state.Channels;
        var requestedInterleaved =
            interleaved.HasValue && interleaved.Value;

        var cache = _frameCache;
        if (cache == null ||
            !cache.Matches(
                sampleCount,
                requestedSampleRate,
                requestedChannelCount,
                requestedInterleaved))
        {
            cache = new AudioFrameCache(
                sampleCount,
                requestedSampleRate,
                requestedChannelCount,
                requestedInterleaved);
            _frameCache = cache;
        }

        cache.Samples.AsSpan().Clear();
        var written = state.Read(
            cache.Samples.AsSpan(),
            sampleCount,
            requestedSampleRate,
            requestedChannelCount,
            requestedInterleaved);
        if (written < sampleCount)
            Interlocked.Increment(ref _underrunCount);

        return cache.Provider;
    }

    private sealed class AudioFrameCache
    {
        public AudioFrameCache(
            int sampleCount,
            int sampleRate,
            int channels,
            bool interleaved)
        {
            SampleCount = sampleCount;
            SampleRate = sampleRate;
            Channels = channels;
            Interleaved = interleaved;
            Samples = new float[checked(sampleCount * channels)];

            var data = interleaved
                ? new ReadOnlyMemory2D<float>(
                    Samples,
                    sampleCount,
                    channels)
                : new ReadOnlyMemory2D<float>(
                    Samples,
                    channels,
                    sampleCount);
            var frame = new AudioFrame(
                data,
                sampleRate,
                interleaved,
                "NodeTool realtime audio",
                TimeSpan.Zero);
            Provider = ResourceProvider.Return(frame);
        }

        public int SampleCount { get; }

        public int SampleRate { get; }

        public int Channels { get; }

        public bool Interleaved { get; }

        public float[] Samples { get; }

        public IResourceProvider<AudioFrame> Provider { get; }

        public bool Matches(
            int sampleCount,
            int sampleRate,
            int channels,
            bool interleaved)
            => SampleCount == sampleCount &&
               SampleRate == sampleRate &&
               Channels == channels &&
               Interleaved == interleaved;
    }

}

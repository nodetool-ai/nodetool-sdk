namespace Nodetool.SDK.Streaming;

/// <summary>
/// Policy used when incoming realtime audio exceeds the fixed buffer capacity.
/// </summary>
public enum AudioStreamOverflowPolicy
{
    DropOldest,
    DropNewest
}

/// <summary>
/// Result of writing one audio block into an <see cref="AudioStreamBuffer"/>.
/// </summary>
public readonly record struct AudioStreamWriteResult(
    int WrittenFrames,
    int DroppedFrames);

/// <summary>
/// Thread-safe, allocation-free-on-read ring buffer for interleaved realtime
/// audio samples.
/// </summary>
public sealed class AudioStreamBuffer
{
    private readonly object _gate = new();
    private readonly float[] _samples;
    private int _readIndex;
    private int _writeIndex;
    private int _count;
    private long _droppedFrames;
    private bool _completed;

    public AudioStreamBuffer(
        int sampleRate,
        int channels,
        int capacityFrames,
        AudioStreamOverflowPolicy overflowPolicy =
            AudioStreamOverflowPolicy.DropOldest)
    {
        if (sampleRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(sampleRate));
        if (channels <= 0)
            throw new ArgumentOutOfRangeException(nameof(channels));
        if (capacityFrames <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacityFrames));

        SampleRate = sampleRate;
        Channels = channels;
        CapacityFrames = capacityFrames;
        OverflowPolicy = overflowPolicy;
        _samples = new float[checked(capacityFrames * channels)];
    }

    public int SampleRate { get; }

    public int Channels { get; }

    public int CapacityFrames { get; }

    public AudioStreamOverflowPolicy OverflowPolicy { get; }

    public int BufferedFrames
    {
        get
        {
            lock (_gate)
                return _count / Channels;
        }
    }

    public long DroppedFrames
    {
        get
        {
            lock (_gate)
                return _droppedFrames;
        }
    }

    public bool IsCompleted
    {
        get
        {
            lock (_gate)
                return _completed;
        }
    }

    /// <summary>
    /// Writes a validated block. Format changes are rejected so hosts can
    /// rebuild their audio adapter explicitly.
    /// </summary>
    public AudioStreamWriteResult Write(AudioStreamChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        if (chunk.SampleRate != SampleRate || chunk.Channels != Channels)
        {
            throw new InvalidOperationException(
                "Audio stream format changed while the buffer was active.");
        }

        lock (_gate)
        {
            if (_completed)
            {
                throw new InvalidOperationException(
                    "Cannot write after the audio stream completed.");
            }

            var incomingFrames = chunk.FrameCount;
            var sourceFrame = 0;
            var framesToWrite = incomingFrames;
            var dropped = 0;
            var bufferedFrames = _count / Channels;

            if (OverflowPolicy == AudioStreamOverflowPolicy.DropOldest)
            {
                if (framesToWrite > CapacityFrames)
                {
                    sourceFrame = framesToWrite - CapacityFrames;
                    dropped += sourceFrame;
                    framesToWrite = CapacityFrames;
                }

                var framesToDiscard = Math.Max(
                    0,
                    bufferedFrames + framesToWrite - CapacityFrames);
                DiscardFrames(framesToDiscard);
                dropped += framesToDiscard;
            }
            else
            {
                var availableFrames = CapacityFrames - bufferedFrames;
                if (framesToWrite > availableFrames)
                {
                    dropped = framesToWrite - availableFrames;
                    framesToWrite = availableFrames;
                }
            }

            var firstSample = sourceFrame * Channels;
            var samplesToWrite = framesToWrite * Channels;
            for (var offset = 0; offset < samplesToWrite; offset++)
            {
                _samples[_writeIndex] =
                    chunk.ReadSample(firstSample + offset);
                _writeIndex = (_writeIndex + 1) % _samples.Length;
            }
            _count += samplesToWrite;
            _droppedFrames += dropped;
            if (chunk.Done)
                _completed = true;

            return new AudioStreamWriteResult(framesToWrite, dropped);
        }
    }

    /// <summary>
    /// Reads complete interleaved frames into caller-owned memory. The return
    /// value is the number of frames copied; an empty read is an underrun, not
    /// an allocation or an exception.
    /// </summary>
    public int Read(Span<float> interleavedDestination)
    {
        if (interleavedDestination.Length % Channels != 0)
        {
            throw new ArgumentException(
                "The destination length must contain complete audio frames.",
                nameof(interleavedDestination));
        }

        lock (_gate)
        {
            var frames = Math.Min(
                interleavedDestination.Length / Channels,
                _count / Channels);
            var samplesToRead = frames * Channels;
            for (var offset = 0; offset < samplesToRead; offset++)
            {
                interleavedDestination[offset] = _samples[_readIndex];
                _readIndex = (_readIndex + 1) % _samples.Length;
            }
            _count -= samplesToRead;
            return frames;
        }
    }

    /// <summary>
    /// Clears buffered samples and completion state for an explicit restart.
    /// </summary>
    public void Reset()
    {
        lock (_gate)
        {
            _readIndex = 0;
            _writeIndex = 0;
            _count = 0;
            _droppedFrames = 0;
            _completed = false;
        }
    }

    private void DiscardFrames(int frames)
    {
        if (frames <= 0)
            return;
        var samples = frames * Channels;
        _readIndex = (_readIndex + samples) % _samples.Length;
        _count -= samples;
    }
}

using System.Threading;

namespace Nodetool.SDK.Streaming;

/// <summary>
/// Fixed-capacity, single-producer/single-consumer buffer for realtime
/// playback. Reads can resample and map channels into caller-owned memory
/// without locking or allocating.
/// </summary>
/// <remarks>
/// The producer drops newest frames when the buffer is full. One execution
/// transport callback must own <see cref="Write"/> and one host audio callback
/// must own <see cref="Read"/>.
/// </remarks>
public sealed class AudioStreamPlaybackBuffer
{
    private readonly float[] _samples;
    private long _writeFrame;
    private long _consumedFrame;
    private double _readPosition;
    private long _droppedFrames;
    private int _completed;

    public AudioStreamPlaybackBuffer(
        int sampleRate,
        int channels,
        int capacityFrames)
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
        _samples = new float[checked(capacityFrames * channels)];
    }

    public int SampleRate { get; }

    public int Channels { get; }

    public int CapacityFrames { get; }

    public int BufferedFrames
    {
        get
        {
            var written = Volatile.Read(ref _writeFrame);
            var consumed = Volatile.Read(ref _consumedFrame);
            return (int)Math.Clamp(
                written - consumed,
                0,
                CapacityFrames);
        }
    }

    public long DroppedFrames => Interlocked.Read(ref _droppedFrames);

    public bool IsCompleted => Volatile.Read(ref _completed) != 0;

    /// <summary>
    /// Decodes and queues one block. Decoding allocates on the producer thread;
    /// the host audio callback remains allocation-free.
    /// </summary>
    public AudioStreamWriteResult Write(AudioStreamChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        if (chunk.SampleRate != SampleRate || chunk.Channels != Channels)
        {
            throw new InvalidOperationException(
                "Audio stream format changed while the playback buffer was active.");
        }

        var decoded = chunk.DecodeInterleavedSamples();
        var readFrame = Volatile.Read(ref _consumedFrame);
        var writeFrame = Volatile.Read(ref _writeFrame);
        var bufferedFrames = Math.Clamp(
            writeFrame - readFrame,
            0,
            CapacityFrames);
        var availableFrames = Math.Max(
            0,
            CapacityFrames - (int)bufferedFrames);
        var framesToWrite = Math.Min(
            chunk.FrameCount,
            availableFrames);

        for (var frameOffset = 0;
             frameOffset < framesToWrite;
             frameOffset++)
        {
            var targetFrame =
                (int)((writeFrame + frameOffset) % CapacityFrames);
            var sourceSample = frameOffset * Channels;
            var targetSample = targetFrame * Channels;
            decoded.AsSpan(sourceSample, Channels)
                .CopyTo(_samples.AsSpan(targetSample, Channels));
        }

        Volatile.Write(
            ref _writeFrame,
            writeFrame + framesToWrite);
        var dropped = chunk.FrameCount - framesToWrite;
        if (dropped > 0)
            Interlocked.Add(ref _droppedFrames, dropped);
        if (chunk.Done)
            Volatile.Write(ref _completed, 1);

        return new AudioStreamWriteResult(framesToWrite, dropped);
    }

    /// <summary>
    /// Reads into interleaved or planar caller-owned storage. Missing frames
    /// are left unchanged so callers can clear once and retain silence for an
    /// underrun.
    /// </summary>
    /// <returns>The number of output frames written.</returns>
    public int Read(
        Span<float> destination,
        int requestedFrames,
        int requestedSampleRate,
        int requestedChannels,
        bool interleaved)
    {
        if (requestedFrames < 0)
            throw new ArgumentOutOfRangeException(nameof(requestedFrames));
        if (requestedSampleRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(requestedSampleRate));
        if (requestedChannels <= 0)
            throw new ArgumentOutOfRangeException(nameof(requestedChannels));
        if (destination.Length <
            checked(requestedFrames * requestedChannels))
        {
            throw new ArgumentException(
                "The destination is too small for the requested audio frame.",
                nameof(destination));
        }

        var writeFrame = Volatile.Read(ref _writeFrame);
        var sourceStep = SampleRate / (double)requestedSampleRate;
        var framesWritten = 0;

        for (var outputFrame = 0;
             outputFrame < requestedFrames;
             outputFrame++)
        {
            var sourceFrame = (long)Math.Floor(_readPosition);
            var fraction = _readPosition - sourceFrame;
            if (sourceFrame >= writeFrame)
                break;

            var nextFrame = sourceFrame + 1;
            if (fraction > 0 && nextFrame >= writeFrame)
                break;

            for (var outputChannel = 0;
                 outputChannel < requestedChannels;
                 outputChannel++)
            {
                var sample = ReadMappedSample(
                    sourceFrame,
                    nextFrame,
                    fraction,
                    outputChannel,
                    requestedChannels);
                var targetIndex = interleaved
                    ? outputFrame * requestedChannels + outputChannel
                    : outputChannel * requestedFrames + outputFrame;
                destination[targetIndex] = sample;
            }

            _readPosition += sourceStep;
            framesWritten++;
        }

        Volatile.Write(
            ref _consumedFrame,
            (long)Math.Floor(_readPosition));
        return framesWritten;
    }

    private float ReadMappedSample(
        long firstFrame,
        long secondFrame,
        double fraction,
        int outputChannel,
        int outputChannels)
    {
        if (outputChannels == 1 && Channels > 1)
        {
            var mixed = 0f;
            for (var sourceChannel = 0;
                 sourceChannel < Channels;
                 sourceChannel++)
            {
                mixed += Interpolate(
                    firstFrame,
                    secondFrame,
                    fraction,
                    sourceChannel);
            }
            return mixed / Channels;
        }

        var mappedChannel = Channels == 1
            ? 0
            : Math.Min(outputChannel, Channels - 1);
        return Interpolate(
            firstFrame,
            secondFrame,
            fraction,
            mappedChannel);
    }

    private float Interpolate(
        long firstFrame,
        long secondFrame,
        double fraction,
        int channel)
    {
        var first = ReadSample(firstFrame, channel);
        if (fraction <= 0)
            return first;
        var second = ReadSample(secondFrame, channel);
        return first + (second - first) * (float)fraction;
    }

    private float ReadSample(long frame, int channel)
    {
        var wrappedFrame = (int)(frame % CapacityFrames);
        return _samples[wrappedFrame * Channels + channel];
    }
}

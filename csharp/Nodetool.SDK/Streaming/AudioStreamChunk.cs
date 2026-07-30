using System.Buffers.Binary;
using Nodetool.SDK.Execution;
using Nodetool.SDK.Values;

namespace Nodetool.SDK.Streaming;

/// <summary>
/// Sample encoding used by NodeTool realtime audio chunks.
/// </summary>
public enum AudioStreamEncoding
{
    Pcm16LittleEndian,
    Float32LittleEndian
}

/// <summary>
/// Validated, host-neutral realtime audio block.
/// </summary>
public sealed class AudioStreamChunk
{
    private AudioStreamChunk(
        ReadOnlyMemory<byte> payload,
        AudioStreamEncoding encoding,
        int sampleRate,
        int channels,
        bool done,
        TimeSpan duration)
    {
        Payload = payload;
        Encoding = encoding;
        SampleRate = sampleRate;
        Channels = channels;
        Done = done;
        Duration = duration;
    }

    public ReadOnlyMemory<byte> Payload { get; }

    public AudioStreamEncoding Encoding { get; }

    public int SampleRate { get; }

    public int Channels { get; }

    public bool Done { get; }

    public int SampleCount => Payload.Length / BytesPerSample;

    public int FrameCount => SampleCount / Channels;

    public TimeSpan Duration { get; }

    private int BytesPerSample =>
        Encoding == AudioStreamEncoding.Float32LittleEndian
            ? sizeof(float)
            : sizeof(short);

    /// <summary>
    /// Parses and validates an audio stream update without depending on a host
    /// audio API.
    /// </summary>
    public static bool TryCreate(
        ExecutionStreamUpdate update,
        out AudioStreamChunk? chunk,
        out string? error)
    {
        ArgumentNullException.ThrowIfNull(update);
        chunk = null;
        error = null;

        if (!string.Equals(
                update.ContentType,
                "audio",
                StringComparison.OrdinalIgnoreCase))
        {
            error = "The stream update is not an audio chunk.";
            return false;
        }

        var metadata = update.ContentMetadata;
        var encodingName = ReadString(metadata, "encoding") ?? "pcm16le";
        var encoding = encodingName.ToLowerInvariant() switch
        {
            "pcm16le" => AudioStreamEncoding.Pcm16LittleEndian,
            "f32le" => AudioStreamEncoding.Float32LittleEndian,
            _ => (AudioStreamEncoding?)null
        };
        if (encoding is null)
        {
            error = $"Unsupported audio stream encoding '{encodingName}'.";
            return false;
        }

        if (!TryReadPositiveInt(metadata, "sample_rate", out var sampleRate))
        {
            error = "Audio stream metadata requires a positive sample_rate.";
            return false;
        }
        if (!TryReadPositiveInt(metadata, "channels", out var channels))
        {
            error = "Audio stream metadata requires a positive channel count.";
            return false;
        }

        if (!TryReadPayload(update.Content, out var payload, out error))
            return false;

        var bytesPerSample =
            encoding == AudioStreamEncoding.Float32LittleEndian
                ? sizeof(float)
                : sizeof(short);
        if (payload.Length % bytesPerSample != 0)
        {
            error =
                $"Audio payload length {payload.Length} is not aligned to {encodingName} samples.";
            return false;
        }
        var sampleCount = payload.Length / bytesPerSample;
        if (sampleCount % channels != 0)
        {
            error =
                $"Audio payload contains {sampleCount} samples, which is not divisible by {channels} channels.";
            return false;
        }

        var calculatedDuration = TimeSpan.FromSeconds(
            sampleCount / (double)channels / sampleRate);
        var duration = TryReadPositiveDouble(
            metadata,
            "duration_seconds",
            out var durationSeconds)
                ? TimeSpan.FromSeconds(durationSeconds)
                : calculatedDuration;
        chunk = new AudioStreamChunk(
            payload,
            encoding.Value,
            sampleRate,
            channels,
            update.Done,
            duration);
        return true;
    }

    /// <summary>
    /// Decodes the interleaved block to normalized float samples.
    /// </summary>
    public float[] DecodeInterleavedSamples()
    {
        var samples = new float[SampleCount];
        for (var index = 0; index < samples.Length; index++)
            samples[index] = ReadSample(index);
        return samples;
    }

    internal float ReadSample(int sampleIndex)
    {
        var source = Payload.Span;
        if (Encoding == AudioStreamEncoding.Float32LittleEndian)
        {
            var bits = BinaryPrimitives.ReadInt32LittleEndian(
                source.Slice(
                    sampleIndex * sizeof(float),
                    sizeof(float)));
            return BitConverter.Int32BitsToSingle(bits);
        }
        var value = BinaryPrimitives.ReadInt16LittleEndian(
            source.Slice(
                sampleIndex * sizeof(short),
                sizeof(short)));
        return value / 32768f;
    }

    private static bool TryReadPayload(
        NodeToolValue content,
        out byte[] payload,
        out string? error)
    {
        if (content.TryGetBytes(out payload))
        {
            error = null;
            return true;
        }

        var encoded = content.AsString();
        if (encoded is null)
        {
            payload = [];
            error = "Audio chunk content must be base64 text or binary data.";
            return false;
        }
        if (encoded.Length == 0)
        {
            payload = [];
            error = null;
            return true;
        }
        try
        {
            payload = Convert.FromBase64String(encoded);
            error = null;
            return true;
        }
        catch (FormatException)
        {
            payload = [];
            error = "Audio chunk content is not valid base64.";
            return false;
        }
    }

    private static string? ReadString(
        IReadOnlyDictionary<string, NodeToolValue> metadata,
        string key)
        => metadata.TryGetValue(key, out var value)
            ? value.AsString()
            : null;

    private static bool TryReadPositiveInt(
        IReadOnlyDictionary<string, NodeToolValue> metadata,
        string key,
        out int value)
    {
        value = 0;
        if (!metadata.TryGetValue(key, out var nodeToolValue) ||
            !nodeToolValue.TryGetLong(out var integer) ||
            integer is <= 0 or > int.MaxValue)
        {
            return false;
        }
        value = (int)integer;
        return true;
    }

    private static bool TryReadPositiveDouble(
        IReadOnlyDictionary<string, NodeToolValue> metadata,
        string key,
        out double value)
    {
        value = 0;
        return metadata.TryGetValue(key, out var nodeToolValue) &&
               nodeToolValue.TryGetDouble(out value) &&
               value >= 0 &&
               double.IsFinite(value);
    }
}

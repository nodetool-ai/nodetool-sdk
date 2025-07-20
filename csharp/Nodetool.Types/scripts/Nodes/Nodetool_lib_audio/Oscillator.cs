using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_lib_audio;

[MessagePackObject]
public class Oscillator
{
    [Key(0)]
    public object waveform { get; set; }
    [Key(1)]
    public double frequency { get; set; } = 440.0;
    [Key(2)]
    public double amplitude { get; set; } = 0.5;
    [Key(3)]
    public double duration { get; set; } = 1.0;
    [Key(4)]
    public int sample_rate { get; set; } = 44100;
    [Key(5)]
    public double pitch_envelope_amount { get; set; } = 0.0;
    [Key(6)]
    public double pitch_envelope_time { get; set; } = 0.5;
    [Key(7)]
    public object pitch_envelope_curve { get; set; }

    public Nodetool.Types.AudioRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.AudioRef);
    }
}

using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_lib_audio;

[MessagePackObject]
public class PitchShift
{
    [Key(0)]
    public Nodetool.Types.AudioRef audio { get; set; } = new Nodetool.Types.AudioRef();
    [Key(1)]
    public double semitones { get; set; } = 0.0;

    public Nodetool.Types.AudioRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.AudioRef);
    }
}

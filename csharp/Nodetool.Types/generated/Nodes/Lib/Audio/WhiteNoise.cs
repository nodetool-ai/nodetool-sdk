using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Lib;

[MessagePackObject]
public class WhiteNoise
{
    [Key(0)]
    public double amplitude { get; set; } = 0.5;
    [Key(1)]
    public double duration { get; set; } = 1.0;
    [Key(2)]
    public int sample_rate { get; set; } = 44100;

    public Nodetool.Types.AudioRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.AudioRef);
    }
}

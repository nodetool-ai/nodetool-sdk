using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Audio;

[MessagePackObject]
public class RemoveSilence
{
    [Key(0)]
    public Nodetool.Types.AudioRef audio { get; set; } = new Nodetool.Types.AudioRef();
    [Key(1)]
    public int min_length { get; set; } = 200;
    [Key(2)]
    public int threshold { get; set; } = -40;
    [Key(3)]
    public double reduction_factor { get; set; } = 1.0;
    [Key(4)]
    public int crossfade { get; set; } = 10;
    [Key(5)]
    public int min_silence_between_parts { get; set; } = 100;

    public Nodetool.Types.AudioRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.AudioRef);
    }
}

using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class RemoveSilence
{
    [Key(0)]
    public Nodetool.Types.Core.AudioRef audio { get; set; } = new Nodetool.Types.Core.AudioRef();
    [Key(1)]
    public int crossfade { get; set; } = 10;
    [Key(2)]
    public int min_length { get; set; } = 200;
    [Key(3)]
    public int min_silence_between_parts { get; set; } = 100;
    [Key(4)]
    public double reduction_factor { get; set; } = 1.0;
    [Key(5)]
    public int threshold { get; set; } = -40;

    public Nodetool.Types.Core.AudioRef Process()
    {
        return default(Nodetool.Types.Core.AudioRef);
    }
}

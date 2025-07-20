using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class SliceAudio
{
    [Key(0)]
    public Nodetool.Types.AudioRef audio { get; set; } = new Nodetool.Types.AudioRef();
    [Key(1)]
    public double start { get; set; } = 0.0;
    [Key(2)]
    public double end { get; set; } = 1.0;

    public Nodetool.Types.AudioRef Process()
    {
        return default(Nodetool.Types.AudioRef);
    }
}

using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class SliceAudio
{
    [Key(0)]
    public Nodetool.Types.Core.AudioRef audio { get; set; } = new Nodetool.Types.Core.AudioRef();
    [Key(1)]
    public double end { get; set; } = 1.0;
    [Key(2)]
    public double start { get; set; } = 0.0;

    public Nodetool.Types.Core.AudioRef Process()
    {
        return default(Nodetool.Types.Core.AudioRef);
    }
}

using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_lib_audio;

[MessagePackObject]
public class Compress
{
    [Key(0)]
    public Nodetool.Types.AudioRef audio { get; set; } = new Nodetool.Types.AudioRef();
    [Key(1)]
    public double threshold { get; set; } = -20.0;
    [Key(2)]
    public double ratio { get; set; } = 4.0;
    [Key(3)]
    public double attack { get; set; } = 5.0;
    [Key(4)]
    public double release { get; set; } = 50.0;

    public Nodetool.Types.AudioRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.AudioRef);
    }
}

using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Lib;

[MessagePackObject]
public class Delay
{
    [Key(0)]
    public Nodetool.Types.AudioRef audio { get; set; } = new Nodetool.Types.AudioRef();
    [Key(1)]
    public double delay_seconds { get; set; } = 0.5;
    [Key(2)]
    public double feedback { get; set; } = 0.3;
    [Key(3)]
    public double mix { get; set; } = 0.5;

    public Nodetool.Types.AudioRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.AudioRef);
    }
}

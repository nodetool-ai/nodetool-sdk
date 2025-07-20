using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class Fps
{
    [Key(0)]
    public Nodetool.Types.VideoRef video { get; set; } = new Nodetool.Types.VideoRef();

    public double Process()
    {
        // Implementation would be generated based on node logic
        return default(double);
    }
}

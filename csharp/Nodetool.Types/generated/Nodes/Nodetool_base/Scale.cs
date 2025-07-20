using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class Scale
{
    [Key(0)]
    public Nodetool.Types.ImageRef image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(1)]
    public double scale { get; set; } = 1.0;

    public Nodetool.Types.ImageRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.ImageRef);
    }
}

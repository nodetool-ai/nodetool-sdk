using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class FrameToVideo
{
    [Key(0)]
    public Nodetool.Types.ImageRef frame { get; set; } = new Nodetool.Types.ImageRef();
    [Key(1)]
    public int index { get; set; } = 0;
    [Key(2)]
    public double fps { get; set; } = 30;
    [Key(3)]
    public Nodetool.Types.Event event { get; set; } = new Nodetool.Types.Event();

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}

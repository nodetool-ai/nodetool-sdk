using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Video;

[MessagePackObject]
public class Blur
{
    [Key(0)]
    public Nodetool.Types.VideoRef video { get; set; } = new Nodetool.Types.VideoRef();
    [Key(1)]
    public double strength { get; set; } = 5.0;

    public Nodetool.Types.VideoRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.VideoRef);
    }
}

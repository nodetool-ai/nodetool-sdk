using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_fal;

[MessagePackObject]
public class PixverseTransition
{
    [Key(0)]
    public Nodetool.Types.ImageRef start_image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(1)]
    public Nodetool.Types.ImageRef end_image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(2)]
    public int seed { get; set; } = -1;

    public Nodetool.Types.VideoRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.VideoRef);
    }
}

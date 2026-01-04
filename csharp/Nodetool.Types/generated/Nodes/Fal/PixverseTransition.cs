using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class PixverseTransition
{
    [Key(0)]
    public Nodetool.Types.Core.ImageRef end_image { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(1)]
    public int seed { get; set; } = -1;
    [Key(2)]
    public Nodetool.Types.Core.ImageRef start_image { get; set; } = new Nodetool.Types.Core.ImageRef();

    public Nodetool.Types.Core.VideoRef Process()
    {
        return default(Nodetool.Types.Core.VideoRef);
    }
}

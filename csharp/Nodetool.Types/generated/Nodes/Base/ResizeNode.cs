using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ResizeNode
{
    [Key(0)]
    public int height { get; set; } = -1;
    [Key(1)]
    public Nodetool.Types.Core.VideoRef video { get; set; } = new Nodetool.Types.Core.VideoRef();
    [Key(2)]
    public int width { get; set; } = -1;

    public Nodetool.Types.Core.VideoRef Process()
    {
        return default(Nodetool.Types.Core.VideoRef);
    }
}

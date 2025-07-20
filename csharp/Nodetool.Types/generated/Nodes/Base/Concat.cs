using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Concat
{
    [Key(0)]
    public Nodetool.Types.VideoRef video_a { get; set; } = new Nodetool.Types.VideoRef();
    [Key(1)]
    public Nodetool.Types.VideoRef video_b { get; set; } = new Nodetool.Types.VideoRef();

    public Nodetool.Types.VideoRef Process()
    {
        return default(Nodetool.Types.VideoRef);
    }
}

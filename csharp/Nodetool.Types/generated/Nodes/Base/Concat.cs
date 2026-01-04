using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Concat
{
    [Key(0)]
    public Nodetool.Types.Core.VideoRef video_a { get; set; } = new Nodetool.Types.Core.VideoRef();
    [Key(1)]
    public Nodetool.Types.Core.VideoRef video_b { get; set; } = new Nodetool.Types.Core.VideoRef();

    public Nodetool.Types.Core.VideoRef Process()
    {
        return default(Nodetool.Types.Core.VideoRef);
    }
}

using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Video
{
    [Key(0)]
    public Nodetool.Types.VideoRef value { get; set; } = new Nodetool.Types.VideoRef();

    public Nodetool.Types.VideoRef Process()
    {
        return default(Nodetool.Types.VideoRef);
    }
}

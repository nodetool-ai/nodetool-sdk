using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Transition
{
    [Key(0)]
    public double duration { get; set; } = 1.0;
    [Key(1)]
    public object transition_type { get; set; } = @"fade";
    [Key(2)]
    public Nodetool.Types.Core.VideoRef video_a { get; set; } = new Nodetool.Types.Core.VideoRef();
    [Key(3)]
    public Nodetool.Types.Core.VideoRef video_b { get; set; } = new Nodetool.Types.Core.VideoRef();

    public Nodetool.Types.Core.VideoRef Process()
    {
        return default(Nodetool.Types.Core.VideoRef);
    }
}

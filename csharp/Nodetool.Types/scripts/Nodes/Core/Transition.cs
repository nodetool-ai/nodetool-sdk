using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class Transition
{
    [Key(0)]
    public Nodetool.Types.VideoRef video_a { get; set; } = new Nodetool.Types.VideoRef();
    [Key(1)]
    public Nodetool.Types.VideoRef video_b { get; set; } = new Nodetool.Types.VideoRef();
    [Key(2)]
    public object transition_type { get; set; } = "TransitionType.fade";
    [Key(3)]
    public double duration { get; set; } = 1.0;

    public Nodetool.Types.VideoRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.VideoRef);
    }
}

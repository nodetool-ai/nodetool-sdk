using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Video;

[MessagePackObject]
public class AddAudio
{
    [Key(0)]
    public Nodetool.Types.VideoRef video { get; set; } = new Nodetool.Types.VideoRef();
    [Key(1)]
    public Nodetool.Types.AudioRef audio { get; set; } = new Nodetool.Types.AudioRef();
    [Key(2)]
    public double volume { get; set; } = 1.0;
    [Key(3)]
    public bool mix { get; set; } = false;

    public Nodetool.Types.VideoRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.VideoRef);
    }
}

using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Video;

[MessagePackObject]
public class ChromaKey
{
    [Key(0)]
    public Nodetool.Types.VideoRef video { get; set; } = new Nodetool.Types.VideoRef();
    [Key(1)]
    public Nodetool.Types.ColorRef key_color { get; set; } = new Nodetool.Types.ColorRef();
    [Key(2)]
    public double similarity { get; set; } = 0.3;
    [Key(3)]
    public double blend { get; set; } = 0.1;

    public Nodetool.Types.VideoRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.VideoRef);
    }
}

using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class HaiperImageToVideo
{
    [Key(0)]
    public Nodetool.Types.ImageRef image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(1)]
    public string prompt { get; set; } = "";
    [Key(2)]
    public object duration { get; set; }
    [Key(3)]
    public bool prompt_enhancer { get; set; } = true;
    [Key(4)]
    public int seed { get; set; } = -1;

    public Nodetool.Types.VideoRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.VideoRef);
    }
}

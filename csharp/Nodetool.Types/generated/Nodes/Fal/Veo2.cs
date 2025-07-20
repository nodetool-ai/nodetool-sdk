using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class Veo2
{
    [Key(0)]
    public string prompt { get; set; } = "";
    [Key(1)]
    public object duration { get; set; }
    [Key(2)]
    public object aspect_ratio { get; set; }
    [Key(3)]
    public int seed { get; set; } = -1;

    public Nodetool.Types.VideoRef Process()
    {
        return default(Nodetool.Types.VideoRef);
    }
}

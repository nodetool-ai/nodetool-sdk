using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class PixverseImageToVideo
{
    [Key(0)]
    public Nodetool.Types.ImageRef image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(1)]
    public string prompt { get; set; } = "";
    [Key(2)]
    public int seed { get; set; } = -1;

    public Nodetool.Types.VideoRef Process()
    {
        return default(Nodetool.Types.VideoRef);
    }
}

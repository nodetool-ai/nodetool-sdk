using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class LumaPhoton
{
    [Key(0)]
    public string prompt { get; set; } = "";
    [Key(1)]
    public object aspect_ratio { get; set; } = "AspectRatioLuma.RATIO_1_1";

    public Nodetool.Types.ImageRef Process()
    {
        return default(Nodetool.Types.ImageRef);
    }
}

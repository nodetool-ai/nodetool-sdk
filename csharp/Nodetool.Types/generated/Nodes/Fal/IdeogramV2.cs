using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class IdeogramV2
{
    [Key(0)]
    public string prompt { get; set; } = "";
    [Key(1)]
    public object aspect_ratio { get; set; } = "AspectRatio.RATIO_1_1";
    [Key(2)]
    public bool expand_prompt { get; set; } = true;
    [Key(3)]
    public object style { get; set; } = "IdeogramStyle.AUTO";
    [Key(4)]
    public string negative_prompt { get; set; } = "";
    [Key(5)]
    public int seed { get; set; } = -1;

    public Nodetool.Types.ImageRef Process()
    {
        return default(Nodetool.Types.ImageRef);
    }
}

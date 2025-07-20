using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class WanV2_1_13BTextToVideo
{
    [Key(0)]
    public string prompt { get; set; } = "";
    [Key(1)]
    public int seed { get; set; } = -1;

    public Nodetool.Types.VideoRef Process()
    {
        return default(Nodetool.Types.VideoRef);
    }
}

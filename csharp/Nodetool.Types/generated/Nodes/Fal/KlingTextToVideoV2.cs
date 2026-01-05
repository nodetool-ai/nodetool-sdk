using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class KlingTextToVideoV2
{
    [Key(0)]
    public object aspect_ratio { get; set; }
    [Key(1)]
    public object duration { get; set; }
    [Key(2)]
    public string prompt { get; set; } = @"";

    public Nodetool.Types.Core.VideoRef Process()
    {
        return default(Nodetool.Types.Core.VideoRef);
    }
}

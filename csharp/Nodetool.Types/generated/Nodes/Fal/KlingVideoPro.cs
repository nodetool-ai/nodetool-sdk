using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class KlingVideoPro
{
    [Key(0)]
    public object aspect_ratio { get; set; }
    [Key(1)]
    public object duration { get; set; }
    [Key(2)]
    public Nodetool.Types.Core.ImageRef image { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(3)]
    public string prompt { get; set; } = @"";

    public Nodetool.Types.Core.VideoRef Process()
    {
        return default(Nodetool.Types.Core.VideoRef);
    }
}

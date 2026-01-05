using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class HaiperImageToVideo
{
    [Key(0)]
    public object duration { get; set; }
    [Key(1)]
    public Nodetool.Types.Core.ImageRef image { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(2)]
    public string prompt { get; set; } = @"";
    [Key(3)]
    public bool prompt_enhancer { get; set; } = true;
    [Key(4)]
    public int seed { get; set; } = -1;

    public Nodetool.Types.Core.VideoRef Process()
    {
        return default(Nodetool.Types.Core.VideoRef);
    }
}

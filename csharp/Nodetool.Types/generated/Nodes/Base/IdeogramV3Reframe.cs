using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class IdeogramV3Reframe
{
    [Key(0)]
    public Nodetool.Types.Core.ImageRef image { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(1)]
    public object image_size { get; set; } = @"square_hd";
    [Key(2)]
    public object rendering_speed { get; set; } = @"BALANCED";
    [Key(3)]
    public int seed { get; set; } = 0;
    [Key(4)]
    public object style { get; set; } = @"AUTO";

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}

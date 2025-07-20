using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class BriaBackgroundReplace
{
    [Key(0)]
    public Nodetool.Types.ImageRef image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(1)]
    public Nodetool.Types.ImageRef ref_image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(2)]
    public string prompt { get; set; } = "";
    [Key(3)]
    public string negative_prompt { get; set; } = "";
    [Key(4)]
    public bool refine_prompt { get; set; } = true;
    [Key(5)]
    public int seed { get; set; } = -1;

    public Nodetool.Types.ImageRef Process()
    {
        return default(Nodetool.Types.ImageRef);
    }
}
